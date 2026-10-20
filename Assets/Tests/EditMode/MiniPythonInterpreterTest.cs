using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MiniPythonInterpreterTest
{
    private MiniPythonInterpreter interpreter;

    [SetUp]
    public void SetUp()
    {
        interpreter = new MiniPythonInterpreter();
    }

    private void ExpectedOutput(string code, string expected)
    {
        Assert.AreEqual(expected, interpreter.Run(code));
    }

    // 1. Integer Arithmetic
    [TestCase("print(2 + 3)", "5\n")]
    [TestCase("print(10 - 4)", "6\n")]
    [TestCase("print(6 * 7)", "42\n")]
    [TestCase("print(10 / 4)", "2.5\n")]
    [TestCase("print(10 // 3)", "3\n")]
    [TestCase("print(10 % 3)", "1\n")]
    [TestCase("print(2 + 3 * 4)", "14\n")]
    [TestCase("print((2 + 3) * 4)", "20\n")]
    [TestCase("print(-7 // 3)", "-3\n")]
    public void ArithmeticWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 2. Variables and Assignment
    [TestCase("score = 100\nprint(score)", "100\n")]
    [TestCase("score = 10\nscore = score + 5\nprint(score)", "15\n")]
    [TestCase("a = 4\nb = 5\nprint(a * b)", "20\n")]
    [TestCase("x = 8\ny = x / 2\nprint(y)", "4.0\n")]
    public void VariablesWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 3. Strings
    [TestCase("print(\"Hello World!\")", "Hello World!\n")]
    [TestCase("message = \"Hello\"\nprint(message)", "Hello\n")]
    [TestCase("print(\"Hello \" + \"Ondel\")", "Hello Ondel\n")]
    [TestCase("print(\"ha\" * 3)", "hahaha\n")]
    [TestCase("print(\"It's working!\")", "It's working!\n")]
    [TestCase("print(\"line1\\nline2\")", "line1\nline2\n")]
    [TestCase("print(\"a#b\")", "a#b\n")]
    public void StringsWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 4. Boolean Values and Comparisons
    [TestCase("print(True)", "True\n")]
    [TestCase("print(False)", "False\n")]
    [TestCase("print(5 > 3)", "True\n")]
    [TestCase("print(5 == 3)", "False\n")]
    [TestCase("print(5 >= 5)", "True\n")]
    [TestCase("print(not False)", "True\n")]
    [TestCase("print(True and False)", "False\n")]
    [TestCase("print(True or False)", "True\n")]
    [TestCase("print(\"cat\" == \"cat\")", "True\n")]
    public void BooleansAndComparisonsWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 5. Conditionals
    [TestCase(
    "if True:\n    print(\"yes\")", "yes\n")]
    [TestCase(
    "if False:\n    print(\"yes\")\nelse:\n    print(\"no\")", "no\n")]
    [TestCase(
    "score = 75\nif score >= 50:\n    print(\"pass\")", "pass\n")]
    [TestCase(
    "score = 30\nif score >= 50:\n    print(\"pass\")\nelse:\n    print(\"fail\")", "fail\n")]
    [TestCase(
    "score = 50\nif score > 50:\n    print(\"high\")\nelif score == 50:\n    print(\"exact\")\nelse:\n    print(\"low\")", "exact\n")]
    [TestCase(
    "if True:\n    if False:\n        print(\"wrong\")\n    else:\n        print(\"nested\")", "nested\n")]
    [TestCase(
    "if False:\n    print(\"first\")\nelif True:\n    print(\"second\")\nelif True:\n    print(\"third\")\nelse:\n    print(\"last\")", "second\n")]
    public void ConditionalsWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 6. While Loop
    [TestCase("count = 0\nwhile count < 3:\n    print(count)\n    count = count + 1", "0\n1\n2\n")]
    [TestCase("while False:\n    print(\"no\")\nprint(\"done\")", "done\n")]
    [TestCase("count = 0\nwhile count < 2:\n    count = count + 1\n    if count == 1:\n        print(\"one\")\n    else:\n        print(\"two\")", "one\ntwo\n")]
    public void WhileLoopsWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 7. For Loop and Range
    [TestCase("for i in range(5):\n    print(i)", "0\n1\n2\n3\n4\n")]
    [TestCase("for i in range(2, 5):\n    print(i)", "2\n3\n4\n")]
    [TestCase("for i in range(5, 0, -2):\n    print(i)", "5\n3\n1\n")]
    [TestCase("for i in range(0):\n    print(i)\nprint(\"done\")", "done\n")]
    [TestCase("limit = 3\nfor i in range(limit):\n    print(i + 10)", "10\n11\n12\n")]
    public void ForLoopsAndRangeWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 8. Nested For Loops
    [TestCase("for i in range (2):\n    for j in range(2):\n        print(i * 10 + j)", "0\n1\n10\n11\n")]
    public void NestedForLoopsWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 9. For Loops And If Statements
    [Test]
    public void ForLoopCanUseIfStatements()
    {
        ExpectedOutput(
            "for i in range(4):\n" +
            "    if i % 2 == 0:\n" +
            "        print(i)",
            "0\n2\n");
    }
    
    // Input Integer
    [Test]
    public void InputIntWorks()
    {
        string code =
            "first = int(input())\n" +
            "second = int(input())\n" +
            "print(first + second)";

        string result = interpreter.Run(
            code,
            new string[] { "7", "3" }
        );

        Assert.AreEqual("10\n", result);
    }

    [Test]
    public void InputFloatWorks()
    {
        string code =
        "price = float(input())\n" +
        "print(price * 2)";

        string result = interpreter.Run(
            code,
            new string[] { "2.5" }
        );

        Assert.AreEqual("5.0\n", result);
    }

    [Test]
    public void FloatPreservesDecimalType()
    {
        string result = interpreter.Run(
        "print(float(\"2\"))"
        );

        Assert.AreEqual("2.0\n", result);
    }

    [Test]
    public void InputCanBeUsedDirectlyAsString()
    {
        string result = interpreter.Run(
            "name = input()\nprint(name)",
            new string[] { "Kancil" }
        );

        Assert.AreEqual("Kancil\n", result);
    }

    // 10. Comments and Multiple Statements
    [TestCase("# comment\nprint(7)", "7\n")]
    [TestCase("x = 5 # assign a value\nprint(x)", "5\n")]
    [TestCase("print(1)\nprint(2)\nprint(3)", "1\n2\n3\n")]
    public void CommentsAndMultipleLinesWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 1. Invalid Codes
    [TestCase("print(missing)", "Variable 'missing' is not defined.")]
    [TestCase("print(5 / 0)", "Division by zero.")]
    [TestCase("print(\"hello\" - \"world\")", "Operator '-' requires numbers.")]
    [TestCase("print(\"unterminated)", "Unterminated string.")]
    [TestCase("print(1 + )", "Expected a value")]
    public void InvalidCodeErrorsWorks(string code, string expected)
    {
        string result = interpreter.Run(code);

        StringAssert.Contains("Line 1:", result);
        StringAssert.Contains(expected, result);
    }

    // 2. Invalid Conditionals
    [TestCase("if True\n    print(\"yes\")", "Expected ':'")]
    [TestCase("if True:\nprint(\"yes\")", "Expected an indented block")]
    [TestCase("else:\n    print(\"orphan\")", "no matching 'if'")]
    [TestCase("if 5:\n    print(\"yes\")", "must evaluate to True or False")]
    [TestCase("if True:\n    print(\"yes\")\n  print(\"wrong\")", "Indent blocks")]
    [TestCase("if True:\n    print(\"yes\")\nelse:\n    print(\"no\")\nelif False:\n    print(\"late\")", "cannot follow 'else'")]
    public void InvalidConditionalsReportErrors(string code, string expected)
    {
        string result = interpreter.Run(code);

        StringAssert.Contains("Line ", result);
        StringAssert.Contains(expected, result);
    }

    // 3. Invalid While Loops
    [TestCase("while True\n    print(\"hello\")", "Expected ':'")]
    [TestCase("while True:", "Expected an indented block")]
    [TestCase("while 5:\n    print(\"hello\")", "must evaluate to True or False")]
    public void InvalidWhileLoopsReportsErrors(string code, string expected)
    {
        string result = interpreter.Run(code);
        StringAssert.Contains(expected, result);
    }

    // 4. Invalid For Loops
    [TestCase("for i in range(3)", "Expected 'for variable in range(...):'")]
    [TestCase("for i in range():", "range() expects")]
    [TestCase("for i in range(1, 2, 3, 4):", "range() expects")]
    [TestCase("for i in range(3, 0, 0):\n    print(i)", "step cannot be zero")]
    [TestCase("for i in range(3.5):\n    print(i)", "must be integers")]
    [TestCase("for i in range(3):", "Expected an indented block")]
    public void InvalidForLoopsReportsError(string code, string expected)
    {
        string result = interpreter.Run(code);
        StringAssert.Contains(expected, result);
    }

    [Test]
    public void ArbitraryFunctionCallsAreRejected()
    {
        string result = interpreter.Run("open(\"test.txt\")");

        StringAssert.Contains("Unsupported statement", result);
    }

    [Test]
    public void IntegerOverflowProducesError()
    {
        string result = interpreter.Run("print(9223372036854775807 + 1)");
        StringAssert.Contains("Line 1:", result);
    }

    [Test]
    public void InfiniteWhileLoopsAtExecutionLimits()
    {
        string result = interpreter.Run("while True:\n    print(\"loop\")");
        StringAssert.Contains("Execution limit exceeded", result);
    }

    [Test]
    public void LargeForLoopRespectsExecutionLimit()
    {
        string result = interpreter.Run(
            "for i in range(100000):\n    print(i)");

        StringAssert.Contains("Execution limit exceeded", result);
    }

    [Test]
    public void InputWithoutEnoughValuesReportsError()
    {
        string result = interpreter.Run(
            "first = input()\nsecond = input()",
            new string[] { "hello" }
        );

        StringAssert.Contains(
            "input() requested more values",
            result
        );
    }
}