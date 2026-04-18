using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Llm;

public sealed class LlamaSharpClient : ILlmClient
{
    // Anti-prompts as a last line of defense. When the model's chat template is applied
    // correctly the native EOT tokens already stop generation, but these catch cases
    // where the template metadata is missing or the model leaks a literal marker.
    private static readonly string[] CommonAntiPrompts =
    {
        "<|im_end|>",       // Qwen / ChatML
        "<end_of_turn>",    // Gemma
        "<|eot_id|>",       // Llama 3.x
        "<|endoftext|>",    // generic
    };

    private readonly LlmOptions _options;
    private readonly ILogger<LlamaSharpClient> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _inferLock = new(1, 1);

    private ModelParams? _modelParams;
    private LLamaWeights? _weights;
    private StatelessExecutor? _executor;

    public LlamaSharpClient(IOptions<SharpBotOptions> options, ILogger<LlamaSharpClient> logger)
    {
        _options = options.Value.Llm;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_executor is not null) return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_executor is not null) return;

            if (!File.Exists(_options.ModelPath))
            {
                throw new FileNotFoundException(
                    $"Model file not found: {_options.ModelPath}. Run 'sharpbot models download' first.",
                    _options.ModelPath);
            }

            _logger.LogInformation("Loading model: {Path}", _options.ModelPath);
            _modelParams = new ModelParams(_options.ModelPath)
            {
                ContextSize = (uint)_options.ContextSize,
                GpuLayerCount = _options.GpuLayerCount,
            };

            _weights = await LLamaWeights.LoadFromFileAsync(_modelParams, cancellationToken).ConfigureAwait(false);
            _executor = new StatelessExecutor(_weights, _modelParams);
            _logger.LogInformation("Model ready.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<LlmResponse> InferAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await _inferLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Render the conversation using the model's native chat template from the GGUF metadata.
            // For Qwen this produces ChatML (<|im_start|>role\ncontent<|im_end|>), for Gemma its own
            // format, for Llama 3 yet another — handled transparently by the GGUF's template.
            var template = new LLamaTemplate(_weights!, strict: true) { AddAssistant = true };
            foreach (var m in conversation)
            {
                var role = m.Role switch
                {
                    ChatRole.System => "system",
                    ChatRole.User => "user",
                    ChatRole.Assistant => "assistant",
                    _ => null, // Tool messages not supported in v0.1
                };
                if (role is null) continue;
                if (string.IsNullOrEmpty(m.Content)) continue; // skip empty assistant placeholders
                template.Add(role, m.Content);
            }

            var promptSpan = template.Apply();
            var prompt = LLamaTemplate.Encoding.GetString(promptSpan.ToArray());

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 1024,
                AntiPrompts = CommonAntiPrompts,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.7f,
                    TopP = 0.95f,
                },
            };

            var sb = new StringBuilder();
            await foreach (var chunk in _executor!.InferAsync(prompt, inferenceParams, cancellationToken).ConfigureAwait(false))
            {
                sb.Append(chunk);
            }

            var text = sb.ToString().Trim();
            foreach (var stop in CommonAntiPrompts)
            {
                if (text.EndsWith(stop, StringComparison.Ordinal))
                {
                    text = text[..^stop.Length].TrimEnd();
                }
            }

            return new LlmResponse(text, Array.Empty<ToolCall>());
        }
        finally
        {
            _inferLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _executor = null;
        _weights?.Dispose();
        _weights = null;
        _initLock.Dispose();
        _inferLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
