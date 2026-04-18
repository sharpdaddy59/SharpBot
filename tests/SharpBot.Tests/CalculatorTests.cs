using SharpBot.Tools.BuiltIn;

namespace SharpBot.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData("1 + 1", 2)]
    [InlineData("10 - 3", 7)]
    [InlineData("6 * 7", 42)]
    [InlineData("20 / 4", 5)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("2 ^ 10", 1024)]
    [InlineData("-5 + 3", -2)]
    [InlineData("-(-5)", 5)]
    [InlineData("10 % 3", 1)]
    public void Handles_basic_arithmetic(string expr, double expected)
    {
        Assert.Equal(expected, CalculatorTool.Evaluate(expr), precision: 10);
    }

    [Fact]
    public void Seventeen_percent_of_2348()
    {
        // Exactly the kind of question where 3B models produce confident garbage.
        var result = CalculatorTool.Evaluate("(17 * 2348) / 100");
        Assert.Equal(399.16, result, precision: 10);
    }

    [Fact]
    public void Accepts_digit_group_commas()
    {
        Assert.Equal(2348, CalculatorTool.Evaluate("2,348"));
        Assert.Equal(4696, CalculatorTool.Evaluate("2,348 * 2"));
    }

    [Fact]
    public void Right_associative_power()
    {
        // 2 ^ 3 ^ 2 should be 2 ^ (3 ^ 2) = 2^9 = 512, not (2^3)^2 = 64.
        Assert.Equal(512, CalculatorTool.Evaluate("2 ^ 3 ^ 2"));
    }

    [Fact]
    public void Supports_unary_functions()
    {
        Assert.Equal(5, CalculatorTool.Evaluate("sqrt(25)"));
        Assert.Equal(5, CalculatorTool.Evaluate("abs(-5)"));
        Assert.Equal(3, CalculatorTool.Evaluate("round(2.6)"));
        Assert.Equal(2, CalculatorTool.Evaluate("floor(2.9)"));
        Assert.Equal(3, CalculatorTool.Evaluate("ceil(2.1)"));
    }

    [Fact]
    public void Supports_constants()
    {
        Assert.Equal(Math.PI, CalculatorTool.Evaluate("pi"), precision: 10);
        Assert.Equal(Math.E, CalculatorTool.Evaluate("e"), precision: 10);
    }

    [Fact]
    public void Nested_functions_and_operators()
    {
        // sqrt(2) + sin(pi/4) ≈ 1.4142 + 0.7071 ≈ 2.1213
        var result = CalculatorTool.Evaluate("sqrt(2) + sin(pi/4)");
        Assert.Equal(2.1213, result, precision: 4);
    }

    [Fact]
    public void Divide_by_zero_throws()
    {
        Assert.Throws<DivideByZeroException>(() => CalculatorTool.Evaluate("1 / 0"));
    }

    [Fact]
    public void Empty_input_throws()
    {
        Assert.Throws<FormatException>(() => CalculatorTool.Evaluate(""));
    }

    [Fact]
    public void Unbalanced_parens_throws()
    {
        Assert.Throws<FormatException>(() => CalculatorTool.Evaluate("(2 + 3"));
        Assert.Throws<FormatException>(() => CalculatorTool.Evaluate("2 + 3)"));
    }

    [Fact]
    public void Unknown_identifier_throws()
    {
        Assert.Throws<FormatException>(() => CalculatorTool.Evaluate("banana + 1"));
    }

    [Fact]
    public void Garbage_characters_throw()
    {
        Assert.Throws<FormatException>(() => CalculatorTool.Evaluate("2 @ 3"));
    }
}
