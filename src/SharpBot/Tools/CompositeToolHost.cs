using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Tools;

/// <summary>
/// Aggregates multiple IToolHost backends behind a single IToolHost façade.
/// Used to combine built-in C# tools with MCP-backed tools so the LLM sees
/// a single flat catalog regardless of where each tool lives.
///
/// Also enforces a host-level cap on tool result size — tools that don't apply
/// their own limits (notably MCP-backed ones) can't bloat the conversation prefix.
/// </summary>
public sealed class CompositeToolHost : IToolHost
{
    private readonly IReadOnlyList<IToolHost> _hosts;
    private readonly ILogger<CompositeToolHost> _logger;
    private readonly int _maxResultBytes;
    private Dictionary<string, IToolHost> _routes = new(StringComparer.Ordinal);
    private List<ToolDescriptor> _tools = new();

    public CompositeToolHost(
        IEnumerable<IToolHost> hosts,
        IOptions<SharpBotOptions> options,
        ILogger<CompositeToolHost> logger)
    {
        _hosts = hosts.ToList();
        _logger = logger;
        _maxResultBytes = options.Value.ToolHost.MaxResultBytes;
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

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        if (!_routes.TryGetValue(call.Name, out var host))
        {
            throw new InvalidOperationException(
                $"Unknown tool '{call.Name}'. Known: {string.Join(", ", _routes.Keys)}");
        }

        var result = await host.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
        return Truncate(result, call.Name);
    }

    /// <summary>
    /// Caps oversized tool output. Keeps both the head and tail with a marker
    /// in the middle — JSON results often have important metadata at the end
    /// (totals, status, pagination cursors), so dropping just the tail loses signal.
    /// The marker is part of the returned string so the model can reason about
    /// the truncation rather than being fed silently-shortened data.
    /// </summary>
    internal string Truncate(string result, string toolName)
    {
        if (_maxResultBytes <= 0 || result.Length <= _maxResultBytes) return result;

        var marker = $"\n\n[... truncated {result.Length - _maxResultBytes} bytes from tool '{toolName}'; use a more specific query to see more ...]\n\n";
        var keepBudget = _maxResultBytes - marker.Length;
        if (keepBudget <= 0)
        {
            // Pathological: the marker itself is bigger than the cap. Return only the marker.
            return marker.Trim();
        }

        var keepEach = keepBudget / 2;
        var truncated = result[..keepEach] + marker + result[^(keepBudget - keepEach)..];

        _logger.LogWarning(
            "Tool {Tool} returned {Bytes} bytes; truncated to {Limit}. " +
            "Consider adding a tool-side cap or refining the query.",
            toolName, result.Length, _maxResultBytes);

        return truncated;
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
