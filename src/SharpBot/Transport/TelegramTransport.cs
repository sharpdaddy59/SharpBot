using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Secrets;

namespace SharpBot.Transport;

public sealed class TelegramTransport : IChatTransport
{
    private readonly TelegramOptions _options;
    private readonly ISecretStore _secrets;
    private readonly ILogger<TelegramTransport> _logger;

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
        _ = _options;
        _ = _secrets;
        _ = _logger;
        await Task.Yield();
        throw new NotImplementedException(
            "TelegramTransport.IncomingMessagesAsync: connect Telegram.Bot client and yield updates.");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    public Task SendAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TelegramTransport.SendAsync: call Telegram.Bot SendMessage.");
    }
}
