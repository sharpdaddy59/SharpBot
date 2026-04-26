namespace SharpBot.Agent;

/// <summary>
/// Deterministic first-pass router that maps a user utterance directly to a tool call,
/// bypassing the LLM round-trip. Returns null when no fast-path matches; the caller
/// then falls through to the LLM-driven path. Designed to be cheap (regex-class work)
/// so it's safe to run on every incoming message.
/// </summary>
public interface IIntentRouter
{
    ToolCall? TryMatch(string userText);
}
