using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class MiniPythonInterpreter
{
    private readonly Dictionary<string, bool> variables =
        new Dictionary<string, bool>();

    private static readonly Regex Identifier = new Regex(
        @"^[A-Za-z_][A-Za-z0-9_]*$"
    );

    public string Run(string source)
    {
        variables.Clear();

        StringBuilder output = new StringBuilder();
        string[] lines = source.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Ignore blank lines and comments.
            if (line.Length == 0 || line.StartsWith("#"))
                continue;

            // Remove inline comments.
            int commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
                line = line.Substring(0, commentIndex).Trim();

            if (line.Length == 0)
                continue;

            try
            {
                ExecuteLine(line, output);
            }
            catch (Exception exception)
            {
                return $"Line {i + 1}: {exception.Message}";
            }
        }

        return output.ToString();
    }

    private void ExecuteLine(string line, StringBuilder output)
    {
        // Support print(variable) and print(True/False).
        if (line.StartsWith("print(") && line.EndsWith(")"))
        {
            string expression = line.Substring(
                6, line.Length - 7
            ).Trim();

            bool value = EvaluateBoolean(expression);
            output.AppendLine(value ? "True" : "False");
            return;
        }

        // Support variable assignments.
        int equalsIndex = line.IndexOf('=');

        if (equalsIndex >= 0)
        {
            // Reject ==, which isn't an assignment.
            if ((equalsIndex > 0 &&
                 line[equalsIndex - 1] == '=') ||
                (equalsIndex + 1 < line.Length &&
                 line[equalsIndex + 1] == '='))
            {
                throw new Exception(
                    "Comparisons aren't supported yet."
                );
            }

            string name = line.Substring(0, equalsIndex).Trim();
            string expression = line.Substring(
                equalsIndex + 1
            ).Trim();

            if (!Identifier.IsMatch(name) ||
                name == "True" || name == "False")
            {
                throw new Exception(
                    "Invalid variable name."
                );
            }

            variables[name] = EvaluateBoolean(expression);
            return;
        }

        throw new Exception(
            "Unsupported syntax. Try a Boolean assignment or print()."
        );
    }

    private bool EvaluateBoolean(string expression)
    {
        if (expression == "True")
            return true;

        if (expression == "False")
            return false;

        if (!Identifier.IsMatch(expression))
        {
            throw new Exception(
                "Expected True, False, or a variable name."
            );
        }

        if (!variables.TryGetValue(expression, out bool value))
        {
            throw new Exception(
                $"Variable '{expression}' is not defined."
            );
        }

        return value;
    }
}
