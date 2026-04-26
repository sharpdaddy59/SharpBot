using SharpBot.Llm;

namespace SharpBot.Tests;

public class StreamingTextEmitterTests
{
    private static string Run(StreamingTextEmitter e, IEnumerable<string> chunks)
    {
        var emitted = new List<string>();
        foreach (var c in chunks)
        {
            var v = e.Push(c);
            if (v is not null) emitted.Add(v);
        }
        var tail = e.Drain();
        if (tail is not null) emitted.Add(tail);
        return string.Concat(emitted);
    }

    [Fact]
    public void Pure_prose_streams_through()
    {
        Assert.Equal(
            "Hello, world!",
            Run(new StreamingTextEmitter(), new[] { "Hello", ", ", "world", "!" }));
    }

    [Fact]
    public void Pure_tool_call_emits_no_visible_text()
    {
        Assert.Equal(
            string.Empty,
            Run(new StreamingTextEmitter(), new[]
            {
                "<tool_call>\n",
                "{\"name\": \"core.current_time\"}",
                "\n</tool_call>",
            }));
    }

    [Fact]
    public void Prose_preamble_then_tool_call_emits_only_the_prose()
    {
        Assert.Equal(
            "Let me check that for you. ",
            Run(new StreamingTextEmitter(), new[]
            {
                "Let me check that for you. ",
                "<tool_call>\n{}\n</tool_call>",
            }));
    }

    [Fact]
    public void Partial_marker_split_one_char_at_a_time_does_not_leak()
    {
        // Model dribbles "<tool_call>" one character per chunk. Nothing should reach the user.
        var chunks = new[] { "<", "t", "o", "o", "l", "_", "c", "a", "l", "l", ">", "{}" };
        Assert.Equal(string.Empty, Run(new StreamingTextEmitter(), chunks));
    }

    [Fact]
    public void Lone_lt_followed_by_safe_text_is_eventually_flushed()
    {
        // "<" alone is held back; the next chunk reveals it's not a marker, so it flushes.
        Assert.Equal(
            "1 < 2 is true.",
            Run(new StreamingTextEmitter(), new[] { "1 ", "<", " 2 is true." }));
    }

    [Fact]
    public void Double_backticks_alone_do_not_trigger_suppression()
    {
        // No triple-backtick is ever formed, so all text should eventually be visible.
        Assert.Equal(
            "see ``code`` here",
            Run(new StreamingTextEmitter(), new[] { "see ", "`", "`", "code", "`", "`", " here" }));
    }

    [Fact]
    public void Triple_backticks_suppress_subsequent_output()
    {
        Assert.Equal(
            "Here you go: ",
            Run(new StreamingTextEmitter(), new[] { "Here you go: ", "```json\n", "{}", "\n```" }));
    }

    [Fact]
    public void Empty_chunks_are_ignored()
    {
        var e = new StreamingTextEmitter();
        Assert.Null(e.Push(string.Empty));
        Assert.Null(e.Push(string.Empty));
        var v = e.Push("hi");
        var tail = e.Drain();
        Assert.Equal("hi", (v ?? "") + (tail ?? ""));
    }

    [Fact]
    public void Once_suppressed_stays_suppressed()
    {
        var e = new StreamingTextEmitter();
        e.Push("<tool_call>{}</tool_call>");
        // Even more output after the marker is silently swallowed.
        Assert.Null(e.Push("more text after"));
        Assert.Null(e.Drain());
    }
}
