using System.Text.RegularExpressions;

namespace SharpBot.Agent;

/// <summary>
/// Regex-based intent router. Matches a small set of common, unambiguous utterances
/// to a fixed tool call. Patterns are intentionally narrow: a false positive sends a
/// confidently wrong response, while a miss just falls through to the LLM. Bias toward
/// missing rather than guessing.
/// </summary>
public sealed partial class RegexIntentRouter : IIntentRouter
{
    private static readonly Regex TimeRegex = BuildTimeRegex();
    private static readonly Regex DateRegex = BuildDateRegex();

    public ToolCall? TryMatch(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return null;

        var trimmed = userText.Trim();

        if (TimeRegex.IsMatch(trimmed) || DateRegex.IsMatch(trimmed))
        {
            return new ToolCall(
                Id: Guid.NewGuid().ToString("N"),
                Name: "core.current_time",
                ArgumentsJson: "{}");
        }

        return null;
    }

    [GeneratedRegex(
        @"^(?:what(?:'s| is)\s+(?:the\s+)?(?:current\s+)?time|what\s+time\s+is\s+it|current\s+time|got\s+the\s+time)\??$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BuildTimeRegex();

    [GeneratedRegex(
        @"^(?:what(?:'s| is)\s+(?:the\s+)?(?:current\s+)?date|today'?s\s+date|what\s+(?:day|date)\s+is\s+(?:it|today))\??$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BuildDateRegex();
}
