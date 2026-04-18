using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Secrets;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class DoctorCommand : Command<DoctorCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly SharpBotOptions _options;
    private readonly ISecretStore _secrets;

    public DoctorCommand(IOptions<SharpBotOptions> options, ISecretStore secrets)
    {
        _options = options.Value;
        _secrets = secrets;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]SharpBot doctor[/]").LeftJustified());

        var table = new Table().AddColumns("Check", "Status", "Detail");

        Add(table, "Data directory",
            Directory.Exists(_options.DataDirectory),
            _options.DataDirectory);

        Add(table, "Model file",
            File.Exists(_options.Llm.ModelPath),
            _options.Llm.ModelPath);

        Add(table, "Telegram token",
            !string.IsNullOrWhiteSpace(_secrets.Get(SecretKeys.TelegramBotToken)),
            "stored in data/secrets.json");

        Add(table, "Allowed Telegram users",
            _options.Telegram.AllowedUserIds.Count > 0,
            $"{_options.Telegram.AllowedUserIds.Count} user(s) — run [green]sharpbot pair[/] to add");

        Add(table, "MCP servers",
            _options.Mcp.Servers.Count > 0,
            $"{_options.Mcp.Servers.Count} configured");

        AnsiConsole.Write(table);
        return 0;
    }

    private static void Add(Table table, string label, bool ok, string detail)
    {
        var status = ok ? "[green]OK[/]" : "[red]MISSING[/]";
        table.AddRow(label, status, detail);
    }
}
