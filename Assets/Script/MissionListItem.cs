using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MissionListItem : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image senderImage;
    [SerializeField] private TMP_Text senderNameText;
    [SerializeField] private TMP_Text senderName;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject finishedPanel;

    private MissionManager missionManager;
    private MissionDefinition mission;
    private int missionIndex;

    public void Initialize(MissionManager manager, MissionDefinition definition, int missionIndex)
    {
        missionManager = manager;
        mission = definition;

        if (senderImage != null)
        {
            senderImage.sprite = mission.SenderImage;
        }

        if (senderNameText != null)
            senderNameText.text = $"From: {mission.SenderName}";

        if (senderName != null)
            senderName.text = mission.SenderName;

        if (descriptionText != null)
            descriptionText.text = mission.ShortDescription;

        selectButton.onClick.RemoveListener(OnSelected);
        selectButton.onClick.AddListener(OnSelected);

        missionManager.MissionCompleted += OnMissionCompleted;
    }

    private void OnSelected()
    {
        if (missionManager != null && mission != null)
            missionManager.SelectMission(mission);
    }

    private void RefreshCompletion()
    {
        if (finishedPanel != null && missionManager != null)
        {
            finishedPanel.SetActive(missionManager.IsMissionCompleted(missionIndex));
        }
    }

    private void OnMissionCompleted(MissionDefinition completedMission)
    {
        if (completedMission == mission)
        {
            RefreshCompletion();
        }
    }

    private void OnDestroy()
    {
        if (missionManager != null)
            missionManager.MissionCompleted -= OnMissionCompleted;

        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelected);
    }
}
