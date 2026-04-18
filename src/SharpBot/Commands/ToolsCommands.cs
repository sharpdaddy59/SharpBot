using SharpBot.Agent;
using SharpBot.Tools;
using SharpBot.Tools.BuiltIn;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class ToolsListCommand : AsyncCommand<ToolsListCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly IToolHost _toolHost;

    public ToolsListCommand(IToolHost toolHost) => _toolHost = toolHost;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]Available tools[/]").LeftJustified());

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Initializing tool hosts…", async _ =>
                {
                    await _toolHost.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Initialization failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        if (_toolHost.AvailableTools.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No tools available.[/] Built-in tools should always register; MCP servers may need configuring.");
            return 0;
        }

        var table = new Table().AddColumns("Source", "Name", "Description");
        foreach (var tool in _toolHost.AvailableTools)
        {
            var source = tool.Name.StartsWith(BuiltInToolHost.NamePrefix, StringComparison.Ordinal)
                ? "[green]built-in[/]"
                : "[blue]mcp[/]";
            var desc = tool.Description.Length > 80 ? tool.Description[..77] + "..." : tool.Description;
            table.AddRow(source, Markup.Escape(tool.Name), Markup.Escape(desc));
        }
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{_toolHost.AvailableTools.Count} tool(s) total.[/]");
        return 0;
    }
}

public sealed class ToolsTestCommand : AsyncCommand<ToolsTestCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<tool>")]
        public string Tool { get; set; } = "";

        [CommandArgument(1, "[arguments-json]")]
        public string? ArgumentsJson { get; set; }
    }

    private readonly IToolHost _toolHost;

    public ToolsTestCommand(IToolHost toolHost) => _toolHost = toolHost;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule($"[bold]Tool test: {Markup.Escape(settings.Tool)}[/]").LeftJustified());

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Initializing tool hosts…", async _ =>
                {
                    await _toolHost.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Initialization failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        var argsJson = settings.ArgumentsJson ?? "{}";
        AnsiConsole.MarkupLine($"[grey]Arguments:[/] {Markup.Escape(argsJson)}");
        AnsiConsole.WriteLine();

        try
        {
            var call = new ToolCall(Id: "test", Name: settings.Tool, ArgumentsJson: argsJson);
            var result = await _toolHost.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
            AnsiConsole.Write(new Panel(Markup.Escape(result)).Header("Result"));
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Tool call failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
