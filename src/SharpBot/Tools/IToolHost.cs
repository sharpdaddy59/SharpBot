using SharpBot.Agent;

namespace SharpBot.Tools;

public interface IToolHost : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<ToolDescriptor> AvailableTools { get; }
    Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
}
