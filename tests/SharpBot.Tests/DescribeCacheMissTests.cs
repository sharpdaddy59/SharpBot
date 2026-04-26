using SharpBot.Llm;

namespace SharpBot.Tests;

public class DescribeCacheMissTests
{
    [Fact]
    public void Reports_byte_offset_at_first_divergence()
    {
        var processed = "Hello world, this is a long shared prefix.";
        var fullPrompt = "Hello world, this is a different ending.";

        var msg = LlamaSharpClient.DescribeCacheMiss(processed, fullPrompt);

        Assert.Contains("diverged at byte 23", msg);
        Assert.Contains("processed.Length=", msg);
        Assert.Contains("fullPrompt.Length=", msg);
    }

    [Fact]
    public void Includes_windowed_snippet_around_divergence()
    {
        var processed = "AAAAAAAAAAXdiverge";
        var fullPrompt = "AAAAAAAAAAYdiverge";

        var msg = LlamaSharpClient.DescribeCacheMiss(processed, fullPrompt);

        Assert.Contains("X", msg);
        Assert.Contains("Y", msg);
    }

    [Fact]
    public void Escapes_control_characters_in_snippet_for_log_readability()
    {
        var processed = "shared\nA";
        var fullPrompt = "shared\nB";

        var msg = LlamaSharpClient.DescribeCacheMiss(processed, fullPrompt);

        Assert.Contains("\\n", msg);
        Assert.DoesNotContain("\n", msg.Replace("\\n", ""));
    }

    [Fact]
    public void Reports_full_common_prefix_when_one_is_strict_prefix_of_other()
    {
        // The miss branch reaches this case only when processed is LONGER than fullPrompt
        // (StartsWith would have hit the cache otherwise). Means the conversation got shorter
        // — likely a history truncation we should surface clearly.
        var processed = "AAAAAAAA-extra-tail-that-is-not-in-fullPrompt";
        var fullPrompt = "AAAAAAAA";

        var msg = LlamaSharpClient.DescribeCacheMiss(processed, fullPrompt);

        Assert.Contains("full common prefix of 8 chars", msg);
        Assert.Contains("processed.Length=", msg);
    }
}
