using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum ClaimMark
{
    None,
    Suspicious,
    Good
}

// Shows one sheet of a ResumeData (the resume, or a reference sheet) and lets the player
// click claims to mark them. Each click cycles the claim: None -> Suspicious -> Good -> None.
[RequireComponent(typeof(ClickableText))]
public class ResumeInvestigator : MonoBehaviour
{
    [SerializeField] private SheetKind sheet = SheetKind.Resume;
    [SerializeField] private string emptyMessage = "<i>Nothing on file.</i>";
    [SerializeField] private Color suspiciousColor = new Color(1f, 0.25f, 0.2f, 0.5f);
    [SerializeField] private Color goodColor = new Color(0.2f, 0.85f, 0.3f, 0.5f);

    // Raised with the claim and its new mark.
    public event Action<ResumeClaim, ClaimMark> ClaimMarked;

    private static readonly Regex ClaimPattern = new Regex(@"\{(\w+)\}");

    private ClickableText clickable;
    private readonly Dictionary<string, ClaimMark> marks = new Dictionary<string, ClaimMark>();
    private ResumeData resume;

    public ResumeData Resume => resume;
    public SheetKind Sheet => sheet;
    public Color SuspiciousColor => suspiciousColor;
    public Color GoodColor => goodColor;

    private ClickableText Clickable
    {
        get
        {
            if (clickable == null)
            {
                clickable = GetComponent<ClickableText>();
                clickable.PhraseClicked += OnClaimClicked;
            }

            return clickable;
        }
    }

    public void Load(ResumeData data)
    {
        // Clear old marks before displaying the new applicant.
        resume = data;
        marks.Clear();

        string body = resume.GetBody(sheet);
        if (string.IsNullOrWhiteSpace(body))
        {
            Clickable.ShowPlain(emptyMessage);
            return;
        }

        foreach (Match match in ClaimPattern.Matches(body))
        {
            if (resume.GetClaim(match.Groups[1].Value) == null)
            {
                Debug.LogWarning(
                    $"Resume '{resume.name}' ({sheet}) references unknown claim '{match.Value}'.", resume);
            }
        }

        Clickable.Show(body, id => resume.GetClaim(id)?.text, DecorateClaim);
    }

    public ClaimMark GetMark(string claimId)
    {
        return marks.TryGetValue(claimId, out ClaimMark mark) ? mark : ClaimMark.None;
    }

    private void OnClaimClicked(string id)
    {
        // Clicking the same claim cycles through no mark, suspicious, and good.
        ClaimMark next = (ClaimMark)(((int)GetMark(id) + 1) % 3);
        marks[id] = next;

        Clickable.Refresh();
        ClaimMarked?.Invoke(resume.GetClaim(id), next);
    }

    private string DecorateClaim(string id, string display)
    {
        ClaimMark mark = GetMark(id);
        if (mark == ClaimMark.None)
        {
            return display;
        }

        Color color = mark == ClaimMark.Suspicious ? suspiciousColor : goodColor;
        return $"<mark=#{ColorUtility.ToHtmlStringRGBA(color)}>{display}</mark>";
    }
}
