using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AppUI.UI;

public class MissionManager : MonoBehaviour
{
    [Header("Missions")]
    [SerializeField]
    private List<MissionDefinition> missions =
        new List<MissionDefinition>();

    [Header("Mission UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text[] SenderName;
    [SerializeField] private TMP_Text[] ShortDescription;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text hintText;

    [Header("Mail UI")]
    public GameObject Mail;

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

        /*if (titleText != null)
        {
            foreach(TMP_Text textcomponent in titleText)
            {
                textcomponent.text = mission.MissionTitle;
            }
        }*/

        if (SenderName != null)
        {
            foreach(TMP_Text textcomponent in SenderName)
            {
                textcomponent.text = mission.SenderName;
            }
        }
        
        if (ShortDescription != null)
        {
            foreach(TMP_Text textcomponent in ShortDescription)
            {
                textcomponent.text = mission.ShortDescription;    
            }
        }

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

        if (!Mail.activeInHierarchy)
            Mail.SetActive(true);
    }

    private void ShowNoMissions()
    {
        Mail.SetActive(false);
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
