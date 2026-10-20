using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMission",
    menuName = "Mission/Mission Definition"
)]

public class MissionDefinition : ScriptableObject
{
    [Header("Identity")]
    public string MissionId;
    public string MissionTitle;

    [Header("Player Instructions")]
    [TextArea(2, 5)]
    public string Objective;

    [TextArea(1, 4)]
    public string Hint;

    [Header("Completion")]
    [TextArea(1, 5)]
    public string ExpectedOutput;
}