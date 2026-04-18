using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Secrets;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SharpBot.Transport;

public sealed class TelegramTransport : IChatTransport
{
    private readonly TelegramOptions _options;
    private readonly ISecretStore _secrets;
    private readonly ILogger<TelegramTransport> _logger;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private TelegramBotClient? _client;

    public TelegramTransport(
        IOptions<SharpBotOptions> options,
        ISecretStore secrets,
        ILogger<TelegramTransport> logger)
    {
        _options = options.Value.Telegram;
        _secrets = secrets;
        _logger = logger;
    }

    public async IAsyncEnumerable<IncomingMessage> IncomingMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var bot = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);
        var me = await bot.GetMe(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Telegram transport started as @{Username} ({Id})", me.Username, me.Id);

        if (_options.AllowedUserIds.Count == 0)
        {
            _logger.LogWarning(
                "No allowed Telegram user IDs configured — ALL incoming messages will be ignored. " +
                "Run 'sharpbot pair' to authorize a user.");
        }

        var offset = 0;
        var timeoutSeconds = Math.Max(1, _options.PollingTimeoutSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            Update[] updates;
            try
            {
                updates = await bot.GetUpdates(
                    offset: offset,
                    timeout: timeoutSeconds,
                    allowedUpdates: new[] { UpdateType.Message },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram GetUpdates failed; retrying in 5s");
                try { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { yield break; }
                continue;
            }

            foreach (var update in updates)
            {
                offset = update.Id + 1;
                var message = update.Message;
                if (message is null || message.Text is null || message.From is null) continue;

                if (_options.AllowedUserIds.Count > 0 && !_options.AllowedUserIds.Contains(message.From.Id))
                {
                    _logger.LogWarning(
                        "Ignored message from non-allowed user {UserId} (@{Username})",
                        message.From.Id, message.From.Username ?? "?");
                    continue;
                }

                yield return new IncomingMessage(
                    ChatId: message.Chat.Id.ToString(CultureInfo.InvariantCulture),
                    SenderId: message.From.Id.ToString(CultureInfo.InvariantCulture),
                    SenderDisplay: FormatDisplay(message.From),
                    Text: message.Text,
                    Timestamp: message.Date);
            }
        }
    }

    public async Task SendAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        var bot = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);
        var id = long.Parse(chatId, CultureInfo.InvariantCulture);

        // Telegram enforces a 4096-character limit per message — chunk on a paragraph boundary where possible.
        const int limit = 4000;
        if (text.Length <= limit)
        {
            await bot.SendMessage(id, text, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        foreach (var chunk in Chunk(text, limit))
        {
            await bot.SendMessage(id, chunk, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TelegramBotClient> GetOrCreateClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null) return _client;
        await _clientLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null) return _client;
            var token = _secrets.Get(SecretKeys.TelegramBotToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "No Telegram bot token saved. Run 'sharpbot tg login' to add one.");
            }
            _client = new TelegramBotClient(token);
            return _client;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    private static string FormatDisplay(Telegram.Bot.Types.User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Username)) return "@" + user.Username;
        if (!string.IsNullOrWhiteSpace(user.FirstName)) return user.FirstName!;
        return user.Id.ToString(CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string> Chunk(string text, int limit)
    {
        var start = 0;
        while (start < text.Length)
        {
            var remaining = text.Length - start;
            var take = Math.Min(limit, remaining);

            if (take < remaining)
            {
                // Prefer splitting at the nearest paragraph/line/space boundary within the window.
                var end = start + take;
                var split = FindSplit(text, start, end);
                if (split > start) take = split - start;
            }

            yield return text.Substring(start, take);
            start += take;
        }
    }

    private static int FindSplit(string text, int start, int end)
    {
        var idx = text.LastIndexOf("\n\n", end - 1, end - start, StringComparison.Ordinal);
        if (idx > start) return idx + 2;
        idx = text.LastIndexOf('\n', end - 1, end - start);
        if (idx > start) return idx + 1;
        idx = text.LastIndexOf(' ', end - 1, end - start);
        if (idx > start) return idx + 1;
        return end;
    }
}
