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
    [TestCase("x = 8\ny = x / 2\nprint(y)", "4\n")]
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

    // 6. Comments and Multiple Statements
    [TestCase("# comment\nprint(7)", "7\n")]
    [TestCase("x = 5 # assign a value\nprint(x)", "5\n")]
    [TestCase("print(1)\nprint(2)\nprint(3)", "1\n2\n3\n")]
    public void CommentsAndMultipleLinesWorks(string code, string expected)
    {
        ExpectedOutput(code, expected);
    }

    // 7. Invalid Codes
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

    // 8. Invalid Conditionals
    [TestCase("if True\n    print(\"yes\")", "Expected ':'")]
    [TestCase("if True:\nprint(\"yes\")", "Expected an indented block")]
    [TestCase("else:\n    print(\"orphan\")", "no matching 'if'")]
    [TestCase("if 5:\n    print(\"yes\")", "must evaluate to True or False")]
    [TestCase("if True:\n    print(\"yes\")\n  print(\"wrong\")", "Indent blocks")]
    [TestCase("if True:\n    print(\"yes\")\nelse:\n    print(\"no\")\nelif False:\n    print(\"late\")", "cannot follow 'else'")]
    public void InvalidConditionalsReportErrors(string code, string expectedError)
    {
        string result = interpreter.Run(code);

        StringAssert.Contains("Line ", result);
        StringAssert.Contains(expectedError, result);
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
}