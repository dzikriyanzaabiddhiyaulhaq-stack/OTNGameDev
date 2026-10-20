using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Details")]
    [SerializeField] private string missionTitle = "Hello, World!";
    [TextArea(2, 4)]
    [SerializeField] private string missionObjective = "Write and run a script that prints Hello, World!";

    [TextArea(1, 3)]
    [SerializeField] private string expectedOutput = "Hello, World!";

    [Header("Mission UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text feedbackText;

    public bool isCompleted { get; private set; }

    public event Action MissionCompleted;

    private void Start()
    {
        if (titleText != null)
            titleText.text = missionTitle;
        
        if (objectiveText != null)
            objectiveText.text = missionObjective;

        if (feedbackText != null)
            objectiveText.text = "Mission in progress...";
    }

    public void CheckOutput(string output)
    {
        if (isCompleted)
            return;

        if (Normalize(output) == Normalize(expectedOutput))
        {
            isCompleted = true;

            if (feedbackText != null)
                feedbackText.text = "Mission complete!";

            MissionCompleted?.Invoke();
            Debug.Log($"Mission completed: {missionTitle}");
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Not quite! Check your output and try again.";
        }
    }

    private string Normalize(string value)
    {
        return(value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
    }
}
