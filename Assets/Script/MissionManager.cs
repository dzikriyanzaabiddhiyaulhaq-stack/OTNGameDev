using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Sequence")]
    [SerializeField] private List<MissionDefinition> missions = new List<MissionDefinition>();

    [Header("Mission UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text hintText;

    public int CurrentMissionIndex { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool AllMissionsCompleted { get; private set; }

    public MissionDefinition CurrentMission
    {
        get
        {
            if (CurrentMissionIndex < 0 || CurrentMissionIndex >= missions.Count)
            {
                return null;
            }
            return missions[CurrentMissionIndex];
        }
    }

    public event Action<MissionDefinition> MissionCompleted;
    public event Action AllMissionsCompletedEvent;

    private void Start()
    {
        ShowCurrentMission();
    }

    private void ShowCurrentMission()
    {
        MissionDefinition mission = CurrentMission;

        if (mission == null)
        {
            if (titleText != null)
                titleText.text = "No Missions Available";

            if (objectiveText != null)
                objectiveText.text =
                    "Add mission assets to the Mission Manager.";

            if (feedbackText != null)
                feedbackText.text = "";

            if (hintText != null)
                hintText.text = "";

            return;
        }

        if (titleText != null)
            titleText.text = mission.MissionTitle;

        if (objectiveText != null)
            objectiveText.text = mission.Objective;

        if (feedbackText != null)
            feedbackText.text = "Mission in progress.";

        if (hintText != null)
            hintText.text = "";
    }

    public void ContinueToNextMission()
    {
        if (!IsCompleted || AllMissionsCompleted)
            return;

        CurrentMissionIndex++;
        IsCompleted = false;

        if (CurrentMissionIndex >= missions.Count)
        {
            AllMissionsCompleted = true;

            if (titleText != null)
                titleText.text = "All Missions Complete!";

            if (objectiveText != null)
                objectiveText.text =
                    "You've completed every available mission.";

            if (feedbackText != null)
                feedbackText.text = "Congratulations!";

            if (hintText != null)
                hintText.text = "";

            AllMissionsCompletedEvent?.Invoke();
            return;
        }

        ShowCurrentMission();
    }

    public void ShowHint()
    {
        MissionDefinition mission = CurrentMission;

        if (mission == null || hintText == null)
            return;

        hintText.text = mission.Hint;
    }

    public void CheckOutput(string output)
    {
        if (IsCompleted || AllMissionsCompleted)
            return;

        MissionDefinition mission = CurrentMission;

        if (mission == null)
            return;

        if (Normalize(output) == Normalize(mission.ExpectedOutput))
        {
            IsCompleted = true;

            if (feedbackText != null)
                feedbackText.text = "Mission complete!";

            MissionCompleted?.Invoke(mission);

            Debug.Log($"Mission completed: {mission.MissionTitle}");
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text =
                    "Not quite! Check your output and try again.";
            }
        }
    }

    private string Normalize(string value)
    {
        return(value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
    }
}
