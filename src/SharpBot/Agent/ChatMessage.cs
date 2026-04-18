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
