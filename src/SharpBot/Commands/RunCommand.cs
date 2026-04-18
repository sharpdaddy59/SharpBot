using Microsoft.Extensions.Logging;
using SharpBot.Agent;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class RunCommand : AsyncCommand<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly AgentLoop _agent;
    private readonly ILogger<RunCommand> _logger;

    public RunCommand(AgentLoop agent, ILogger<RunCommand> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[green]SharpBot[/] starting — press Ctrl+C to stop.");

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
