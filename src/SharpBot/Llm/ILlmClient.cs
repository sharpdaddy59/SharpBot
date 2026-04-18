using SharpBot.Agent;

namespace SharpBot.Llm;

public interface ILlmClient : IAsyncDisposable
{
    Task<LlmResponse> InferAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default);
}
