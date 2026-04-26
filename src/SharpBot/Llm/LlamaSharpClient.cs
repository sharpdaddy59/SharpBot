using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Llm;

public sealed partial class LlamaSharpClient : ILlmClient
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

    // Per-conversation state: each gets its own LLamaContext + InteractiveExecutor so that
    // the KV cache persists across turns and only new tokens get prefilled.
    private readonly Dictionary<string, ConversationState> _states =
        new(StringComparer.Ordinal);

    private ModelParams? _modelParams;
    private LLamaWeights? _weights;

    public LlamaSharpClient(IOptions<SharpBotOptions> options, ILogger<LlamaSharpClient> logger)
    {
        _options = options.Value.Llm;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_weights is not null) return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_weights is not null) return;

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
            _logger.LogInformation("Model ready. Per-conversation KV cache reuse enabled (max {N} active).",
                Math.Max(1, _options.MaxActiveConversations));
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<LlmResponse> InferAsync(
        string conversationId,
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        LlmResponse? final = null;
        await foreach (var ev in StreamInferAsync(conversationId, conversation, availableTools, cancellationToken)
            .ConfigureAwait(false))
        {
            if (ev.Final is { } f) final = f;
        }
        return final ?? new LlmResponse(string.Empty, Array.Empty<ToolCall>());
    }

    public async IAsyncEnumerable<LlmStreamEvent> StreamInferAsync(
        string conversationId,
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await _inferLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetOrCreateState(conversationId);
            state.LastUsed = DateTimeOffset.UtcNow;

            // Render the full prompt (including tool injection and the trailing assistant marker)
            // using the GGUF's native chat template.
            var effective = InjectToolDefinitions(conversation, availableTools);
            var template = new LLamaTemplate(_weights!, strict: true) { AddAssistant = true };
            foreach (var rendered in effective.SelectMany(RenderMessage))
            {
                template.Add(rendered.Role, rendered.Content);
            }
            var fullPrompt = LLamaTemplate.Encoding.GetString(template.Apply().ToArray());

            // If the new rendering extends what this conversation's executor has already
            // processed, we only need to prefill the delta. Otherwise reset the cache —
            // something changed (tool list, older turn edited, ...) and KV state is stale.
            //
            // Subtlety: LlamaSharp's InteractiveExecutor consumes the EOT anti-prompt that
            // stops generation but does NOT emit it in the streamed output. So after inference
            // the KV cache contains one extra anti-prompt token beyond what we tracked. We
            // account for that by retrying the prefix check against `state.ProcessedText` plus
            // each possible anti-prompt. Whichever one aligns with the new rendering tells us
            // what the model's EOT actually was, and we keep track of it for future calls.
            string delta;
            if (state.ProcessedText.Length > 0 &&
                fullPrompt.StartsWith(state.ProcessedText, StringComparison.Ordinal))
            {
                delta = fullPrompt[state.ProcessedText.Length..];
                _logger.LogDebug(
                    "Conversation {Id}: KV-cache hit — prefilling {DeltaBytes} new bytes of {FullBytes}.",
                    conversationId, delta.Length, fullPrompt.Length);
            }
            else if (state.ProcessedText.Length > 0 &&
                     TryAlignWithAntiPrompt(state.ProcessedText, fullPrompt, out var extended, out var stop))
            {
                state.ProcessedText = extended;
                delta = fullPrompt[extended.Length..];
                _logger.LogDebug(
                    "Conversation {Id}: KV-cache hit via trailing '{Stop}' — prefilling {DeltaBytes} of {FullBytes}.",
                    conversationId, stop, delta.Length, fullPrompt.Length);
            }
            else
            {
                if (state.ProcessedText.Length > 0)
                {
                    _logger.LogInformation(
                        "Conversation {Id}: KV-cache miss — resetting and doing a full prefill.",
                        conversationId);
                }
                state.Reset(_weights!, _modelParams!);
                delta = fullPrompt;
            }

            if (delta.Length == 0)
            {
                // Nothing new to infer. Caller fed the same conversation twice.
                yield return LlmStreamEvent.Done(new LlmResponse(string.Empty, Array.Empty<ToolCall>()));
                yield break;
            }

            var inferenceParams = BuildInferenceParams();

            var sb = new StringBuilder();
            var emitter = new StreamingTextEmitter();
            await foreach (var chunk in state.Executor!.InferAsync(delta, inferenceParams, cancellationToken).ConfigureAwait(false))
            {
                sb.Append(chunk);
                var visible = emitter.Push(chunk);
                if (visible is not null)
                {
                    yield return LlmStreamEvent.Delta(visible);
                }
            }

            // Flush any clean prose still buffered (e.g. a sentence the model finished without
            // a sentence-ending punctuation char). If the emitter went silent on a tool marker
            // this is a no-op.
            var tail = emitter.Drain();
            if (tail is not null)
            {
                yield return LlmStreamEvent.Delta(tail);
            }

            // Record everything the executor now has in its KV cache so the next call can
            // compute an accurate delta. The executor processed `delta` (our prefilled tokens)
            // plus whatever it generated on top.
            var generated = sb.ToString();
            state.ProcessedText = fullPrompt + generated;

            // Strip trailing anti-prompt tokens that may have leaked into the visible output.
            var raw = generated.Trim();
            foreach (var stopAnti in CommonAntiPrompts)
            {
                if (raw.EndsWith(stopAnti, StringComparison.Ordinal))
                {
                    raw = raw[..^stopAnti.Length].TrimEnd();
                }
            }

            var (text, calls) = ExtractToolCalls(raw);
            yield return LlmStreamEvent.Done(new LlmResponse(text, calls));
        }
        finally
        {
            _inferLock.Release();
        }
    }

    private ConversationState GetOrCreateState(string conversationId)
    {
        if (_states.TryGetValue(conversationId, out var existing))
        {
            return existing;
        }

        EvictIfNeeded();

        var state = new ConversationState { LastUsed = DateTimeOffset.UtcNow };
        state.Reset(_weights!, _modelParams!);
        _states[conversationId] = state;
        _logger.LogInformation("Conversation {Id}: created new KV cache ({Active} active).",
            conversationId, _states.Count);

        if (_options.WarmupOnFirstTurn)
        {
            WarmupExecutor(state, conversationId);
        }

        return state;
    }

    /// <summary>
    /// Some models (notably Gemma) produce empty output on their first one or two inferences
    /// against a fresh executor. A brief warmup with MaxTokens=1 primes whatever internal
    /// state they're missing. Adds ~100–300 ms to the first turn of each new conversation;
    /// worth it to avoid a silent no-op first response.
    /// </summary>
    private void WarmupExecutor(ConversationState state, string conversationId)
    {
        try
        {
            var warmupParams = new InferenceParams
            {
                MaxTokens = 1,
                AntiPrompts = CommonAntiPrompts,
            };
            // Simple text; the executor will render it under the model's chat template during
            // the real first call anyway. We just need to bump its internal state.
            for (var i = 0; i < 2; i++)
            {
                var consumer = state.Executor!.InferAsync("hi", warmupParams).GetAsyncEnumerator();
                try
                {
                    while (consumer.MoveNextAsync().AsTask().GetAwaiter().GetResult()) { /* drain */ }
                }
                finally
                {
                    consumer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
            // The warmup dirtied the KV cache with "hi"; reset so the real first inference
            // gets a clean prefill and our ProcessedText tracking starts from zero.
            state.Reset(_weights!, _modelParams!);
            _logger.LogDebug("Conversation {Id}: warmed up executor.", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Conversation {Id}: warmup failed; continuing anyway.", conversationId);
        }
    }

    private InferenceParams BuildInferenceParams() => new()
    {
        MaxTokens = _options.MaxTokens,
        AntiPrompts = CommonAntiPrompts,
        SamplingPipeline = new DefaultSamplingPipeline
        {
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            TopK = _options.TopK,
            RepeatPenalty = _options.RepeatPenalty,
            FrequencyPenalty = _options.FrequencyPenalty,
            PresencePenalty = _options.PresencePenalty,
        },
    };

    /// <summary>
    /// Tries to make <paramref name="processed"/> align with <paramref name="fullPrompt"/> by
    /// appending one of the known anti-prompt tokens. Returns true if any candidate lands the
    /// extended string as a proper prefix of <paramref name="fullPrompt"/>. Used to recover the
    /// EOT token that LlamaSharp's InteractiveExecutor consumes silently on stop.
    /// </summary>
    private static bool TryAlignWithAntiPrompt(
        string processed,
        string fullPrompt,
        out string extended,
        out string matchedStop)
    {
        foreach (var stop in CommonAntiPrompts)
        {
            var candidate = processed + stop;
            if (fullPrompt.StartsWith(candidate, StringComparison.Ordinal))
            {
                extended = candidate;
                matchedStop = stop;
                return true;
            }
        }
        extended = processed;
        matchedStop = string.Empty;
        return false;
    }

    private void EvictIfNeeded()
    {
        var max = Math.Max(1, _options.MaxActiveConversations);
        if (_states.Count < max) return;

        var oldest = _states.OrderBy(kv => kv.Value.LastUsed).First();
        _logger.LogInformation(
            "Evicting conversation {Id} (LRU, idle for {Idle}).",
            oldest.Key, DateTimeOffset.UtcNow - oldest.Value.LastUsed);
        oldest.Value.Dispose();
        _states.Remove(oldest.Key);
    }

    /// <summary>
    /// If tools are available, weave them into the conversation's system message using Qwen's format.
    /// Returns the original list unchanged when no tools are supplied.
    /// </summary>
    private static List<ChatMessage> InjectToolDefinitions(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools)
    {
        var result = new List<ChatMessage>(conversation);
        if (availableTools.Count == 0) return result;

        var toolSection = BuildToolsSystemSection(availableTools);

        var firstIdx = result.FindIndex(m => m.Role == ChatRole.System);
        if (firstIdx >= 0)
        {
            var existing = result[firstIdx];
            var combined = string.IsNullOrWhiteSpace(existing.Content)
                ? toolSection
                : existing.Content.TrimEnd() + "\n\n" + toolSection;
            result[firstIdx] = existing with { Content = combined };
        }
        else
        {
            result.Insert(0, new ChatMessage(ChatRole.System, toolSection));
        }
        return result;
    }

    private static string BuildToolsSystemSection(IReadOnlyList<ToolDescriptor> tools)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Tools");
        sb.AppendLine();
        sb.AppendLine("You may call one or more functions to assist with the user query.");
        sb.AppendLine();
        sb.AppendLine("You are provided with function signatures within <tools></tools> XML tags:");
        sb.AppendLine("<tools>");
        foreach (var tool in tools)
        {
            JsonElement parameters;
            try
            {
                parameters = JsonDocument.Parse(tool.ParametersJsonSchema).RootElement.Clone();
            }
            catch (JsonException)
            {
                parameters = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();
            }

            var entry = new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters,
                },
            };
            sb.AppendLine(JsonSerializer.Serialize(entry));
        }
        sb.AppendLine("</tools>");
        sb.AppendLine();
        sb.AppendLine("For each function call, return a json object with function name and arguments");
        sb.AppendLine("within <tool_call></tool_call> XML tags:");
        sb.AppendLine("<tool_call>");
        sb.AppendLine("{\"name\": <function-name>, \"arguments\": <args-json-object>}");
        sb.Append("</tool_call>");
        return sb.ToString();
    }

    /// <summary>
    /// Translate one ChatMessage into zero or more (role, content) entries fed to LLamaTemplate.
    /// Handles the assistant-with-tool-calls case (embed <tool_call> blocks in the assistant turn)
    /// and the tool-result case (role "tool" so the model's chat template wraps it appropriately).
    /// </summary>
    private static IEnumerable<(string Role, string Content)> RenderMessage(ChatMessage message)
    {
        var role = message.Role switch
        {
            ChatRole.System => "system",
            ChatRole.User => "user",
            ChatRole.Assistant => "assistant",
            ChatRole.Tool => "tool",
            _ => null,
        };
        if (role is null) yield break;

        if (message.Role == ChatRole.Assistant && message.ToolCalls is { Count: > 0 })
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message.Content)) sb.AppendLine(message.Content);
            foreach (var call in message.ToolCalls)
            {
                sb.AppendLine("<tool_call>");
                sb.AppendLine($"{{\"name\": \"{call.Name}\", \"arguments\": {NormalizeArgsJson(call.ArgumentsJson)}}}");
                sb.AppendLine("</tool_call>");
            }
            yield return (role, sb.ToString().TrimEnd());
            yield break;
        }

        if (string.IsNullOrEmpty(message.Content) && message.Role != ChatRole.Tool) yield break;
        yield return (role, message.Content);
    }

    private static string NormalizeArgsJson(string? argsJson)
    {
        if (string.IsNullOrWhiteSpace(argsJson)) return "{}";
        try
        {
            using var doc = JsonDocument.Parse(argsJson);
            return doc.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    /// <summary>
    /// Pull tool-call blocks out of the raw model output. Qwen 2.5's native format is
    /// <![CDATA[<tool_call>...</tool_call>]]>; other models (Gemma, Llama) often improvise with a
    /// markdown JSON code fence instead. We recognize both. Returns the remaining user-facing
    /// text (with those blocks removed) and the list of parsed calls.
    /// </summary>
    internal static (string Text, IReadOnlyList<ToolCall> Calls) ExtractToolCalls(string raw)
    {
        var calls = new List<ToolCall>();
        var cleaned = raw;
        var malformedToolCalls = 0;

        // Primary: Qwen's native <tool_call>...</tool_call> format. Tags always get stripped
        // — a tag-wrapped block is unambiguously intended as a tool call, so even if the JSON
        // inside is malformed we remove it rather than leak the raw tags to the user.
        cleaned = ExtractWith(cleaned, ToolCallRegex(), calls,
            requireShape: false, stripOnFailure: true, failureCount: out var nativeFailures);
        malformedToolCalls += nativeFailures;

        // Fallback: ```json { ... } ``` fenced blocks whose JSON happens to carry the expected
        // tool-call shape (name + arguments). Shape-checked so we don't strip random JSON
        // examples the model might include in its response. Not stripped on failure.
        cleaned = ExtractWith(cleaned, FencedJsonRegex(), calls,
            requireShape: true, stripOnFailure: false, failureCount: out _);

        cleaned = cleaned.Trim();
        if (malformedToolCalls > 0 && calls.Count == 0)
        {
            // The model clearly tried to call a tool but fumbled the JSON. Surface something
            // meaningful rather than an empty response.
            cleaned = $"(The model tried to call a tool but produced invalid JSON. " +
                      $"This sometimes happens with aggressive sampling penalties — try lowering " +
                      $"RepeatPenalty in data/user-config.json.)\n\n{cleaned}".TrimEnd();
        }
        return (cleaned, calls);
    }

    private static string ExtractWith(
        string input,
        Regex pattern,
        List<ToolCall> calls,
        bool requireShape,
        bool stripOnFailure,
        out int failureCount)
    {
        failureCount = 0;
        var matches = pattern.Matches(input);
        if (matches.Count == 0) return input;

        // Only strip blocks from the returned text when they actually produced a tool call —
        // otherwise ordinary JSON examples would get ripped out of the model's prose.
        var toStrip = new List<Match>();

        foreach (Match m in matches)
        {
            var json = m.Groups[1].Value.Trim();
            var parsedOk = false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;
                if (!root.TryGetProperty("name", out var nameEl)) continue;
                var name = nameEl.GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                // For the fenced-block fallback, require the shape to include an 'arguments'
                // key so we don't accidentally strip ordinary JSON examples the model shares.
                if (requireShape && !root.TryGetProperty("arguments", out _)) continue;

                var argsJson = "{}";
                if (root.TryGetProperty("arguments", out var argsEl))
                {
                    argsJson = argsEl.ValueKind switch
                    {
                        JsonValueKind.String => argsEl.GetString() ?? "{}",
                        JsonValueKind.Object => argsEl.GetRawText(),
                        _ => argsEl.GetRawText(),
                    };
                }

                var id = "call_" + Guid.NewGuid().ToString("N")[..8];
                calls.Add(new ToolCall(id, name!, argsJson));
                toStrip.Add(m);
                parsedOk = true;
            }
            catch (JsonException)
            {
                // Malformed JSON — the model tried but fumbled it.
            }

            if (!parsedOk)
            {
                failureCount++;
                // For wrappers that unambiguously indicate "I'm calling a tool" (native tool_call
                // tags), remove the block even on parse failure so the raw tag/JSON doesn't leak
                // to the user as the bot's reply.
                if (stripOnFailure) toStrip.Add(m);
            }
        }

        if (toStrip.Count == 0) return input;

        // Strip accepted matches back-to-front so earlier indices stay valid.
        var sb = new StringBuilder(input);
        foreach (var m in toStrip.OrderByDescending(x => x.Index))
        {
            sb.Remove(m.Index, m.Length);
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"<tool_call>\s*(\{.*?\})\s*</tool_call>", RegexOptions.Singleline)]
    private static partial Regex ToolCallRegex();

    [GeneratedRegex(@"```(?:json)?\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex FencedJsonRegex();

    public ValueTask DisposeAsync()
    {
        foreach (var state in _states.Values)
        {
            state.Dispose();
        }
        _states.Clear();
        _weights?.Dispose();
        _weights = null;
        _initLock.Dispose();
        _inferLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class ConversationState : IDisposable
    {
        public LLamaContext? Context;
        public InteractiveExecutor? Executor;
        public string ProcessedText = string.Empty;
        public DateTimeOffset LastUsed;

        public void Reset(LLamaWeights weights, ModelParams modelParams)
        {
            Context?.Dispose();
            Context = weights.CreateContext(modelParams);
            Executor = new InteractiveExecutor(Context);
            ProcessedText = string.Empty;
        }

        public void Dispose()
        {
            Executor = null;
            Context?.Dispose();
            Context = null;
        }
    }
}
