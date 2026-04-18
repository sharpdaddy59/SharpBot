namespace SharpBot.Transport;

public interface IChatTransport
{
    IAsyncEnumerable<IncomingMessage> IncomingMessagesAsync(CancellationToken cancellationToken);
    Task SendAsync(string chatId, string text, CancellationToken cancellationToken = default);
}

public sealed record IncomingMessage(
    string ChatId,
    string SenderId,
    string SenderDisplay,
    string Text,
    DateTimeOffset Timestamp);
