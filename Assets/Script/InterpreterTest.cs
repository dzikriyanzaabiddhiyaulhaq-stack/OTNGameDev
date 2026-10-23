using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;

public class InterpreterTest : MonoBehaviour
{
    [SerializeField] private TMP_InputField CodeInput;
    [SerializeField] private TMP_Text OutputText;
    [SerializeField] private MissionManager MissionManager;

    private readonly MiniPythonInterpreter interpreter =
        new MiniPythonInterpreter();

    public void RunCode()
    {
        if (CodeInput == null)
        {
            Debug.LogError("Code input is missing.");
            return;
        }

        // No mission selected? Run as a sandbox.
        if (MissionManager == null ||
            MissionManager.CurrentMission == null)
        {
            RunSandbox();
            return;
        }

        RunMissionTests();
    }

    private void RunSandbox()
    {
        string output = interpreter.Run(
            CodeInput.text,
            new List<string>()
        );

        DisplayOutput(output);

        Debug.Log("=== Sandbox Output ===\n" + output);
    }

    private void RunMissionTests()
    {
        MissionDefinition mission = MissionManager.CurrentMission;

        if (MissionManager.IsCompleted)
        {
            Debug.Log("Mission already completed: " + mission.MissionTitle);
            return;
        }

        if (mission.TestCases == null || mission.TestCases.Count == 0)
        {
            Debug.LogError(
                "Mission has no test cases: " + mission.MissionTitle
            );
            return;
        }

        int passedTests = 0;
        int totalTests = mission.TestCases.Count;

        string displayedOutput = "(No output produced)";

        Debug.Log($"=== Running Mission: {mission.MissionTitle} ===");
        Debug.Log("Player code:\n" + CodeInput.text);

        for (int i = 0; i < totalTests; i++)
        {
            MissionTestCase testCase = mission.TestCases[i];

            if (testCase == null)
                continue;

            List<string> inputs = new List<string>();

            if (!string.IsNullOrEmpty(testCase.Input))
            {
                string normalizedInput = testCase.Input
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n');

                inputs.AddRange(normalizedInput.Split('\n'));
            }

            string actualOutput = interpreter.Run(
                CodeInput.text,
                inputs
            );

            if (i == 0)
            {
                displayedOutput = string.IsNullOrEmpty(actualOutput)
                    ? "(No output produced)"
                    : actualOutput;
            }

            bool isError = actualOutput != null &&
                (actualOutput.StartsWith(
                    "Line ", StringComparison.Ordinal) ||
                 actualOutput.StartsWith(
                    "Interpreter error:", StringComparison.Ordinal));

            bool passed = !isError &&
                Normalize(actualOutput) ==
                Normalize(testCase.ExpectedOutput);

            if (passed)
                passedTests++;

            Debug.Log(
                $"[{(passed ? "PASS" : "FAIL")}] Test Case {i + 1}\n" +
                $"Input:\n{testCase.Input}\n" +
                $"Expected Output:\n{testCase.ExpectedOutput}\n" +
                $"Actual Output:\n{actualOutput}"
            );
        }

        // Show output first, then the test summary.
        DisplayOutput(displayedOutput);

        Debug.Log(
            $"=== Program Output (Test Case 1) ===\n{displayedOutput}"
        );

        bool allPassed = passedTests == totalTests;

        Debug.Log(
            $"=== Test Results: {passedTests}/{totalTests} passed ==="
        );

        if (allPassed)
            Debug.Log($"MISSION COMPLETE: {mission.MissionTitle}");
        else
            Debug.LogWarning($"MISSION INCOMPLETE: {mission.MissionTitle}");

        MissionManager.CheckMissionResult(allPassed);
    }

    private void DisplayOutput(string output)
    {
        string displayedOutput = string.IsNullOrEmpty(output)
            ? "(No output produced)"
            : output;

        if (OutputText != null)
            OutputText.text = displayedOutput;
    }

    private string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimEnd('\n');
    }
}