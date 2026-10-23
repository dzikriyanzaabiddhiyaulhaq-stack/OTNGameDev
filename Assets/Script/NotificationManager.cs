using System;
using TMPro;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text UIText;

    private MissionManager missionManager;
    private MissionDefinition mission;
    private int missionIndex;

    private void Start()
    {
        UIText.text = "New contracts available";

        missionManager = FindFirstObjectByType<MissionManager>();

        if (missionManager == null)
        {
            Debug.LogWarning(
                "NotificationManager could not find MissionManager."
            );
            return;
        }

        missionManager.MissionAccepted += NotificationAvailable;
    }

    private void NotificationAvailable(
        MissionManager manager,
        MissionDefinition definition,
        int index)
    {
        missionManager = manager;
        mission = definition;
        missionIndex = index;

        UIText.text = $"New email from: {mission.SenderName}";
    }

    private void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.MissionAccepted -= NotificationAvailable;
        }
    }

    public void CloseNotification()
    {
        // Play animation here...
    }
}
