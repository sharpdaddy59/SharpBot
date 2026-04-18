using System.Globalization;
using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

public sealed class CurrentTimeTool : IBuiltInTool
{
    public string Name => "current_time";
    public string Description =>
        "Returns the current date and time. Pass an optional IANA timezone name (e.g. 'America/New_York'). " +
        "Defaults to the system's local time.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "timezone": {
              "type": "string",
              "description": "Optional IANA timezone name (e.g. 'UTC', 'America/New_York', 'Europe/London')."
            }
          }
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string? tz = null;
        if (arguments.ValueKind == JsonValueKind.Object &&
            arguments.TryGetProperty("timezone", out var tzEl) &&
            tzEl.ValueKind == JsonValueKind.String)
        {
            tz = tzEl.GetString();
        }

        DateTimeOffset now;
        string zoneLabel;
        if (string.IsNullOrWhiteSpace(tz))
        {
            now = DateTimeOffset.Now;
            zoneLabel = TimeZoneInfo.Local.Id;
        }
        else
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
                now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
                zoneLabel = zone.Id;
            }
            catch (TimeZoneNotFoundException)
            {
                return Task.FromResult($"Unknown timezone: {tz}. Try 'UTC' or an IANA name like 'America/New_York'.");
            }
        }

        var iso = now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        var friendly = now.ToString("dddd, MMMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
        return Task.FromResult($"{friendly} ({zoneLabel}) — {iso}");
    }
}
