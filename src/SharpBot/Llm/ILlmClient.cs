using SharpBot.Agent;

namespace SharpBot.Llm;

public interface ILlmClient : IAsyncDisposable
{
    /// <summary>
    /// Loads the model and prepares the inference context. Idempotent — safe to call multiple times.
    /// Typically called during host startup to pay the ~3–5s load cost before the first user request.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a single round of inference over the provided conversation and available tool schemas.
    /// Returns the assistant's response (text + any tool calls). Stateless from the caller's point of view —
    /// the full conversation must be passed on every call — but the implementation may cache KV state
    /// per <paramref name="conversationId"/> so follow-up turns only prefill the new tokens.
    /// </summary>
    /// <param name="conversationId">
    /// Stable identifier for the conversation (Telegram chat id, REPL session id, etc.). Used by the
    /// client to key a KV-cache per conversation. Assumes the conversation list grows monotonically
    /// between calls for the same id; on any divergence the cache is reset transparently.
    /// </param>
    Task<LlmResponse> InferAsync(
        string conversationId,
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default);
}
