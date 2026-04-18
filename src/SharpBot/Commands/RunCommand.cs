using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly AgentLoop _agent;
    private readonly SharpBotOptions _options;
    private readonly ILogger<RunCommand> _logger;

    public RunCommand(AgentLoop agent, IOptions<SharpBotOptions> options, ILogger<RunCommand> logger)
    {
        _agent = agent;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold green]SharpBot[/]").LeftJustified());
        AnsiConsole.MarkupLine($"Model:   [grey]{Markup.Escape(_options.Llm.ModelPath)}[/]");
        AnsiConsole.MarkupLine($"Allowed: [grey]{_options.Telegram.AllowedUserIds.Count} Telegram user(s)[/]");
        AnsiConsole.MarkupLine($"MCP:     [grey]{_options.Mcp.Servers.Count} server(s) configured[/]");
        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to stop.[/]");
        AnsiConsole.WriteLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await _agent.RunAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Shutdown requested.[/]");
            return 0;
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "A component is not yet implemented");
            AnsiConsole.MarkupLine($"[red]Not implemented yet:[/] {ex.Message}");
            return 2;
        }
    }
}
