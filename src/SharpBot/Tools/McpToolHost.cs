using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Tools;

public sealed class McpToolHost : IToolHost
{
    private readonly McpOptions _options;
    private readonly ILogger<McpToolHost> _logger;
    private IReadOnlyList<ToolDescriptor> _tools = Array.Empty<ToolDescriptor>();

    public McpToolHost(IOptions<SharpBotOptions> options, ILogger<McpToolHost> logger)
    {
        _options = options.Value.Mcp;
        _logger = logger;
    }

    public IReadOnlyList<ToolDescriptor> AvailableTools => _tools;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("McpToolHost: {Count} server(s) configured", _options.Servers.Count);
        // TODO: spawn each configured MCP server via ModelContextProtocol C# SDK client,
        // list tools from each, and aggregate into _tools with name-prefixing to avoid collisions.
        _tools = Array.Empty<ToolDescriptor>();
        return Task.CompletedTask;
    }

    public Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            $"McpToolHost.ExecuteAsync: route call '{call.Name}' to the owning MCP server client.");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
