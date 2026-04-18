using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Llm;

public sealed class LlamaSharpClient : ILlmClient
{
    private readonly LlmOptions _options;
    private readonly ILogger<LlamaSharpClient> _logger;

    public LlamaSharpClient(IOptions<SharpBotOptions> options, ILogger<LlamaSharpClient> logger)
    {
        _options = options.Value.Llm;
        _logger = logger;
    }

    public Task<LlmResponse> InferAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "LlamaSharpClient.InferAsync: wire up LLamaSharp ChatSession with ModelPath=" + _options.ModelPath);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
