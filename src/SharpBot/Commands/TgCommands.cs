using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Secrets;
using Spectre.Console;
using Spectre.Console.Cli;
using Telegram.Bot;

namespace SharpBot.Commands;

public sealed class TgLoginCommand : AsyncCommand<TgLoginCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[token]")]
        public string? Token { get; set; }
    }

    private readonly ISecretStore _secrets;

    public TgLoginCommand(ISecretStore secrets) => _secrets = secrets;

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]Telegram login[/]").LeftJustified());
        AnsiConsole.MarkupLine("Create a bot and get a token:");
        AnsiConsole.MarkupLine("  1. Open Telegram, message [blue]@BotFather[/].");
        AnsiConsole.MarkupLine("  2. Send [yellow]/newbot[/]. Pick a display name and a username (must end in [grey]bot[/]).");
        AnsiConsole.MarkupLine("  3. BotFather replies with a token like [grey]123456:ABC-DEF...[/] — paste it below.");
        AnsiConsole.WriteLine();

        var token = settings.Token ?? AnsiConsole.Prompt(
            new TextPrompt<string>("Bot token:")
                .Secret()
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error("Token cannot be empty.")
                    : ValidationResult.Success()));

        AnsiConsole.MarkupLine("[grey]Validating with Telegram…[/]");
        try
        {
            var bot = new TelegramBotClient(token);
            var me = await bot.GetMe(cancellationToken).ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[green]OK.[/] Bot: [green]@{me.Username}[/] ({me.FirstName})");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Telegram rejected the token:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        _secrets.Set(SecretKeys.TelegramBotToken, token);
        _secrets.Save();
        AnsiConsole.MarkupLine("[green]Token saved.[/] Next: run [green]sharpbot pair[/] to authorize your Telegram user.");
        return 0;
    }
}

public sealed class TgLogoutCommand : Command<TgLogoutCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ISecretStore _secrets;
    public TgLogoutCommand(ISecretStore secrets) => _secrets = secrets;

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        _secrets.Delete(SecretKeys.TelegramBotToken);
        _secrets.Save();
        AnsiConsole.MarkupLine("[green]Telegram bot token removed.[/]");
        return 0;
    }
}

public sealed class TgStatusCommand : Command<TgStatusCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ISecretStore _secrets;
    private readonly SharpBotOptions _options;

    public TgStatusCommand(ISecretStore secrets, IOptions<SharpBotOptions> options)
    {
        _secrets = secrets;
        _options = options.Value;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var token = _secrets.Get(SecretKeys.TelegramBotToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            AnsiConsole.MarkupLine("[yellow]No Telegram bot token saved.[/] Run [green]sharpbot tg login[/] to add one.");
            return 0;
        }

        var masked = token.Length > 8 ? $"{token[..4]}…{token[^4..]}" : "****";
        AnsiConsole.MarkupLine($"Token: [green]saved[/] [grey]({masked})[/]");

        var ids = _options.Telegram.AllowedUserIds;
        if (ids.Count == 0)
        {
            AnsiConsole.MarkupLine("Paired users: [yellow]none[/] — run [green]sharpbot pair[/] to authorize yourself.");
        }
        else
        {
            AnsiConsole.MarkupLine($"Paired users: [green]{ids.Count}[/] [grey]({string.Join(", ", ids)})[/]");
        }
        return 0;
    }
}
