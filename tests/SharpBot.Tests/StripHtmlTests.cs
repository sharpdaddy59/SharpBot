using SharpBot.Tools.BuiltIn;

namespace SharpBot.Tests;

public class StripHtmlTests
{
    [Fact]
    public void Removes_basic_tags()
    {
        var result = FetchUrlTool.StripHtml("<p>Hello <b>world</b>!</p>");
        Assert.Equal("Hello world !", result);
    }

    [Fact]
    public void Drops_script_content_entirely()
    {
        var input = "<html><body>before<script>alert('xss')</script>after</body></html>";
        var result = FetchUrlTool.StripHtml(input);
        Assert.DoesNotContain("alert", result);
        Assert.Contains("before", result);
        Assert.Contains("after", result);
    }

    [Fact]
    public void Drops_style_content_entirely()
    {
        var input = "<style>body { color: red }</style>Hello";
        var result = FetchUrlTool.StripHtml(input);
        Assert.DoesNotContain("color: red", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void Decodes_html_entities()
    {
        var result = FetchUrlTool.StripHtml("<p>Tom &amp; Jerry &lt;3</p>");
        Assert.Equal("Tom & Jerry <3", result);
    }

    [Fact]
    public void Collapses_whitespace()
    {
        var input = "<div>\n    Hello\n\n\n    World  </div>";
        var result = FetchUrlTool.StripHtml(input);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Handles_empty_input()
    {
        Assert.Equal(string.Empty, FetchUrlTool.StripHtml(""));
    }

    [Fact]
    public void Preserves_plain_text_when_no_tags()
    {
        var result = FetchUrlTool.StripHtml("Just plain text.");
        Assert.Equal("Just plain text.", result);
    }

    [Fact]
    public void Case_insensitive_script_match()
    {
        var input = "before<SCRIPT>bad</SCRIPT>after";
        var result = FetchUrlTool.StripHtml(input);
        Assert.DoesNotContain("bad", result);
    }
}
