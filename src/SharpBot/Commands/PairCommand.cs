using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Secrets;
using Spectre.Console;
using Spectre.Console.Cli;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SharpBot.Commands;

public sealed class PairCommand : AsyncCommand<PairCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ISecretStore _secrets;
    private readonly ConfigWriter _configWriter;
    private readonly SharpBotOptions _options;

    public PairCommand(ISecretStore secrets, ConfigWriter configWriter, IOptions<SharpBotOptions> options)
    {
        _secrets = secrets;
        _configWriter = configWriter;
        _options = options.Value;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]Telegram pairing[/]").LeftJustified());

        var token = _secrets.Get(SecretKeys.TelegramBotToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            AnsiConsole.MarkupLine("[red]No Telegram bot token saved.[/] Run [green]sharpbot tg login[/] first.");
            return 1;
        }

        var bot = new TelegramBotClient(token);
        Telegram.Bot.Types.User me;
        try
        {
            me = await bot.GetMe(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Could not reach Telegram:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        AnsiConsole.MarkupLine($"Bot: [green]@{me.Username}[/] ({me.FirstName})");
        AnsiConsole.WriteLine();

        // Drain the update queue FIRST, before telling the user to send a message —
        // otherwise a fast user can race the priming call and their message gets skipped.
        var offset = 0;
        try
        {
            var priming = await bot.GetUpdates(offset: -1, limit: 1, timeout: 0, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (priming.Length > 0) offset = priming[^1].Id + 1;
        }
        catch (OperationCanceledException) { return 130; }

        AnsiConsole.MarkupLine($"Open Telegram, find [blue]@{me.Username}[/], and send it any message.");
        AnsiConsole.MarkupLine("[grey]The sender of the first incoming message will be the candidate. Ctrl+C to cancel.[/]");
        AnsiConsole.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Telegram.Bot.Types.Update[] updates;
            try
            {
                updates = await bot.GetUpdates(
                    offset: offset,
                    timeout: 30,
                    allowedUpdates: new[] { UpdateType.Message },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return 130; }

            foreach (var update in updates)
            {
                offset = update.Id + 1;
                var msg = update.Message;
                if (msg?.From is null) continue;

                var user = msg.From;
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"Incoming from [green]{Markup.Escape(user.FirstName ?? "?")}[/] " +
                    $"[grey]@{Markup.Escape(user.Username ?? "-")}[/] [grey](id {user.Id})[/]: " +
                    $"{Markup.Escape(msg.Text ?? "(non-text)")}");

                if (!AnsiConsole.Confirm("Pair this user?", defaultValue: true))
                {
                    AnsiConsole.MarkupLine("[yellow]Skipped. Waiting for next message…[/]");
                    continue;
                }

                var combined = new HashSet<long>(_options.Telegram.AllowedUserIds) { user.Id };
                _configWriter.SetAllowedUserIds(combined);

                try
                {
                    await bot.SendMessage(msg.Chat.Id,
                        $"✅ Paired with SharpBot. You can now send me messages.",
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Saved locally but couldn't send confirmation:[/] {Markup.Escape(ex.Message)}");
                }

                AnsiConsole.MarkupLine($"[green]Paired.[/] Allowed user IDs: [grey]{string.Join(", ", combined)}[/]");
                AnsiConsole.MarkupLine($"[grey]Updated {_configWriter.FilePath}[/]");
                AnsiConsole.MarkupLine("Next: [green]sharpbot run[/] to start the bot.");
                return 0;
            }
        }

        return 130;
    }
}
