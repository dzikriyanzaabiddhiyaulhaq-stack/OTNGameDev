using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;

public class MissionManager : MonoBehaviour
{
    // This code is a complete clusterfuck.
    // Don't rely too much on AI to do your work, kids.

    [Header("Missions")]
    [SerializeField]
    private List<MissionDefinition> missions =
        new List<MissionDefinition>();

    [Header("Mission UI")]
    [SerializeField] private UnityEngine.UI.Image senderImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text[] SenderName;
    [SerializeField] private TMP_Text[] ShortDescription;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Contract List")]
    [SerializeField] private Transform contractContent;
    [SerializeField] private GameObject contractPrefab;

    [Header("Mail App")]
    public GameObject Mail;
    public GameObject HintSolutionScrollView;
    [SerializeField] private TMP_Text HintSolutionText;

    [Header("Code Editor")]
    [SerializeField] private TMP_InputField codeInput;

    private readonly HashSet<string> completedMissionIds =
        new HashSet<string>();

    public event Action<MissionManager, MissionDefinition, int>
    MissionAccepted;

    private enum HintSolutionMode
    {
        None,
        Hint,
        Solution
    }
    
    private HintSolutionMode currentHintSolutionMode =
        HintSolutionMode.None;

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
        PopulateContracts();
        ShowNoMissions();
    }

    private void PopulateContracts()
    {
        for (int i = 0; i < missions.Count; i++)
        {
            MissionDefinition mission = missions[i];

            if (mission == null)
                continue;

            GameObject contractObject = Instantiate(
                contractPrefab,
                contractContent
            );

            MissionListItem item =
                contractObject.GetComponent<MissionListItem>();

            if (item == null)
            {
                Debug.LogError(
                    "Contract prefab is missing MissionListItem."
                );

                Destroy(contractObject);
                continue;
            }

            item.Initialize(this, mission, i);
        }
    }

    public void SelectMission(MissionDefinition mission)
    {
        int index = missions.IndexOf(mission);

        Debug.Log($"SelectMission called with index: {index}");
        if (index < 0 || index >= missions.Count ||
            missions[index] == null)
            return;

        CurrentMissionIndex = index;

        if (senderImage != null)
        {
            senderImage.sprite = mission.SenderImage;
        }

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
                ? "Contract already completed!"
                : "Contract in progress.";
        }

        if (HintSolutionText != null)
            HintSolutionText.text = "";

        if (!Mail.activeInHierarchy)
            Mail.SetActive(true);

        UpdateAcceptedPanel();

        MissionAccepted?.Invoke(
            this,
            missions[index],
            index
        );
    }

    private void ShowNoMissions()
    {
        Mail.SetActive(false);
    }

    public void ToggleHint()
    {
        if (CurrentMissionIndex < 0 ||
        CurrentMissionIndex >= missions.Count)
        return;

        MissionDefinition mission = missions[CurrentMissionIndex];

        if (currentHintSolutionMode == HintSolutionMode.Hint)
        {
            HintSolutionScrollView.SetActive(false);
            currentHintSolutionMode = HintSolutionMode.None;
            return;
        }

        HintSolutionText.text = mission.Hint;
        HintSolutionScrollView.SetActive(true);
        currentHintSolutionMode = HintSolutionMode.Hint;
    }

    public void ToggleSolution()
    {
        if (CurrentMissionIndex < 0 ||
        CurrentMissionIndex >= missions.Count)
            return;

        MissionDefinition mission = missions[CurrentMissionIndex];

        if (currentHintSolutionMode == HintSolutionMode.Solution)
        {
            HintSolutionScrollView.SetActive(false);
            currentHintSolutionMode = HintSolutionMode.None;
            return;
        }

        HintSolutionText.text = mission.Solution;
        HintSolutionScrollView.SetActive(true);
        currentHintSolutionMode = HintSolutionMode.Solution;
    }


    private void UpdateAcceptedPanel()
    {
        foreach (Transform child in contractContent)
    {
        MissionListItem item =
            child.GetComponent<MissionListItem>();

        if (item == null)
            continue;

        bool isAccepted =
            missions.IndexOf(item.GetMission()) == CurrentMissionIndex;

        item.SetAccepted(isAccepted);
        }
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
