using System;
using System.Collections.Generic;
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

    [Header("Hidden Test Cases")]
    [Tooltip("Enter each input() value on a separate line.")]
    public List<MissionTestCase> TestCases =
        new List<MissionTestCase>();
}

[Serializable]
public class MissionTestCase
{
    [TextArea(1, 4)]
    public string Input;

    [TextArea(1, 4)]
    public string ExpectedOutput;
}
