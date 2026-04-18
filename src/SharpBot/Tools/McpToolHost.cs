using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SharpBot.Agent;
using SharpBot.Config;

namespace SharpBot.Tools;

public sealed class McpToolHost : IToolHost
{
    private const char NameSeparator = '.';

    private readonly McpOptions _options;
    private readonly ILogger<McpToolHost> _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly Dictionary<string, McpClient> _clients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string ServerName, string ToolName)> _toolIndex = new(StringComparer.Ordinal);
    private List<ToolDescriptor> _tools = new();
    private bool _initialized;

    public McpToolHost(
        IOptions<SharpBotOptions> options,
        ILogger<McpToolHost> logger,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value.Mcp;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public IReadOnlyList<ToolDescriptor> AvailableTools => _tools;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;
        _initialized = true;

        if (_options.Servers.Count == 0)
        {
            _logger.LogInformation("McpToolHost: no servers configured.");
            return;
        }

        var aggregated = new List<ToolDescriptor>();

        foreach (var server in _options.Servers)
        {
            if (string.IsNullOrWhiteSpace(server.Name))
            {
                _logger.LogWarning("Skipping MCP server with empty Name.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(server.Command))
            {
                _logger.LogWarning("Skipping MCP server '{Name}' with empty Command.", server.Name);
                continue;
            }
            if (server.Name.Contains(NameSeparator))
            {
                _logger.LogWarning(
                    "Skipping MCP server with name '{Name}' — names may not contain '{Sep}' (used as a tool-name separator).",
                    server.Name, NameSeparator);
                continue;
            }
            if (_clients.ContainsKey(server.Name))
            {
                _logger.LogWarning("Duplicate MCP server name '{Name}' — skipping second occurrence.", server.Name);
                continue;
            }

            try
            {
                var client = await ConnectAsync(server, cancellationToken).ConfigureAwait(false);
                _clients[server.Name] = client;

                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var tool in tools)
                {
                    var qualifiedName = $"{server.Name}{NameSeparator}{tool.Name}";
                    _toolIndex[qualifiedName] = (server.Name, tool.Name);
                    aggregated.Add(new ToolDescriptor(
                        Name: qualifiedName,
                        Description: tool.Description ?? string.Empty,
                        ParametersJsonSchema: tool.JsonSchema.GetRawText()));
                }

                _logger.LogInformation("MCP server '{Name}': {Count} tool(s)", server.Name, tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to initialize MCP server '{Name}' ({Command}). Skipping.",
                    server.Name, server.Command);
            }
        }

        _tools = aggregated;
        _logger.LogInformation("McpToolHost initialized: {ServerCount} server(s), {ToolCount} tool(s).",
            _clients.Count, _tools.Count);
    }

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        if (!_toolIndex.TryGetValue(call.Name, out var route))
        {
            throw new InvalidOperationException(
                $"Unknown tool '{call.Name}'. Known tools: {string.Join(", ", _toolIndex.Keys)}");
        }
        if (!_clients.TryGetValue(route.ServerName, out var client))
        {
            throw new InvalidOperationException($"MCP server '{route.ServerName}' is not connected.");
        }

        IReadOnlyDictionary<string, object?>? arguments = null;
        if (!string.IsNullOrWhiteSpace(call.ArgumentsJson))
        {
            try
            {
                arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(call.ArgumentsJson);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Tool '{call.Name}' arguments are not valid JSON: {ex.Message}", ex);
            }
        }

        var result = await client.CallToolAsync(
            toolName: route.ToolName,
            arguments: arguments,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return FormatResult(result);
    }

    private Task<McpClient> ConnectAsync(McpServerConfig server, CancellationToken cancellationToken)
    {
        var transportOptions = new StdioClientTransportOptions
        {
            Name = server.Name,
            Command = server.Command,
            Arguments = server.Args,
        };
        if (server.Env.Count > 0)
        {
            transportOptions.EnvironmentVariables = server.Env.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);
        }

        var transport = new StdioClientTransport(transportOptions, _loggerFactory);
        return McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
    }

    private static string FormatResult(CallToolResult result)
    {
        if (result.Content is null || result.Content.Count == 0)
        {
            return result.IsError == true ? "(tool reported error with no content)" : "(empty)";
        }

        var sb = new StringBuilder();
        foreach (var block in result.Content)
        {
            if (block is TextContentBlock text)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(text.Text);
            }
            else
            {
                // Non-text content (image, audio, embedded resource) — summarize so the LLM sees something useful.
                if (sb.Length > 0) sb.AppendLine();
                sb.Append($"[non-text content: {block.GetType().Name}]");
            }
        }
        var output = sb.ToString();
        return result.IsError == true ? $"[error] {output}" : output;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (name, client) in _clients)
        {
            try { await client.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogWarning(ex, "Error disposing MCP client '{Name}'", name); }
        }
        _clients.Clear();
        _toolIndex.Clear();
        _tools = new List<ToolDescriptor>();
    }
}
