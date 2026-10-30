using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Ending
{
    [Tooltip("This ending is used when the company score is at least this value.")]
    public int minScore;
    public string title;
    [TextArea(3, 10)] public string text;
}

[CreateAssetMenu(fileName = "Campaign", menuName = "ATS/Campaign")]
public class CampaignData : ScriptableObject
{
    public string companyName = "Synergix Global Solutions";
    public List<DayData> days = new List<DayData>();

    [Tooltip("HR memo shown when the player hires nobody that day.")]
    [TextArea(2, 6)] public string emptyPositionMemo;
    public int emptyPositionPerformance = -1;

    [Tooltip("Checked from the highest minScore down.")]
    public List<Ending> endings = new List<Ending>();

    public Ending GetEnding(int score)
    {
        Ending best = null;
        // Find the highest ending threshold that the player's score reached.
        foreach (Ending ending in endings)
        {
            if (score >= ending.minScore && (best == null || ending.minScore > best.minScore))
            {
                best = ending;
            }
        }

        return best;
    }
}
