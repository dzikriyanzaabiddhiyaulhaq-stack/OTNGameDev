using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(
    fileName = "NewMission",
    menuName = "Mission/Mission Definition"
)]

public class MissionDefinition : ScriptableObject
{
    [Header("Mail")]
    public Sprite SenderImage;
    public string SenderName;
    public string ShortDescription;

    [Header("Identity")]
    public string MissionId;
    public string MissionTitle;

    [Header("Player Instructions")]
    [TextArea(2, 5)]
    public string Objective; // Put this in the detailed description of the mail.

    [TextArea(1, 4)]
    public string Hint;

    [TextArea(4, 4)]
    public string PlaceholderCode;

    [TextArea(4, 4)]
    public string Solution;

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
