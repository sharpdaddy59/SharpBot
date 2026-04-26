namespace SharpBot.Agent;

public enum ChatRole { System, User, Assistant, Tool }

public sealed record ChatMessage(
    ChatRole Role,
    string Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    string? ToolCallId = null);

public sealed record ToolCall(string Id, string Name, string ArgumentsJson);

public sealed record ToolDescriptor(string Name, string Description, string ParametersJsonSchema);

public sealed record LlmResponse(string? Text, IReadOnlyList<ToolCall> ToolCalls)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <summary>
/// One frame of a streaming inference. Either a chunk of user-visible text the model
/// has produced so far (TextDelta), or the terminal Final event carrying the complete
/// parsed response. Tool-call markup is buffered internally and never surfaces as a
/// TextDelta — callers can render deltas straight to the user.
/// </summary>
public sealed record LlmStreamEvent
{
    private LlmStreamEvent() { }

    public string? TextDelta { get; private init; }
    public LlmResponse? Final { get; private init; }

    public static LlmStreamEvent Delta(string text) => new() { TextDelta = text };
    public static LlmStreamEvent Done(LlmResponse response) => new() { Final = response };
}
