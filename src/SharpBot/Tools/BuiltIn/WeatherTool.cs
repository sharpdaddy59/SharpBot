using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

/// <summary>
/// Current weather via the free, no-key Open-Meteo APIs.
/// Geocoding: https://open-meteo.com/en/docs/geocoding-api
/// Forecast:  https://open-meteo.com/en/docs
/// </summary>
public sealed class WeatherTool : IBuiltInTool
{
    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    private readonly HttpClient _http;

    public WeatherTool(HttpClient http) => _http = http;

    public string Name => "weather";

    public string Description =>
        "Get the current weather for a location. Use this instead of fetching weather websites — " +
        "it returns clean data from Open-Meteo in one call. Accepts a city name ('Phoenix', 'London'), " +
        "a US zip code ('85001'), or 'city, country' ('Paris, France'). Returns temperature, conditions, " +
        "humidity, and wind.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "location": {
              "type": "string",
              "description": "Location: city name, US zip code, or 'city, country'."
            },
            "units": {
              "type": "string",
              "enum": ["imperial", "metric"],
              "description": "Unit system. Defaults to 'imperial' (°F, mph)."
            }
          },
          "required": ["location"]
        }
        """;

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("location", out var locEl) ||
            locEl.ValueKind != JsonValueKind.String)
        {
            return "Error: missing required 'location' string argument.";
        }
        var location = locEl.GetString()!;

        var imperial = true;
        if (arguments.TryGetProperty("units", out var unitsEl) && unitsEl.ValueKind == JsonValueKind.String)
        {
            imperial = !string.Equals(unitsEl.GetString(), "metric", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            var place = await GeocodeAsync(location, cancellationToken).ConfigureAwait(false);
            if (place is null)
            {
                return $"Could not find a location matching '{location}'. Try a full city name or 'city, country'.";
            }

            var weather = await FetchWeatherAsync(place.Value.Lat, place.Value.Lon, imperial, cancellationToken)
                .ConfigureAwait(false);
            return FormatReport(place.Value, weather, imperial);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "Error: weather request timed out.";
        }
        catch (HttpRequestException ex)
        {
            return $"Error: could not reach Open-Meteo ({ex.Message}).";
        }
    }

    private async Task<Place?> GeocodeAsync(string query, CancellationToken ct)
    {
        // Open-Meteo's geocoding matches against the city name only — a query like
        // "London, UK" or "Paris, France" fails because the API won't match the whole
        // comma-joined string. Try as-is first; if no hit and the query has a comma,
        // retry with just the part before the first comma.
        var place = await TryGeocodeOneAsync(query, ct).ConfigureAwait(false);
        if (place is not null) return place;

        var commaIdx = query.IndexOf(',');
        if (commaIdx > 0)
        {
            var primary = query[..commaIdx].Trim();
            if (primary.Length > 0 && !primary.Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                return await TryGeocodeOneAsync(primary, ct).ConfigureAwait(false);
            }
        }

        return null;
    }

    private async Task<Place?> TryGeocodeOneAsync(string query, CancellationToken ct)
    {
        var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(query)}&count=1&language=en&format=json";
        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("results", out var resultsEl)
            || resultsEl.ValueKind != JsonValueKind.Array
            || resultsEl.GetArrayLength() == 0)
        {
            return null;
        }

        var first = resultsEl[0];
        return new Place(
            Name: first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? query : query,
            Admin1: first.TryGetProperty("admin1", out var a1) ? a1.GetString() : null,
            Country: first.TryGetProperty("country", out var c) ? c.GetString() : null,
            Lat: first.GetProperty("latitude").GetDouble(),
            Lon: first.GetProperty("longitude").GetDouble());
    }

    private async Task<Weather> FetchWeatherAsync(double lat, double lon, bool imperial, CancellationToken ct)
    {
        var tempUnit = imperial ? "fahrenheit" : "celsius";
        var windUnit = imperial ? "mph" : "kmh";
        var url =
            $"{ForecastUrl}?latitude={lat.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={lon.ToString(CultureInfo.InvariantCulture)}" +
            "&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,wind_direction_10m" +
            $"&temperature_unit={tempUnit}&wind_speed_unit={windUnit}&timezone=auto";

        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var current = doc.RootElement.GetProperty("current");
        return new Weather(
            TemperatureActual: current.GetProperty("temperature_2m").GetDouble(),
            TemperatureFeels: current.GetProperty("apparent_temperature").GetDouble(),
            HumidityPct: current.GetProperty("relative_humidity_2m").GetDouble(),
            WindSpeed: current.GetProperty("wind_speed_10m").GetDouble(),
            WindDirectionDeg: current.GetProperty("wind_direction_10m").GetDouble(),
            WeatherCode: current.GetProperty("weather_code").GetInt32(),
            LocalTime: current.TryGetProperty("time", out var tEl) ? tEl.GetString() ?? "" : "");
    }

    private static string FormatReport(Place place, Weather w, bool imperial)
    {
        var tempUnit = imperial ? "°F" : "°C";
        var windUnit = imperial ? "mph" : "km/h";

        var placeName = string.Join(", ", new[] { place.Name, place.Admin1, place.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var sb = new StringBuilder();
        sb.Append(placeName).AppendLine();
        sb.Append(DescribeWeatherCode(w.WeatherCode)).AppendLine();
        sb.Append(CultureInfo.InvariantCulture,
            $"Temperature: {w.TemperatureActual:F0}{tempUnit} (feels like {w.TemperatureFeels:F0}{tempUnit})").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"Humidity: {w.HumidityPct:F0}%").AppendLine();
        sb.Append(CultureInfo.InvariantCulture,
            $"Wind: {w.WindSpeed:F0} {windUnit} from {DescribeWindDirection(w.WindDirectionDeg)}").AppendLine();
        if (!string.IsNullOrEmpty(w.LocalTime))
        {
            sb.Append("Local time: ").Append(w.LocalTime);
        }
        return sb.ToString().TrimEnd();
    }

    private static string DescribeWeatherCode(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 => "Light drizzle",
        53 => "Moderate drizzle",
        55 => "Dense drizzle",
        56 or 57 => "Freezing drizzle",
        61 => "Light rain",
        63 => "Moderate rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 => "Light snow",
        73 => "Moderate snow",
        75 => "Heavy snow",
        77 => "Snow grains",
        80 => "Light rain showers",
        81 => "Moderate rain showers",
        82 => "Violent rain showers",
        85 => "Light snow showers",
        86 => "Heavy snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => $"Weather code {code}",
    };

    private static string DescribeWindDirection(double degrees)
    {
        var dirs = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                           "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        var idx = (int)Math.Round(degrees / 22.5) % 16;
        if (idx < 0) idx += 16;
        return dirs[idx];
    }

    private readonly record struct Place(string Name, string? Admin1, string? Country, double Lat, double Lon);

    private readonly record struct Weather(
        double TemperatureActual,
        double TemperatureFeels,
        double HumidityPct,
        double WindSpeed,
        double WindDirectionDeg,
        int WeatherCode,
        string LocalTime);
}
