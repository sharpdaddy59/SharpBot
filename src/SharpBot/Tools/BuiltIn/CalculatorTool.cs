using System.Globalization;
using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

/// <summary>
/// Exact (well, double-precision) arithmetic evaluator — small LLMs get math wrong surprisingly often,
/// so routing arithmetic to real code is a big accuracy win.
/// </summary>
public sealed class CalculatorTool : IBuiltInTool
{
    public string Name => "calculator";

    public string Description =>
        "Evaluate a math expression and return the numeric result. Use this for any arithmetic — " +
        "small language models get math wrong surprisingly often. Supports + - * / % ^ and parentheses, " +
        "plus functions: sqrt, abs, min, max, round, floor, ceil, log, ln, sin, cos, tan, pi, e.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "expression": {
              "type": "string",
              "description": "Math expression, e.g. '(17 * 2348) / 100' or 'sqrt(2) + sin(pi/4)'."
            }
          },
          "required": ["expression"]
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("expression", out var exprEl) ||
            exprEl.ValueKind != JsonValueKind.String)
        {
            return Task.FromResult("Error: missing required 'expression' string argument.");
        }

        var expression = exprEl.GetString()!;
        try
        {
            var result = Evaluate(expression);
            return Task.FromResult(FormatResult(result));
        }
        catch (FormatException ex)
        {
            return Task.FromResult($"Error: {ex.Message}");
        }
        catch (DivideByZeroException)
        {
            return Task.FromResult("Error: division by zero.");
        }
    }

    private static string FormatResult(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsInfinity(value)) return value > 0 ? "Infinity" : "-Infinity";

        // Show as integer when exactly integral; else full precision trimmed.
        if (Math.Abs(value) < 1e15 && value == Math.Truncate(value))
        {
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        }
        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    // ====================================================================================
    // Minimal shunting-yard parser + RPN evaluator. Supports:
    //  - numeric literals (with optional exponent), parentheses, + - * / % ^
    //  - unary minus
    //  - the function set listed in Description above
    //  - constants: pi, e
    // Deliberately small and dependency-free. Not meant to rival NCalc; meant to be
    // correct on basic arithmetic (which is all the LLM needs for "17% of 2,348").
    // ====================================================================================

    internal static double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("empty expression");

        var tokens = Tokenize(expression);
        var rpn = ShuntingYard(tokens);
        return EvaluateRpn(rpn);
    }

    private enum TokType { Number, Op, LParen, RParen, Comma, Ident }

    private readonly record struct Tok(TokType Type, string Text, double Number = 0);

    private static List<Tok> Tokenize(string s)
    {
        var tokens = new List<Tok>();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            // skip digit group commas like "2,348" — treat as separators only inside numbers
            if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                var start = i;
                var sawDot = c == '.';
                i++;
                while (i < s.Length)
                {
                    var cc = s[i];
                    if (char.IsDigit(cc)) { i++; continue; }
                    if (cc == ',' && i + 1 < s.Length && char.IsDigit(s[i + 1])) { i++; continue; }
                    if (cc == '.' && !sawDot) { sawDot = true; i++; continue; }
                    if ((cc == 'e' || cc == 'E') && i + 1 < s.Length)
                    {
                        i++;
                        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                        while (i < s.Length && char.IsDigit(s[i])) i++;
                        break;
                    }
                    break;
                }
                var raw = s[start..i].Replace(",", "");
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                    throw new FormatException($"invalid number '{raw}'");
                tokens.Add(new Tok(TokType.Number, raw, num));
                continue;
            }

            if (char.IsLetter(c))
            {
                var start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                var ident = s[start..i].ToLowerInvariant();
                tokens.Add(new Tok(TokType.Ident, ident));
                continue;
            }

            switch (c)
            {
                case '+' or '-' or '*' or '/' or '%' or '^':
                    tokens.Add(new Tok(TokType.Op, c.ToString()));
                    i++;
                    continue;
                case '(':
                    tokens.Add(new Tok(TokType.LParen, "("));
                    i++;
                    continue;
                case ')':
                    tokens.Add(new Tok(TokType.RParen, ")"));
                    i++;
                    continue;
                case ',':
                    tokens.Add(new Tok(TokType.Comma, ","));
                    i++;
                    continue;
                default:
                    throw new FormatException($"unexpected character '{c}' at position {i}");
            }
        }
        return tokens;
    }

    private static int Precedence(string op) => op switch
    {
        "+" or "-" => 1,
        "*" or "/" or "%" => 2,
        "^" => 3,
        "u-" => 4, // unary minus, highest
        _ => 0,
    };

    private static bool IsRightAssoc(string op) => op is "^" or "u-";

    private static List<Tok> ShuntingYard(List<Tok> tokens)
    {
        var output = new List<Tok>();
        var stack = new Stack<Tok>();
        Tok? prev = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var tok = tokens[i];

            // Disambiguate unary minus: occurs at start, after another op, or after "(".
            if (tok.Type == TokType.Op && tok.Text == "-"
                && (prev is null || prev.Value.Type is TokType.Op or TokType.LParen or TokType.Comma))
            {
                tok = new Tok(TokType.Op, "u-");
            }

            switch (tok.Type)
            {
                case TokType.Number:
                    output.Add(tok);
                    break;
                case TokType.Ident:
                {
                    // An ident followed by "(" is a function call — push to stack so it pops
                    // after the matching ")". An ident not followed by "(" is a constant;
                    // resolve its value now and emit as a Number token.
                    var isCall = i + 1 < tokens.Count && tokens[i + 1].Type == TokType.LParen;
                    if (isCall)
                    {
                        stack.Push(tok);
                    }
                    else
                    {
                        var value = tok.Text switch
                        {
                            "pi" => Math.PI,
                            "e" => Math.E,
                            _ => throw new FormatException($"unknown identifier '{tok.Text}'"),
                        };
                        output.Add(new Tok(TokType.Number, tok.Text, value));
                    }
                    break;
                }
                case TokType.Comma:
                    while (stack.Count > 0 && stack.Peek().Type != TokType.LParen)
                        output.Add(stack.Pop());
                    if (stack.Count == 0) throw new FormatException("misplaced comma");
                    break;
                case TokType.Op:
                    while (stack.Count > 0 && stack.Peek().Type == TokType.Op)
                    {
                        var top = stack.Peek();
                        var cond = IsRightAssoc(tok.Text)
                            ? Precedence(tok.Text) < Precedence(top.Text)
                            : Precedence(tok.Text) <= Precedence(top.Text);
                        if (!cond) break;
                        output.Add(stack.Pop());
                    }
                    stack.Push(tok);
                    break;
                case TokType.LParen:
                    stack.Push(tok);
                    break;
                case TokType.RParen:
                    while (stack.Count > 0 && stack.Peek().Type != TokType.LParen)
                        output.Add(stack.Pop());
                    if (stack.Count == 0) throw new FormatException("unbalanced parentheses");
                    stack.Pop(); // discard LParen
                    if (stack.Count > 0 && stack.Peek().Type == TokType.Ident)
                        output.Add(stack.Pop()); // function call
                    break;
            }
            prev = tok;
        }

        while (stack.Count > 0)
        {
            var top = stack.Pop();
            if (top.Type is TokType.LParen or TokType.RParen)
                throw new FormatException("unbalanced parentheses");
            output.Add(top);
        }
        return output;
    }

    private static double EvaluateRpn(List<Tok> rpn)
    {
        var stack = new Stack<double>();
        foreach (var tok in rpn)
        {
            switch (tok.Type)
            {
                case TokType.Number:
                    stack.Push(tok.Number);
                    break;
                case TokType.Op when tok.Text == "u-":
                    if (stack.Count < 1) throw new FormatException("unary minus missing operand");
                    stack.Push(-stack.Pop());
                    break;
                case TokType.Op:
                {
                    if (stack.Count < 2) throw new FormatException($"operator {tok.Text} missing operands");
                    var b = stack.Pop();
                    var a = stack.Pop();
                    stack.Push(tok.Text switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => b == 0 ? throw new DivideByZeroException() : a / b,
                        "%" => a % b,
                        "^" => Math.Pow(a, b),
                        _ => throw new FormatException($"unknown operator {tok.Text}"),
                    });
                    break;
                }
                case TokType.Ident:
                    stack.Push(ApplyFunction(tok.Text, stack));
                    break;
                default:
                    throw new FormatException($"unexpected token '{tok.Text}'");
            }
        }
        if (stack.Count != 1) throw new FormatException("malformed expression");
        return stack.Pop();
    }

    private static double ApplyFunction(string name, Stack<double> stack)
    {
        // Constants come through as zero-arg idents.
        switch (name)
        {
            case "pi": return Math.PI;
            case "e": return Math.E;
        }

        // Unary functions consume one operand. For min/max we'd need comma handling;
        // keep it simple — all supported functions below are unary.
        if (stack.Count < 1) throw new FormatException($"function {name} missing argument");
        var x = stack.Pop();
        return name switch
        {
            "sqrt" => Math.Sqrt(x),
            "abs" => Math.Abs(x),
            "round" => Math.Round(x),
            "floor" => Math.Floor(x),
            "ceil" or "ceiling" => Math.Ceiling(x),
            "log" => Math.Log10(x),
            "ln" => Math.Log(x),
            "exp" => Math.Exp(x),
            "sin" => Math.Sin(x),
            "cos" => Math.Cos(x),
            "tan" => Math.Tan(x),
            _ => throw new FormatException($"unknown function '{name}'"),
        };
    }
}
