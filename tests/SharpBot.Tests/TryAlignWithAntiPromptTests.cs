using SharpBot.Llm;

namespace SharpBot.Tests;

public class TryAlignWithAntiPromptTests
{
    [Fact]
    public void Aligns_when_processed_plus_eot_is_exact_prefix_of_fullPrompt()
    {
        var processed = "system stuff<|im_start|>user\nHi<|im_end|>\n<|im_start|>assistant\ntoday?";
        var fullPrompt = processed + "<|im_end|>\n<|im_start|>user\nNext question";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out var stop);

        Assert.True(ok);
        Assert.Equal(processed + "<|im_end|>", extended);
        Assert.Equal("<|im_end|>", stop);
    }

    [Fact]
    public void Aligns_with_trimmed_processed_when_model_emitted_trailing_newline_before_eot()
    {
        // The actual Brazos failure mode: model emits a '\n' between content and EOT, but
        // the chat template re-renders content directly against the EOT. Literal alignment
        // fails; trimmed alignment must succeed so the cache stays warm.
        var processed = "system<|im_start|>assistant\ntoday?\n";
        var fullPrompt = "system<|im_start|>assistant\ntoday?<|im_end|>\n<|im_start|>user\nWhat next?";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out var stop);

        Assert.True(ok);
        Assert.Equal("system<|im_start|>assistant\ntoday?<|im_end|>", extended);
        Assert.Equal("<|im_end|>", stop);
    }

    [Fact]
    public void Aligns_with_multiple_trailing_whitespace_chars()
    {
        var processed = "answer here  \n\n";
        var fullPrompt = "answer here<|im_end|>\nmore content";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out _);

        Assert.True(ok);
        Assert.Equal("answer here<|im_end|>", extended);
    }

    [Fact]
    public void Returns_false_when_no_anti_prompt_can_make_processed_a_prefix()
    {
        var processed = "totally different content";
        var fullPrompt = "system stuff and other things";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out var stop);

        Assert.False(ok);
        Assert.Equal(processed, extended);
        Assert.Equal(string.Empty, stop);
    }

    [Fact]
    public void Tries_all_anti_prompts_on_literal_alignment_first()
    {
        // Gemma's <end_of_turn> as the EOT — should align without needing the trim fallback.
        var processed = "answer";
        var fullPrompt = "answer<end_of_turn>\nmore";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out var stop);

        Assert.True(ok);
        Assert.Equal("answer<end_of_turn>", extended);
        Assert.Equal("<end_of_turn>", stop);
    }

    [Fact]
    public void Tries_all_anti_prompts_on_trimmed_alignment_too()
    {
        // Trim path with a non-Qwen anti-prompt.
        var processed = "answer\n";
        var fullPrompt = "answer<|eot_id|>more";

        var ok = LlamaSharpClient.TryAlignWithAntiPrompt(processed, fullPrompt, out var extended, out var stop);

        Assert.True(ok);
        Assert.Equal("answer<|eot_id|>", extended);
        Assert.Equal("<|eot_id|>", stop);
    }
}
