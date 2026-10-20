using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using UnityEngine;

public sealed class MiniPythonInterpreter
{
    private sealed class ScriptException : Exception
    {
        public int Line { get; }

        public ScriptException(int line, string message) : base(message)
        {
            Line = line;
        }
    }

    private sealed class SourceLine
    {
        public int Number;
        public int Indent;
        public string Text;
    }

    private abstract class Statement
    {
        public int Line;
    }

    private sealed class SimpleStatement : Statement
    {
        public string Source;
    }

    private sealed class IfBranch
    {
        public string Condition;
        public int Line;
        public List<Statement> Body = new List<Statement>();
    }

    private sealed class IfStatement : Statement
    {
        public List<IfBranch> Branches = new List<IfBranch>();
    }

    private sealed class WhileStatement : Statement
    {
        public string Condition;
        public List<Statement> Body = new List<Statement>();
    }

    private sealed class ForStatement : Statement
    {
        public string Variable;
        public List<string> RangeArguments = new List<string>();
        public List<Statement> Body = new List<Statement>();
    }

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

    private const int MaxStatementExecutions = 10000;
    private int statementExecutions;

    private readonly Dictionary<string, Value> variables =
        new Dictionary<string, Value>();

    public string Run(string source)
    {
        variables.Clear();
        statementExecutions = 0;

        try
        {
            var lines = PrepareSource(source ?? "");
            int position = 0;

            List<Statement> program = ParseBlock(lines, ref position, 0);

            if (position < lines.Count)
            {
                throw new ScriptException(
                    lines[position].Number,
                    "Unexpected indentation.");
            }

            var output = new StringBuilder();
            ExecuteBlock(program, output);

            return output.ToString();
        }
        catch (ScriptException ex)
        {
            return $"Line {ex.Line}: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Interpreter error: {ex.Message}";
        }
    }

    private List<SourceLine> PrepareSource(string source)
    {
        var result = new List<SourceLine>();

        string[] rawLines = source
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        for (int i = 0; i < rawLines.Length; i++)
        {
            string raw = rawLines[i];

            if (raw.Contains("\t"))
            {
                throw new ScriptException(
                    i + 1,
                    "Use spaces for indentation, not tabs.");
            }

            int indent = 0;

            while (indent < raw.Length && raw[indent] == ' ')
                indent++;

            string text = raw.Substring(indent).TrimEnd();

            if (string.IsNullOrWhiteSpace(text) ||
                text.StartsWith("#"))
            {
                continue;
            }

            result.Add(new SourceLine
            {
                Number = i + 1,
                Indent = indent,
                Text = text
            });
        }
        return result;
    }

    private static bool HasKeyword(string text, string keyword)
    {
        return text == keyword ||
            (text.StartsWith(keyword, StringComparison.Ordinal) &&
             text.Length > keyword.Length &&
             (char.IsWhiteSpace(text[keyword.Length]) ||
              text[keyword.Length] == ':'));
    }

    private List<Statement> ParseBlock(
        List<SourceLine> lines,
        ref int position,
        int expectedIndent)
        {
            var statements = new List<Statement>();

            while (position < lines.Count)
            {
                SourceLine line = lines[position];

                if (line.Indent < expectedIndent)
                    break;

                if (line.Indent > expectedIndent)
                {
                    throw new ScriptException(
                        line.Number,
                        "Indent blocks using exactly 4 spaces per level.");
                }

                if (HasKeyword(line.Text, "elif") ||
                    HasKeyword(line.Text, "else"))
                {
                    throw new ScriptException(
                        line.Number,
                        $"'{line.Text.Split(' ')[0]}' has no matching 'if'.");
                }

                if (HasKeyword(line.Text, "if"))
                {
                    statements.Add(ParseIfStatement(lines, ref position));
                    continue;
                }

                if (HasKeyword(line.Text, "while"))
                {
                    statements.Add(ParseWhileStatement(lines, ref position));
                    continue;
                }

                if (HasKeyword(line.Text, "for"))
                {
                    statements.Add(ParseForStatement(lines, ref position));
                    continue;
                }

                statements.Add(new SimpleStatement
                {
                    Line = line.Number,
                    Source = line.Text
                });

                position++;
            }
        return statements;
    }

    private IfStatement ParseIfStatement(
        List<SourceLine> lines,
        ref int position)
    {
        SourceLine firstLine = lines[position];
        int baseIndent = firstLine.Indent;

        var result = new IfStatement
        {
            Line = firstLine.Number
        };

        bool sawElse = false;

        while (position < lines.Count)
        {
            SourceLine header = lines[position];

            if (header.Indent != baseIndent)
                break;

            bool isIf = HasKeyword(header.Text, "if");
            bool isElif = HasKeyword(header.Text, "elif");
            bool isElse = HasKeyword(header.Text, "else");

            if (!isIf && !isElif && !isElse)
                break;

            if (result.Branches.Count == 0 && !isIf)
            {
                throw new ScriptException(
                    header.Number,
                    "A conditional chain must begin with 'if'.");
            }

            if (result.Branches.Count > 0 && isIf)
                break;

            if (sawElse)
            {
                throw new ScriptException(
                    header.Number,
                    "'elif' or 'else' cannot follow 'else'.");
            }

            string condition = null;

            if (isElse)
            {
                if (header.Text != "else:")
                {
                    throw new ScriptException(
                        header.Number,
                        "Expected 'else:'.");
                }

                sawElse = true;
            }
            else
            {
                string keyword = isIf ? "if" : "elif";

                if (!header.Text.EndsWith(":"))
                {
                    throw new ScriptException(
                        header.Number,
                        $"Expected ':' after '{keyword}' condition.");
                }

                condition = header.Text.Substring(
                    keyword.Length,
                    header.Text.Length - keyword.Length - 1
                ).Trim();

                if (condition.Length == 0)
                {
                    throw new ScriptException(
                        header.Number,
                        $"Expected a condition after '{keyword}'.");
                }
            }

            position++;

            if (position >= lines.Count ||
                lines[position].Indent <= baseIndent)
            {
                throw new ScriptException(
                    header.Number,
                    "Expected an indented block after conditional.");
            }

            if (lines[position].Indent != baseIndent + 4)
            {
                throw new ScriptException(
                    lines[position].Number,
                    "Indent blocks using exactly 4 spaces per level.");
            }

            List<Statement> body = ParseBlock(
                lines, ref position, baseIndent + 4);

            if (body.Count == 0)
            {
                throw new ScriptException(
                    header.Number,
                    "Conditional block cannot be empty.");
            }

            result.Branches.Add(new IfBranch
            {
                Condition = condition,
                Line = header.Number,
                Body = body
            });

            if (sawElse)
                break;

            // Consume another elif/else only if it is at
            // the same indentation level.
            if (position >= lines.Count ||
                lines[position].Indent != baseIndent ||
                (!HasKeyword(lines[position].Text, "elif") &&
                 !HasKeyword(lines[position].Text, "else")))
            {
                break;
            }
        }

        // An else branch must be last, even if another branch
        // follows after the else body.
        if (sawElse && position < lines.Count &&
            lines[position].Indent == baseIndent &&
            (HasKeyword(lines[position].Text, "elif") ||
             HasKeyword(lines[position].Text, "else")))
        {
            throw new ScriptException(
                lines[position].Number,
                "'elif' or 'else' cannot follow 'else'.");
        }

        return result;
    }

    private WhileStatement ParseWhileStatement(
        List<SourceLine> lines,
        ref int position)
    {
        SourceLine header = lines[position];
        int baseIndent = header.Indent;

        if (!header.Text.EndsWith(":"))
        {
            throw new ScriptException(
                header.Number,
                "Expected ':' after 'while' condition.");
        }

        string condition = header.Text.Substring(
            "while".Length,
            header.Text.Length - "while".Length - 1
        ).Trim();

        if (condition.Length == 0)
        {
            throw new ScriptException(
                header.Number,
                "Expected a condition after 'while'.");
        }

        position++;

        if (position >= lines.Count ||
            lines[position].Indent <= baseIndent)
        {
            throw new ScriptException(
                header.Number,
                "Expected an indented block after while.");
        }

        if (lines[position].Indent != baseIndent + 4)
        {
            throw new ScriptException(
                lines[position].Number,
                "Indent blocks using exactly 4 spaces per level.");
        }

        List<Statement> body = ParseBlock(
            lines, ref position, baseIndent + 4);

        if (body.Count == 0)
        {
            throw new ScriptException(
                header.Number,
                "While block cannot be empty.");
        }

        return new WhileStatement
        {
            Line = header.Number,
            Condition = condition,
            Body = body
        };
    }

    private ForStatement ParseForStatement(
        List<SourceLine> lines,
        ref int position)
    {
        SourceLine header = lines[position];
        int baseIndent = header.Indent;

        // Parse the header in separate steps rather than relying on one
        // regular expression. This also makes nested loop headers behave
        // consistently when ParseBlock recursively parses their bodies.
        string headerText = header.Text.Trim();
        const string forPrefix = "for ";
        const string rangeMarker = " in range";

        int rangeMarkerIndex = headerText.IndexOf(
            rangeMarker, StringComparison.Ordinal);
        int openingParen = -1;

        if (rangeMarkerIndex >= 0)
        {
            int afterRange = rangeMarkerIndex + rangeMarker.Length;

            // Allow whitespace between "range" and "(".
            while (afterRange < headerText.Length &&
            char.IsWhiteSpace(headerText[afterRange]))
            {
                afterRange++;
            }

            if (afterRange < headerText.Length &&
                headerText[afterRange] == '(')
            {
                openingParen = afterRange;
            }
        }


        int closingParen = headerText.Length >= 2
            ? headerText.LastIndexOf(')')
            : -1;

        bool validHeader =
            headerText.StartsWith(forPrefix, StringComparison.Ordinal) &&
            headerText.EndsWith(":", StringComparison.Ordinal) &&
            rangeMarkerIndex > forPrefix.Length &&
            openingParen >= 0 &&
            closingParen == headerText.Length - 2 &&
            closingParen > openingParen;

        if (!validHeader)
        {
            throw new ScriptException(
            header.Number,
            $"Invalid for header: [{headerText}] " +
            $"Length={headerText.Length}, " +
            $"StartsWith={headerText.StartsWith(forPrefix, StringComparison.Ordinal)}, " +
            $"EndsWith={headerText.EndsWith(":", StringComparison.Ordinal)}, " +
            $"rangeMarkerIndex={rangeMarkerIndex}, " +
            $"openingParen={openingParen}, " +
            $"closingParen={closingParen}");
        }

        string variable = headerText.Substring(
            forPrefix.Length, rangeMarkerIndex - forPrefix.Length).Trim();

        if (!Regex.IsMatch(variable, @"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.CultureInvariant))
        {
            throw new ScriptException(
                header.Number,
                "Expected a valid loop variable name.");
        }
        if (variable == "True" || variable == "False" ||
            variable == "and" || variable == "or" || variable == "not" ||
            variable == "if" || variable == "elif" || variable == "else" ||
            variable == "while" || variable == "for" || variable == "in" ||
            variable == "range")
        {
            throw new ScriptException(
                header.Number,
                $"'{variable}' is a reserved word and cannot be used as a loop variable.");
        }

        string argumentsText = headerText.Substring(
            openingParen + 1, closingParen - openingParen - 1).Trim();
        string[] arguments = argumentsText.Split(',');
        if (arguments.Length < 1 || arguments.Length > 3 ||
            arguments.Any(argument => string.IsNullOrWhiteSpace(argument)))
        {
            throw new ScriptException(
                header.Number,
                "range() expects one, two, or three arguments.");
        }

        position++;

        if (position >= lines.Count ||
            lines[position].Indent <= baseIndent)
        {
            throw new ScriptException(
                header.Number,
                "Expected an indented block after for.");
        }

        if (lines[position].Indent != baseIndent + 4)
        {
            throw new ScriptException(
                lines[position].Number,
                "Indent blocks using exactly 4 spaces per level.");
        }

        List<Statement> body = ParseBlock(
            lines, ref position, baseIndent + 4);

        if (body.Count == 0)
        {
            throw new ScriptException(
                header.Number,
                "For block cannot be empty.");
        }

        return new ForStatement
        {
            Line = header.Number,
            Variable = variable,
            RangeArguments = arguments.Select(argument => argument.Trim()).ToList(),
            Body = body
        };
    }

    private long EvaluateIntegerExpression(string expression)
    {
        List<Token> tokens = Tokenize(expression);
        var parser = new Parser(tokens, variables);
        Value value = parser.ParseExpression();
        parser.ExpectEnd();

        if (value.Type != ValueType.Integer)
        {
            throw new Exception("range() arguments must be integers.");
        }

        return value.AsInt();
    }

    private bool EvaluateCondition(string expression)
    {
        List<Token> tokens = Tokenize(expression);
        var parser = new Parser(tokens, variables);

        Value value = parser.ParseExpression();
        parser.ExpectEnd();

        if (value.Type != ValueType.Boolean)
        {
            throw new Exception(
                "Conditional expressions must evaluate to True or False.");
        }

        return value.AsBool();
    }

    private void ExecuteBlock(
        List<Statement> statements,
        StringBuilder output)
    {
        foreach (Statement statement in statements)
        {
            statementExecutions++;

            if (statementExecutions > MaxStatementExecutions)
            {
                throw new ScriptException(
                    statement.Line,
                    "Execution limit exceeded. Check for an infinite loop.");
            }

            try
            {
                if (statement is SimpleStatement simple)
                {
                    ExecuteLine(simple.Source, output);
                }
                else if (statement is IfStatement conditional)
                {
                    foreach (IfBranch branch in conditional.Branches)
                    {
                        bool matches = branch.Condition == null ||
                            EvaluateCondition(branch.Condition);

                        if (matches)
                        {
                            ExecuteBlock(branch.Body, output);
                            break;
                        }
                    }
                }
                else if (statement is WhileStatement loop)
                {
                    while (EvaluateCondition(loop.Condition))
                    {
                        ExecuteBlock(loop.Body, output);
                    }
                }
                else if (statement is ForStatement forLoop)
                {
                    long start;
                    long stop;
                    long step;

                    if (forLoop.RangeArguments.Count == 1)
                    {
                        start = 0;
                        stop = EvaluateIntegerExpression(forLoop.RangeArguments[0]);
                        step = 1;
                    }
                    else
                    {
                        start = EvaluateIntegerExpression(forLoop.RangeArguments[0]);
                        stop = EvaluateIntegerExpression(forLoop.RangeArguments[1]);
                        step = forLoop.RangeArguments.Count == 3
                            ? EvaluateIntegerExpression(forLoop.RangeArguments[2])
                            : 1;
                    }

                    if (step == 0)
                    {
                        throw new Exception("range() step cannot be zero.");
                    }

                    long index = start;
                    while (step > 0 ? index < stop : index > stop)
                    {
                        variables[forLoop.Variable] = Value.Int(index);
                        ExecuteBlock(forLoop.Body, output);

                        // Avoid overflowing after the final useful iteration.
                        if (step > 0 && index > long.MaxValue - step)
                            break;
                        if (step < 0 && index < long.MinValue - step)
                            break;

                        index += step;
                    }
                }
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScriptException(statement.Line, ex.Message);
            }
        }
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
