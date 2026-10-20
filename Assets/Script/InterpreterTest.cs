using TMPro;
using UnityEditor;
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
        if (CodeInput == null ||
            OutputText == null ||
            MissionManager == null ||
            MissionManager.CurrentMission == null)
        {
            Debug.Log("No mission selected or code input missing.");
            return;
        }

        MissionDefinition mission = MissionManager.CurrentMission;

        if (MissionManager.IsCompleted)
        {
            OutputText.text = "This mission is already completed.";
            Debug.Log("Mission already completed: " + mission.MissionTitle);
            return;
        }

        if (mission.TestCases == null ||
            mission.TestCases.Count == 0)
        {
            OutputText.text = "This mission has no test cases.";
            Debug.LogError("Mission has no test cases: " + mission.MissionTitle);
            return;
        }

        int passedTests = 0;
        int totalTests = mission.TestCases.Count;
        Debug.Log($"=== Running Mission: {mission.MissionTitle} ===");
        Debug.Log("Player code:\n" + CodeInput.text);

        foreach (MissionTestCase testCase in mission.TestCases)
        {
            if (testCase == null)
                continue;

            List<string> inputs = ParseInputs(testCase.Input);

            string actualOutput = interpreter.Run(
                CodeInput.text,
                inputs
            );

            // The interpreter returns errors as strings.
            // They must not count as successful output.
            if (IsInterpreterError(actualOutput))
                continue;

            if (Normalize(actualOutput) ==
                Normalize(testCase.ExpectedOutput))
            {
                passedTests++;

                Debug.Log(
                //$"[{(passed ? "PASS" : "FAIL")}] " +
                $"Test Case {mission.TestCases.IndexOf(testCase) + 1}\n" +
                $"Input:\n{testCase.Input}\n" +
                $"Expected Output:\n{testCase.ExpectedOutput}\n" +
                $"Actual Output:\n{actualOutput}");
            }
        }

        bool allPassed = passedTests == totalTests;

        Debug.Log(
            $"=== Mission Result: {passedTests}/{totalTests} tests passed ==="
        );

        OutputText.text =
            $"Tests passed: {passedTests}/{totalTests}";

        if (allPassed)
            Debug.Log($"MISSION COMPLETE: {mission.MissionTitle}");
        else
            Debug.LogWarning($"MISSION INCOMPLETE: {mission.MissionTitle}");

        MissionManager.CheckMissionResult(allPassed);
    }

    private List<string> ParseInputs(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new List<string>();

        string normalized = input
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        return new List<string>(normalized.Split('\n'));
    }

    private bool IsInterpreterError(string output)
    {
        return output != null &&
            (output.StartsWith(
                "Line ", StringComparison.Ordinal) ||
             output.StartsWith(
                "Interpreter error:", StringComparison.Ordinal));
    }

    private string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimEnd('\n');
    }
}