using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

public sealed class MiniPythonInterpreter
{
    private enum ValueType
    {
        Integer,
        Float,
        String,
        Boolean
    }

    private readonly struct Value
    {
        public readonly ValueType Type;
        public readonly object Data;

        private Value(ValueType type, object data)
        {
            Type = type;
            Data = data;
        }

        public static Value Int(long n) =>
            new Value(ValueType.Integer, n);

        public static Value Float(double n) =>
            new Value(ValueType.Float, n);

        public static Value Str(string s) =>
            new Value(ValueType.String, s);

        public static Value Bool(bool b) =>
            new Value(ValueType.Boolean, b);

        public long AsInt() => (long)Data;
        public double AsNumber() => Convert.ToDouble(
            Data, CultureInfo.InvariantCulture);
        public string AsString() => (string)Data;
        public bool AsBool() => (bool)Data;

        public bool IsNumber =>
            Type == ValueType.Integer || Type == ValueType.Float;

        public string Format()
        {
            switch (Type)
            {
                case ValueType.Integer:
                    return AsInt().ToString(
                        CultureInfo.InvariantCulture);

                case ValueType.Float:
                    return AsNumber().ToString(
                        "G", CultureInfo.InvariantCulture);

                case ValueType.String:
                    return AsString();

                case ValueType.Boolean:
                    return AsBool() ? "True" : "False";

                default:
                    throw new Exception("Unknown value type.");
            }
        }
    }

    private enum TokenType
    {
        Number,
        String,
        Identifier,
        Operator,
        LeftParen,
        RightParen,
        End
    }

    private readonly struct Token
    {
        public readonly TokenType Type;
        public readonly string Text;
        public readonly int Position;

        public Token(TokenType type, string text, int position)
        {
            Type = type;
            Text = text;
            Position = position;
        }
    }

    private readonly Dictionary<string, Value> variables =
        new Dictionary<string, Value>();

    public string Run(string source)
    {
        variables.Clear();

        var output = new StringBuilder();
        string[] lines = (source ?? "")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            try
            {
                ExecuteLine(lines[i], output);
            }
            catch (Exception ex)
            {
                return $"Line {i + 1}: {ex.Message}";
            }
        }

        return output.ToString();
    }

    private void ExecuteLine(string source, StringBuilder output)
    {
        List<Token> tokens = Tokenize(source);

        if (tokens.Count == 1)
            return;

        // print(expression)
        if (tokens[0].Type == TokenType.Identifier &&
            tokens[0].Text == "print")
        {
            var parser = new Parser(tokens, variables);

            parser.ExpectIdentifier("print");
            parser.Expect(TokenType.LeftParen, "(");

            Value value = parser.ParseExpression();

            parser.Expect(TokenType.RightParen, ")");
            parser.ExpectEnd();

            output.AppendLine(value.Format());
            return;
        }

        // variable = expression
        if (tokens.Count >= 3 &&
            tokens[0].Type == TokenType.Identifier &&
            tokens[1].Type == TokenType.Operator &&
            tokens[1].Text == "=")
        {
            string name = tokens[0].Text;

            if (name == "True" || name == "False" ||
                name == "and" || name == "or" || name == "not")
            {
                throw new Exception(
                    $"'{name}' is a reserved word.");
            }

            var parser = new Parser(tokens, variables);
            parser.ExpectIdentifier(name);
            parser.ExpectOperator("=");

            Value value = parser.ParseExpression();
            parser.ExpectEnd();

            variables[name] = value;
            return;
        }

        throw new Exception(
            "Unsupported statement. Use variable = expression " +
            "or print(expression).");
    }

    private static List<Token> Tokenize(string source)
    {
        var result = new List<Token>();
        int i = 0;

        while (i < source.Length)
        {
            char c = source[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Comments outside strings.
            if (c == '#')
                break;

            int start = i;

            // Strings: single or double quotes.
            if (c == '"' || c == '\'')
            {
                char quote = c;
                i++;
                var value = new StringBuilder();
                bool closed = false;

                while (i < source.Length)
                {
                    c = source[i++];

                    if (c == quote)
                    {
                        closed = true;
                        break;
                    }

                    if (c == '\\')
                    {
                        if (i >= source.Length)
                            throw new Exception(
                                "Incomplete string escape.");

                        char escaped = source[i++];

                        switch (escaped)
                        {
                            case 'n': value.Append('\n'); break;
                            case 't': value.Append('\t'); break;
                            case '\\': value.Append('\\'); break;
                            case '"': value.Append('"'); break;
                            case '\'': value.Append('\''); break;
                            default:
                                throw new Exception(
                                    $"Unsupported escape '\\{escaped}'.");
                        }
                    }
                    else
                    {
                        value.Append(c);
                    }
                }

                if (!closed)
                    throw new Exception("Unterminated string.");

                result.Add(new Token(
                    TokenType.String, value.ToString(), start));
                continue;
            }

            // Integer and decimal literals.
            if (char.IsDigit(c))
            {
                i++;
                while (i < source.Length &&
                       char.IsDigit(source[i]))
                    i++;

                if (i < source.Length &&
                    source[i] == '.' &&
                    i + 1 < source.Length &&
                    char.IsDigit(source[i + 1]))
                {
                    i++;
                    while (i < source.Length &&
                           char.IsDigit(source[i]))
                        i++;
                }

                result.Add(new Token(
                    TokenType.Number,
                    source.Substring(start, i - start), start));
                continue;
            }

            // Identifiers.
            if (char.IsLetter(c) || c == '_')
            {
                i++;
                while (i < source.Length &&
                       (char.IsLetterOrDigit(source[i]) ||
                        source[i] == '_'))
                    i++;

                result.Add(new Token(
                    TokenType.Identifier,
                    source.Substring(start, i - start), start));
                continue;
            }

            if (c == '(')
            {
                result.Add(new Token(
                    TokenType.LeftParen, "(", i++));
                continue;
            }

            if (c == ')')
            {
                result.Add(new Token(
                    TokenType.RightParen, ")", i++));
                continue;
            }

            // Two-character operators.
            if (i + 1 < source.Length)
            {
                string pair = source.Substring(i, 2);

                if (pair == "==" || pair == "!=" ||
                    pair == "<=" || pair == ">=" ||
                    pair == "//")
                {
                    result.Add(new Token(
                        TokenType.Operator, pair, i));
                    i += 2;
                    continue;
                }
            }

            if ("+-*/%<>=".IndexOf(c) >= 0)
            {
                result.Add(new Token(
                    TokenType.Operator, c.ToString(), i++));
                continue;
            }

            throw new Exception(
                $"Unexpected character '{c}'.");
        }

        result.Add(new Token(TokenType.End, "", source.Length));
        return result;
    }

    private sealed class Parser
    {
        private readonly List<Token> tokens;
        private readonly Dictionary<string, Value> variables;
        private int position;

        public Parser(
            List<Token> tokens,
            Dictionary<string, Value> variables)
        {
            this.tokens = tokens;
            this.variables = variables;
        }

        private Token Current => tokens[position];

        private Token Advance()
        {
            Token token = Current;
            if (Current.Type != TokenType.End)
                position++;
            return token;
        }

        public void Expect(TokenType type, string description)
        {
            if (Current.Type != type)
                throw new Exception($"Expected '{description}'.");

            Advance();
        }

        public void ExpectIdentifier(string name)
        {
            if (Current.Type != TokenType.Identifier ||
                Current.Text != name)
                throw new Exception($"Expected '{name}'.");

            Advance();
        }

        public void ExpectOperator(string op)
        {
            if (Current.Type != TokenType.Operator ||
                Current.Text != op)
                throw new Exception($"Expected '{op}'.");

            Advance();
        }

        public void ExpectEnd()
        {
            if (Current.Type != TokenType.End)
                throw new Exception(
                    $"Unexpected token '{Current.Text}'.");
        }

        public Value ParseExpression() => ParseOr();

        private Value ParseOr()
        {
            Value left = ParseAnd();

            while (IsWord("or"))
            {
                Advance();
                Value right = ParseAnd();
                left = Value.Bool(
                    RequireBool(left, "or") ||
                    RequireBool(right, "or"));
            }

            return left;
        }

        private Value ParseAnd()
        {
            Value left = ParseNot();

            while (IsWord("and"))
            {
                Advance();
                Value right = ParseNot();
                left = Value.Bool(
                    RequireBool(left, "and") &&
                    RequireBool(right, "and"));
            }

            return left;
        }

        private Value ParseNot()
        {
            if (IsWord("not"))
            {
                Advance();
                return Value.Bool(!RequireBool(
                    ParseNot(), "not"));
            }

            return ParseComparison();
        }

        private Value ParseComparison()
        {
            Value left = ParseAdditive();

            if (Current.Type != TokenType.Operator ||
                !(Current.Text == "==" || Current.Text == "!=" ||
                  Current.Text == "<" || Current.Text == ">" ||
                  Current.Text == "<=" || Current.Text == ">="))
                return left;

            string op = Advance().Text;
            Value right = ParseAdditive();

            bool result;

            if (op == "==" || op == "!=")
            {
                bool equal;

                if (left.IsNumber && right.IsNumber)
                    equal = left.AsNumber() == right.AsNumber();
                else if (left.Type != right.Type)
                    equal = false;
                else if (left.Type == ValueType.String)
                    equal = left.AsString() == right.AsString();
                else if (left.Type == ValueType.Boolean)
                    equal = left.AsBool() == right.AsBool();
                else
                    equal = false;

                result = op == "==" ? equal : !equal;
            }
            else if (left.IsNumber && right.IsNumber)
            {
                double a = left.AsNumber();
                double b = right.AsNumber();

                switch (op)
                {
                    case "<": result = a < b; break;
                    case ">": result = a > b; break;
                    case "<=": result = a <= b; break;
                    default: result = a >= b; break;
                }
            }
            else if (left.Type == ValueType.String &&
                     right.Type == ValueType.String)
            {
                int comparison = string.CompareOrdinal(
                    left.AsString(), right.AsString());

                switch (op)
                {
                    case "<": result = comparison < 0; break;
                    case ">": result = comparison > 0; break;
                    case "<=": result = comparison <= 0; break;
                    default: result = comparison >= 0; break;
                }
            }
            else
            {
                throw new Exception(
                    $"Operator '{op}' requires compatible values.");
            }

            return Value.Bool(result);
        }

        private Value ParseAdditive()
        {
            Value left = ParseMultiplicative();

            while (IsOperator("+") || IsOperator("-"))
            {
                string op = Advance().Text;
                Value right = ParseMultiplicative();

                if (op == "+" &&
                    left.Type == ValueType.String &&
                    right.Type == ValueType.String)
                {
                    left = Value.Str(
                        left.AsString() + right.AsString());
                }
                else
                {
                    left = NumericOperation(left, right, op);
                }
            }

            return left;
        }

        private Value ParseMultiplicative()
        {
            Value left = ParseUnary();

            while (IsOperator("*") || IsOperator("/") ||
                   IsOperator("//") || IsOperator("%"))
            {
                string op = Advance().Text;
                Value right = ParseUnary();

                if (op == "*" &&
                    left.Type == ValueType.String &&
                    right.Type == ValueType.Integer)
                {
                    long count = right.AsInt();

                    if (count < 0 || count > 10000)
                        throw new Exception(
                            "String repetition must be between 0 and 10000.");

                    var repeated = new StringBuilder();
                    for (long n = 0; n < count; n++)
                        repeated.Append(left.AsString());

                    left = Value.Str(repeated.ToString());
                }
                else
                {
                    left = NumericOperation(left, right, op);
                }
            }

            return left;
        }

        private Value ParseUnary()
        {
            if (IsOperator("+"))
            {
                Advance();
                Value value = ParseUnary();
                RequireNumber(value, "unary +");
                return value;
            }

            if (IsOperator("-"))
            {
                Advance();
                Value value = ParseUnary();
                RequireNumber(value, "unary -");

                if (value.Type == ValueType.Integer)
                    return Value.Int(checked(-value.AsInt()));

                return Value.Float(-value.AsNumber());
            }

            return ParsePrimary();
        }

        private Value ParsePrimary()
        {
            Token token = Current;

            if (token.Type == TokenType.Number)
            {
                Advance();

                if (token.Text.Contains("."))
                {
                    if (double.TryParse(token.Text,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out double f))
                        return Value.Float(f);
                }
                else if (long.TryParse(token.Text,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture, out long n))
                {
                    return Value.Int(n);
                }

                throw new Exception(
                    $"Invalid or out-of-range number '{token.Text}'.");
            }

            if (token.Type == TokenType.String)
            {
                Advance();
                return Value.Str(token.Text);
            }

            if (token.Type == TokenType.Identifier)
            {
                Advance();

                if (token.Text == "True") return Value.Bool(true);
                if (token.Text == "False") return Value.Bool(false);

                if (token.Text == "and" || token.Text == "or" ||
                    token.Text == "not")
                    throw new Exception(
                        $"Expected a value, not '{token.Text}'.");

                if (!variables.TryGetValue(token.Text, out Value value))
                    throw new Exception(
                        $"Variable '{token.Text}' is not defined.");

                return value;
            }

            if (token.Type == TokenType.LeftParen)
            {
                Advance();
                Value value = ParseExpression();
                Expect(TokenType.RightParen, ")");
                return value;
            }

            throw new Exception(
                $"Expected a value, but found '{token.Text}'.");
        }

        private static Value NumericOperation(
            Value left, Value right, string op)
        {
            RequireNumber(left, op);
            RequireNumber(right, op);

            if (op == "/" || op == "//" || op == "%")
            {
                double a = left.AsNumber();
                double b = right.AsNumber();

                if (b == 0)
                    throw new Exception("Division by zero.");

                if (op == "/")
                    return Value.Float(a / b);

                double quotient = Math.Floor(a / b);

                if (op == "//")
                    return MakeNumber(quotient);

                return MakeNumber(a - quotient * b);
            }

            if (left.Type == ValueType.Integer &&
                right.Type == ValueType.Integer)
            {
                long a = left.AsInt();
                long b = right.AsInt();

                switch (op)
                {
                    case "+":
                        return Value.Int(checked(a + b));
                    case "-":
                        return Value.Int(checked(a - b));
                    case "*":
                        return Value.Int(checked(a * b));
                }
            }

            double x = left.AsNumber();
            double y = right.AsNumber();

            switch (op)
            {
                case "+": return MakeNumber(x + y);
                case "-": return MakeNumber(x - y);
                case "*": return MakeNumber(x * y);
            }

            throw new Exception($"Unsupported operator '{op}'.");
        }

        private static Value MakeNumber(double number)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw new Exception("Invalid numeric result.");

            if (number >= long.MinValue &&
                number < 9223372036854775808.0 &&
                number == Math.Truncate(number))
                return Value.Int((long)number);

            return Value.Float(number);
        }

        private static void RequireNumber(Value value, string op)
        {
            if (!value.IsNumber)
                throw new Exception(
                    $"Operator '{op}' requires numbers.");
        }

        private static bool RequireBool(Value value, string op)
        {
            if (value.Type != ValueType.Boolean)
                throw new Exception(
                    $"Operator '{op}' requires Boolean values.");

            return value.AsBool();
        }

        private bool IsOperator(string op) =>
            Current.Type == TokenType.Operator &&
            Current.Text == op;

        private bool IsWord(string word) =>
            Current.Type == TokenType.Identifier &&
            Current.Text == word;
    }
}
