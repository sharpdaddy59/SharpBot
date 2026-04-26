using SharpBot.Agent;
using SharpBot.Llm;

namespace SharpBot.Tests;

public class RenderMessageTests
{
    [Fact]
    public void Empty_assistant_message_still_renders_so_kv_cache_stays_in_sync()
    {
        var msg = new ChatMessage(ChatRole.Assistant, string.Empty);
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();

        Assert.Single(rendered);
        Assert.Equal("assistant", rendered[0].Role);
        Assert.Equal(string.Empty, rendered[0].Content);
    }

    [Fact]
    public void Assistant_with_text_and_no_tool_calls_renders_text()
    {
        var msg = new ChatMessage(ChatRole.Assistant, "Hello there.");
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();

        Assert.Single(rendered);
        Assert.Equal("assistant", rendered[0].Role);
        Assert.Equal("Hello there.", rendered[0].Content);
    }

    [Fact]
    public void Assistant_with_tool_calls_renders_tool_call_format()
    {
        var msg = new ChatMessage(
            ChatRole.Assistant,
            "Let me check that.",
            new[] { new ToolCall("id1", "core.current_time", "{}") });
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();

        Assert.Single(rendered);
        Assert.Equal("assistant", rendered[0].Role);
        Assert.Contains("<tool_call>", rendered[0].Content);
        Assert.Contains("core.current_time", rendered[0].Content);
        Assert.Contains("Let me check that.", rendered[0].Content);
    }

    [Fact]
    public void Empty_assistant_with_tool_calls_renders_just_the_tool_call()
    {
        var msg = new ChatMessage(
            ChatRole.Assistant,
            string.Empty,
            new[] { new ToolCall("id1", "core.current_time", "{}") });
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();

        Assert.Single(rendered);
        Assert.Equal("assistant", rendered[0].Role);
        Assert.Contains("<tool_call>", rendered[0].Content);
        Assert.Contains("core.current_time", rendered[0].Content);
    }

    [Fact]
    public void Empty_user_message_is_skipped_as_caller_bug_defense()
    {
        var msg = new ChatMessage(ChatRole.User, string.Empty);
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();
        Assert.Empty(rendered);
    }

    [Fact]
    public void Empty_system_message_is_skipped_as_caller_bug_defense()
    {
        var msg = new ChatMessage(ChatRole.System, string.Empty);
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();
        Assert.Empty(rendered);
    }

    [Fact]
    public void Tool_message_with_empty_content_still_renders()
    {
        var msg = new ChatMessage(ChatRole.Tool, string.Empty, ToolCallId: "id1");
        var rendered = LlamaSharpClient.RenderMessage(msg).ToList();
        Assert.Single(rendered);
        Assert.Equal("tool", rendered[0].Role);
    }
}
