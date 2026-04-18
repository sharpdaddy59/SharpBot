using Microsoft.Extensions.Logging;
using SharpBot.Agent;

namespace SharpBot.Tools;

/// <summary>
/// Aggregates multiple IToolHost backends behind a single IToolHost façade.
/// Used to combine built-in C# tools with MCP-backed tools so the LLM sees
/// a single flat catalog regardless of where each tool lives.
/// </summary>
public sealed class CompositeToolHost : IToolHost
{
    private readonly IReadOnlyList<IToolHost> _hosts;
    private readonly ILogger<CompositeToolHost> _logger;
    private Dictionary<string, IToolHost> _routes = new(StringComparer.Ordinal);
    private List<ToolDescriptor> _tools = new();

    public CompositeToolHost(IEnumerable<IToolHost> hosts, ILogger<CompositeToolHost> logger)
    {
        _hosts = hosts.ToList();
        _logger = logger;
    }

    public IReadOnlyList<ToolDescriptor> AvailableTools => _tools;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var host in _hosts)
        {
            await host.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        var aggregated = new List<ToolDescriptor>();
        var routes = new Dictionary<string, IToolHost>(StringComparer.Ordinal);

        foreach (var host in _hosts)
        {
            foreach (var tool in host.AvailableTools)
            {
                if (routes.ContainsKey(tool.Name))
                {
                    _logger.LogWarning(
                        "Duplicate tool name '{Name}' — keeping the first registration.", tool.Name);
                    continue;
                }
                aggregated.Add(tool);
                routes[tool.Name] = host;
            }
        }

        _tools = aggregated;
        _routes = routes;
        _logger.LogInformation(
            "CompositeToolHost ready: {ToolCount} tool(s) across {HostCount} host(s).",
            _tools.Count, _hosts.Count);
    }

    public Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        if (!_routes.TryGetValue(call.Name, out var host))
        {
            throw new InvalidOperationException(
                $"Unknown tool '{call.Name}'. Known: {string.Join(", ", _routes.Keys)}");
        }
        return host.ExecuteAsync(call, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var host in _hosts)
        {
            try { await host.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogWarning(ex, "Error disposing tool host"); }
        }
    }
}
