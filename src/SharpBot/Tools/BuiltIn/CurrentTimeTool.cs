using System.Globalization;
using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

public sealed class CurrentTimeTool : IBuiltInTool
{
    public string Name => "current_time";

    public string Description =>
        "Returns the current date and time. Pass an optional IANA timezone name " +
        "(e.g. 'UTC', 'Asia/Tokyo', 'Europe/London', 'America/New_York', 'America/Los_Angeles'). " +
        "Omit the argument to get the system's local time.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "timezone": {
              "type": "string",
              "description": "Optional IANA timezone name. Examples: 'UTC', 'Asia/Tokyo', 'Europe/London', 'America/New_York'."
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
        else if (TryResolveTimeZone(tz, out var zone))
        {
            now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
            zoneLabel = zone.Id;
        }
        else
        {
            return Task.FromResult(
                $"Unknown timezone: '{tz}'. Use an IANA name like 'UTC', 'Asia/Tokyo', 'Europe/London', or 'America/New_York'.");
        }

        var iso = now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        var friendly = now.ToString("dddd, MMMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
        return Task.FromResult($"{friendly} ({zoneLabel}) — {iso}");
    }

    /// <summary>
    /// Accept an IANA or Windows timezone identifier and convert as needed. Falls back
    /// through alternate forms so the model doesn't have to know which flavor the host OS uses.
    /// </summary>
    private static bool TryResolveTimeZone(string tz, out TimeZoneInfo zone)
    {
        // Direct lookup handles both IANA (on Linux/macOS, and Windows with ICU) and Windows names.
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            return true;
        }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        // Try converting between IANA and Windows representations.
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(tz, out var winId))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(winId); return true; }
            catch { }
        }
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz, out var ianaId))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(ianaId); return true; }
            catch { }
        }

        zone = null!;
        return false;
    }
}
