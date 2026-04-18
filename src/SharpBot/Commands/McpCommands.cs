using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Tools;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class McpListCommand : AsyncCommand<McpListCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly IToolHost _toolHost;
    private readonly SharpBotOptions _options;

    public McpListCommand(IToolHost toolHost, IOptions<SharpBotOptions> options)
    {
        _toolHost = toolHost;
        _options = options.Value;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]MCP servers[/]").LeftJustified());

        if (_options.Mcp.Servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No MCP servers configured.[/] Add one to [grey]data/user-config.json[/] or [grey]appsettings.json[/] under [grey]SharpBot:Mcp:Servers[/].");
            AnsiConsole.MarkupLine("See the README for examples.");
            return 0;
        }

        var serverTable = new Table().AddColumns("Name", "Command", "Args");
        foreach (var s in _options.Mcp.Servers)
        {
            serverTable.AddRow(
                Markup.Escape(s.Name),
                Markup.Escape(s.Command),
                Markup.Escape(string.Join(" ", s.Args)));
        }
        AnsiConsole.Write(serverTable);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Connecting…[/]").LeftJustified());

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Spawning MCP servers and listing tools...", async _ =>
                {
                    await _toolHost.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Initialization failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold]Tools ({_toolHost.AvailableTools.Count})[/]").LeftJustified());

        if (_toolHost.AvailableTools.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No tools discovered.[/] Check log output above for initialization errors.");
            return 0;
        }

        var toolTable = new Table().AddColumns("Name", "Description");
        foreach (var t in _toolHost.AvailableTools)
        {
            var desc = t.Description.Length > 80 ? t.Description[..77] + "..." : t.Description;
            toolTable.AddRow(Markup.Escape(t.Name), Markup.Escape(desc));
        }
        AnsiConsole.Write(toolTable);
        return 0;
    }
}

public sealed class McpTestCommand : AsyncCommand<McpTestCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<tool>")]
        public string Tool { get; set; } = "";

        [CommandArgument(1, "[arguments-json]")]
        public string? ArgumentsJson { get; set; }
    }

    private readonly IToolHost _toolHost;

    public McpTestCommand(IToolHost toolHost) => _toolHost = toolHost;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule($"[bold]MCP test: {Markup.Escape(settings.Tool)}[/]").LeftJustified());

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Connecting to MCP servers...", async _ =>
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
