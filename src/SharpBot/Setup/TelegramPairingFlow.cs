namespace SharpBot.Setup;

public sealed class TelegramPairingFlow
{
    public Task<PairingResult> RunAsync(string botToken, CancellationToken cancellationToken = default)
    {
        _ = botToken;
        _ = cancellationToken;
        throw new NotImplementedException(
            "TelegramPairingFlow.RunAsync: validate token via getMe, poll updates, return first sender's user info.");
    }
}

public sealed record PairingResult(long UserId, string Username, string DisplayName);
