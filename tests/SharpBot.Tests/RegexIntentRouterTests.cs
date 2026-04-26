using SharpBot.Agent;

namespace SharpBot.Tests;

public class RegexIntentRouterTests
{
    private readonly RegexIntentRouter _router = new();

    [Theory]
    [InlineData("what time is it")]
    [InlineData("What time is it?")]
    [InlineData("what's the time")]
    [InlineData("what is the time")]
    [InlineData("current time")]
    [InlineData("got the time?")]
    [InlineData("  what time is it  ")]
    public void Matches_time_questions(string input)
    {
        var match = _router.TryMatch(input);
        Assert.NotNull(match);
        Assert.Equal("core.current_time", match!.Name);
        Assert.Equal("{}", match.ArgumentsJson);
    }

    [Theory]
    [InlineData("what's the date")]
    [InlineData("what is the date?")]
    [InlineData("today's date")]
    [InlineData("todays date")]
    [InlineData("what date is it")]
    [InlineData("what day is today")]
    public void Matches_date_questions(string input)
    {
        var match = _router.TryMatch(input);
        Assert.NotNull(match);
        Assert.Equal("core.current_time", match!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("what time does the store open")]
    [InlineData("tell me a joke")]
    [InlineData("set a timer for five minutes")]
    [InlineData("what time is it in tokyo")]
    [InlineData("can you check the weather")]
    public void Skips_anything_outside_the_narrow_match_set(string input)
    {
        Assert.Null(_router.TryMatch(input));
    }

    [Fact]
    public void Generated_tool_call_has_unique_id_per_invocation()
    {
        var a = _router.TryMatch("what time is it");
        var b = _router.TryMatch("what time is it");
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotEqual(a!.Id, b!.Id);
    }
}
