using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using NUnit.Framework;

public class MissionManager : MonoBehaviour
{
    [Header("Missions")]
    [SerializeField]
    private List<MissionDefinition> missions =
        new List<MissionDefinition>();

    [Header("Mission UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text hintText;

    [Header("Code Editor")]
    [SerializeField] private TMP_InputField codeInput;

    private readonly HashSet<string> completedMissionIds =
        new HashSet<string>();

    public int CurrentMissionIndex { get; private set; }

    public MissionDefinition CurrentMission
    {
        get
        {
            if (CurrentMissionIndex < 0 ||
                CurrentMissionIndex >= missions.Count)
                return null;

            return missions[CurrentMissionIndex];
        }
    }

    public bool IsCompleted =>
        CurrentMission != null &&
        completedMissionIds.Contains(CurrentMission.MissionId);

    public bool AllMissionsCompleted =>
        missions.Count > 0 &&
        missions.TrueForAll(m =>
            m != null &&
            completedMissionIds.Contains(m.MissionId));

    public event Action<MissionDefinition> MissionCompleted;
    public event Action AllMissionsCompletedEvent;

    private void Start()
    {
        CurrentMissionIndex = -1;
        ShowNoMissions();
    }

    public void SelectMission(int index)
    {
        Debug.Log($"SelectMission called with index: {index}");
        if (index < 0 || index >= missions.Count ||
            missions[index] == null)
            return;

        CurrentMissionIndex = index;

        MissionDefinition mission = CurrentMission;

        if (titleText != null)
            titleText.text = mission.MissionTitle;

        if (objectiveText != null)
            objectiveText.text = mission.Objective;

        if (codeInput != null)
            codeInput.text = mission.PlaceholderCode;

        if (feedbackText != null)
        {
            feedbackText.text = IsCompleted
                ? "Mission already completed!"
                : "Mission in progress.";
        }

        if (hintText != null)
            hintText.text = "";
    }

    private void ShowNoMissions()
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
    }

    public void ShowHint()
    {
        if (CurrentMission != null && hintText != null)
            hintText.text = CurrentMission.Hint;
    }

    public void CheckMissionResult(bool passed)
    {
        MissionDefinition mission = CurrentMission;

        if (mission == null || IsCompleted)
            return;

        if (!passed)
        {
            if (feedbackText != null)
                feedbackText.text =
                    "Not quite! Check your code and try again.";

            return;
        }

        completedMissionIds.Add(mission.MissionId);

        if (feedbackText != null)
            feedbackText.text = "Mission complete!";

        MissionCompleted?.Invoke(mission);

        Debug.Log($"Mission completed: {mission.MissionTitle}");

        if (AllMissionsCompleted)
        {
            if (feedbackText != null)
                feedbackText.text = "All missions completed!";

            AllMissionsCompletedEvent?.Invoke();
        }
    }

    public bool IsMissionCompleted(int index)
    {
        return index >= 0 &&
               index < missions.Count &&
               missions[index] != null &&
               completedMissionIds.Contains(
                   missions[index].MissionId);
    }
}
