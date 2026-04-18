using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using SharpBot.Secrets;

namespace SharpBot.Setup;

public sealed class HuggingFaceClient
{
    private readonly HttpClient _http;
    private readonly ISecretStore _secrets;

    public HuggingFaceClient(HttpClient http, ISecretStore secrets)
    {
        _http = http;
        _secrets = secrets;
    }

    public bool HasToken => !string.IsNullOrWhiteSpace(_secrets.Get(SecretKeys.HuggingFaceToken));

    public async Task DownloadAsync(
        string repo,
        string filename,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://huggingface.co/{repo}/resolve/main/{Uri.EscapeDataString(filename)}?download=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = _secrets.Get(SecretKeys.HuggingFaceToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new HuggingFaceAuthException(repo, response.StatusCode, !string.IsNullOrWhiteSpace(token));
        }
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var tempPath = destinationPath + ".partial";

        try
        {
            await using (var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long bytesRead = 0;
                long lastReportedBytes = 0;
                var stopwatch = Stopwatch.StartNew();
                var lastReportTime = stopwatch.Elapsed;

                int read;
                while ((read = await networkStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    bytesRead += read;

                    var now = stopwatch.Elapsed;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    var isDone = totalBytes.HasValue && bytesRead == totalBytes.Value;
                    if (elapsed >= 0.1 || isDone)
                    {
                        var bps = elapsed > 0 ? (bytesRead - lastReportedBytes) / elapsed : 0;
                        progress?.Report(new DownloadProgress(bytesRead, totalBytes, bps));
                        lastReportedBytes = bytesRead;
                        lastReportTime = now;
                    }
                }
            }

            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            File.Move(tempPath, destinationPath);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://huggingface.co/api/whoami-v2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }
}

public sealed record DownloadProgress(long BytesRead, long? TotalBytes, double BytesPerSecond);

public sealed class HuggingFaceAuthException : Exception
{
    public string Repo { get; }
    public HttpStatusCode StatusCode { get; }
    public bool HadToken { get; }

    public HuggingFaceAuthException(string repo, HttpStatusCode statusCode, bool hadToken)
        : base($"HuggingFace returned {(int)statusCode} for repo '{repo}'.")
    {
        Repo = repo;
        StatusCode = statusCode;
        HadToken = hadToken;
    }
}
