using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpBot.Agent;

namespace SharpBot.Tools.BuiltIn;

public sealed class BuiltInToolHost : IToolHost
{
    public const string NamePrefix = "core.";

    private readonly IReadOnlyList<IBuiltInTool> _tools;
    private readonly ILogger<BuiltInToolHost> _logger;
    private readonly Dictionary<string, IBuiltInTool> _byQualifiedName;
    private readonly List<ToolDescriptor> _descriptors;

    public BuiltInToolHost(IEnumerable<IBuiltInTool> tools, ILogger<BuiltInToolHost> logger)
    {
        _tools = tools.ToList();
        _logger = logger;

        _byQualifiedName = new Dictionary<string, IBuiltInTool>(StringComparer.Ordinal);
        _descriptors = new List<ToolDescriptor>(_tools.Count);
        foreach (var t in _tools)
        {
            var qualified = NamePrefix + t.Name;
            _byQualifiedName[qualified] = t;
            _descriptors.Add(new ToolDescriptor(qualified, t.Description, t.ParametersJsonSchema));
        }
    }

    public IReadOnlyList<ToolDescriptor> AvailableTools => _descriptors;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BuiltInToolHost ready: {Count} tool(s)", _tools.Count);
        return Task.CompletedTask;
    }

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        if (!_byQualifiedName.TryGetValue(call.Name, out var tool))
        {
            throw new InvalidOperationException(
                $"Unknown built-in tool '{call.Name}'. Known: {string.Join(", ", _byQualifiedName.Keys)}");
        }

        JsonElement args;
        if (string.IsNullOrWhiteSpace(call.ArgumentsJson))
        {
            args = JsonDocument.Parse("{}").RootElement;
        }
        else
        {
            try
            {
                args = JsonDocument.Parse(call.ArgumentsJson).RootElement;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Tool '{call.Name}' arguments are not valid JSON: {ex.Message}", ex);
            }
        }

        return await tool.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
