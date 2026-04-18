using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class PairCommand : AsyncCommand<PairCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]Telegram pairing[/]").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Stub — pairing flow not implemented yet.[/]");
        AnsiConsole.MarkupLine("Planned: connect to Telegram, wait for first message, capture sender's user ID, save to config.");
        return Task.FromResult(0);
    }
}
