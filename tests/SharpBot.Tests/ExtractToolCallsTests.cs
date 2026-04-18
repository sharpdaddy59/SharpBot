using SharpBot.Llm;

namespace SharpBot.Tests;

public class ExtractToolCallsTests
{
    [Fact]
    public void Returns_raw_text_when_no_tool_call_blocks()
    {
        var (text, calls) = LlamaSharpClient.ExtractToolCalls("Just a plain assistant response.");
        Assert.Equal("Just a plain assistant response.", text);
        Assert.Empty(calls);
    }

    [Fact]
    public void Parses_single_tool_call()
    {
        const string raw = """
            <tool_call>
            {"name": "core.current_time", "arguments": {}}
            </tool_call>
            """;

        var (text, calls) = LlamaSharpClient.ExtractToolCalls(raw);

        Assert.Equal(string.Empty, text);
        Assert.Single(calls);
        Assert.Equal("core.current_time", calls[0].Name);
        Assert.Equal("{}", calls[0].ArgumentsJson);
        Assert.False(string.IsNullOrWhiteSpace(calls[0].Id));
    }

    [Fact]
    public void Preserves_prose_around_tool_call()
    {
        const string raw = """
            Let me check that for you.

            <tool_call>
            {"name": "core.weather", "arguments": {"location": "Phoenix"}}
            </tool_call>

            That should do it.
            """;

        var (text, calls) = LlamaSharpClient.ExtractToolCalls(raw);

        Assert.Contains("Let me check that for you.", text);
        Assert.Contains("That should do it.", text);
        Assert.DoesNotContain("<tool_call>", text);
        Assert.Single(calls);
        Assert.Equal("core.weather", calls[0].Name);
        Assert.Contains("Phoenix", calls[0].ArgumentsJson);
    }

    [Fact]
    public void Parses_multiple_tool_calls_in_one_response()
    {
        const string raw = """
            <tool_call>{"name": "core.current_time", "arguments": {}}</tool_call>
            <tool_call>{"name": "core.weather", "arguments": {"location": "Tokyo"}}</tool_call>
            """;

        var (_, calls) = LlamaSharpClient.ExtractToolCalls(raw);

        Assert.Equal(2, calls.Count);
        Assert.Equal("core.current_time", calls[0].Name);
        Assert.Equal("core.weather", calls[1].Name);
    }

    [Fact]
    public void Handles_nested_braces_in_arguments()
    {
        // Nested JSON objects inside `arguments` must survive parsing (non-greedy regex
        // could bail out on the inner closing brace).
        const string raw = """
            <tool_call>
            {"name": "x.y", "arguments": {"filter": {"op": "eq", "val": 42}}}
            </tool_call>
            """;

        var (_, calls) = LlamaSharpClient.ExtractToolCalls(raw);

        Assert.Single(calls);
        Assert.Equal("x.y", calls[0].Name);
        Assert.Contains("\"filter\"", calls[0].ArgumentsJson);
        Assert.Contains("\"val\":42", calls[0].ArgumentsJson.Replace(" ", ""));
    }

    [Fact]
    public void Skips_malformed_json_without_throwing()
    {
        const string raw = """
            <tool_call>
            {"name": "broken" "arguments": {}}
            </tool_call>
            <tool_call>
            {"name": "good", "arguments": {}}
            </tool_call>
            """;

        var (_, calls) = LlamaSharpClient.ExtractToolCalls(raw);

        Assert.Single(calls);
        Assert.Equal("good", calls[0].Name);
    }

    [Fact]
    public void Skips_tool_calls_missing_name_field()
    {
        const string raw = """
            <tool_call>{"arguments": {}}</tool_call>
            """;

        var (_, calls) = LlamaSharpClient.ExtractToolCalls(raw);
        Assert.Empty(calls);
    }

    [Fact]
    public void Generates_distinct_ids_per_call()
    {
        const string raw = """
            <tool_call>{"name":"a","arguments":{}}</tool_call>
            <tool_call>{"name":"b","arguments":{}}</tool_call>
            """;

        var (_, calls) = LlamaSharpClient.ExtractToolCalls(raw);
        Assert.Equal(2, calls.Count);
        Assert.NotEqual(calls[0].Id, calls[1].Id);
    }
}
