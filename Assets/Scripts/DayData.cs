using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Day", menuName = "ATS/Day")]
public class DayData : ScriptableObject
{
    public string positionTitle;
    [TextArea(3, 8)] public string jobDescription;

    [Tooltip("Memo from the boss shown at the start of the day.")]
    [TextArea(3, 10)] public string bossMemo;

    [Tooltip("Rules added to the hiring manual today. Earlier days' rules stay in the manual.")]
    [TextArea(1, 3)] public List<string> newRules = new List<string>();

    public List<ResumeData> applicants = new List<ResumeData>();
}
