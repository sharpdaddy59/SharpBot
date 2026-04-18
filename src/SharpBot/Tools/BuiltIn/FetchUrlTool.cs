using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SharpBot.Config;

namespace SharpBot.Tools.BuiltIn;

public sealed class FetchUrlTool : IBuiltInTool
{
    private readonly HttpClient _http;
    private readonly BuiltInToolsOptions _options;

    public FetchUrlTool(HttpClient http, IOptions<SharpBotOptions> options)
    {
        _http = http;
        _options = options.Value.BuiltInTools;
    }

    public string Name => "fetch_url";
    public string Description =>
        "GET an http/https URL and return the response body as text. Truncated if the response is very large. " +
        "Use this to look up information on public web pages.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "url": { "type": "string", "description": "Absolute http/https URL to fetch." }
          },
          "required": ["url"]
        }
        """;

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("url", out var urlEl) ||
            urlEl.ValueKind != JsonValueKind.String)
        {
            return "Error: missing required 'url' string argument.";
        }

        var url = urlEl.GetString()!;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return $"Error: '{url}' is not a valid absolute URL.";
        }
        if (uri.Scheme is not ("http" or "https"))
        {
            return $"Error: only http/https URLs are allowed (got {uri.Scheme}).";
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.FetchTimeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("SharpBot/0.2 (+https://github.com/sharpdaddy59/SharpBot)");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html", 0.9));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 1.0));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.1));

        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase ?? ""} for {uri}";
            }

            var buffer = new byte[_options.FetchMaxBytes];
            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var total = 0;
            int read;
            while (total < buffer.Length &&
                   (read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cts.Token).ConfigureAwait(false)) > 0)
            {
                total += read;
            }

            var truncated = false;
            if (total == buffer.Length)
            {
                // Check if there's more
                var probe = new byte[1];
                var extra = await stream.ReadAsync(probe.AsMemory(0, 1), cts.Token).ConfigureAwait(false);
                if (extra > 0) truncated = true;
            }

            var encoding = DetectEncoding(response.Content.Headers.ContentType?.CharSet);
            var text = encoding.GetString(buffer, 0, total);

            if (truncated)
            {
                text += $"\n\n[...truncated at {_options.FetchMaxBytes} bytes]";
            }
            return text;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return $"Error: fetch timed out after {_options.FetchTimeoutSeconds}s.";
        }
        catch (HttpRequestException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static Encoding DetectEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset)) return Encoding.UTF8;
        try { return Encoding.GetEncoding(charset.Trim('"')); }
        catch { return Encoding.UTF8; }
    }
}
