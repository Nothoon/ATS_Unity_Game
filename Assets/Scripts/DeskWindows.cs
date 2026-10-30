using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DeskScreen
{
    Hidden,
    Resume,
    Interview,
    Decision
}

// The player's desk: the resume, hiring manual, reference letter, reference-call transcript and
// notes, each on its own movable notepad, plus the toolbar and countdown strip. The objects live
// in the scene under Canvas/DeskLayer; this component drives them.
// Red/green marks on any of them are written to the notes automatically.
public class DeskWindows : MonoBehaviour
{
    [SerializeField] private int maxReferenceChecks = 2;

    [Header("Desk objects")]
    [SerializeField] private RectTransform layer;
    [SerializeField] private GameObject toolbar;
    [Tooltip("Toolbar buttons that are disabled once time is up (everything except Notes).")]
    [SerializeField] private List<GameObject> resourceButtons = new List<GameObject>();
    [SerializeField] private GameObject timerStrip;
    [SerializeField] private TMP_Text timerText;

    [Header("Boxes")]
    [SerializeField] private DraggableWindow resumeWindow;
    [SerializeField] private DraggableWindow manualWindow;
    [SerializeField] private DraggableWindow letterWindow;
    [SerializeField] private DraggableWindow callWindow;
    [SerializeField] private DraggableWindow notesWindow;

    [Header("Content")]
    [SerializeField] private ResumeInvestigator resumeSheet;
    [SerializeField] private ResumeInvestigator letterSheet;
    [SerializeField] private ResumeInvestigator callSheet;
    [SerializeField] private ClickableText manualSheet;
    [SerializeField] private NotesLog notes;

    [Header("Reference call")]
    [SerializeField] private TMP_Text callStatus;
    [SerializeField] private Button callButton;
    [SerializeField] private TMP_Text callToolbarLabel;

    private static readonly Regex RichTextTag = new Regex("<[^>]+>");

    private readonly Dictionary<string, ClaimMark> manualMarks = new Dictionary<string, ClaimMark>();
    private List<string> manualRules = new List<string>();

    private ResumeData applicant;
    private int checksUsed;
    private bool calledThisApplicant;
    private int lastTickSecond = -1;

    private int ChecksLeft => maxReferenceChecks - checksUsed;

    // Runtime hookups (events aren't saved in a scene) and the starting state.
    public void Wire()
    {
        resumeSheet.ClaimMarked += (claim, mark) => OnClaimMarked(resumeSheet, "Resume", claim, mark);
        letterSheet.ClaimMarked += (claim, mark) => OnClaimMarked(letterSheet, "Reference letter", claim, mark);
        callSheet.ClaimMarked += (claim, mark) => OnClaimMarked(callSheet, "Reference call", claim, mark);
        manualSheet.PhraseClicked += OnRuleClicked;
        callButton.onClick.AddListener(PlaceCall);

        timerStrip.SetActive(false);
        SetScreen(DeskScreen.Hidden);
    }

    // Loads a new applicant onto the desk. Clears the notes; boxes keep their positions.
    public void BeginApplicant(ResumeData data)
    {
        applicant = data;
        calledThisApplicant = false;
        notes.Clear();

        resumeSheet.Load(data);
        letterSheet.Load(data);
        callSheet.Load(data);

        manualWindow.Close();
        letterWindow.Close();
        callWindow.Close();
        RefreshCallWindow();
    }

    // body contains {rule_0}, {rule_1}... placeholders; rules[i] is the text of {rule_i}.
    public void SetManual(string body, List<string> rules)
    {
        manualRules = rules;
        manualMarks.Clear();
        manualSheet.Show(body, id => RuleText(id), DecorateRule);
    }

    // When locked, the resume, manual and reference boxes close and only the notes stay available.
    public void SetResourcesLocked(bool locked)
    {
        foreach (GameObject button in resourceButtons)
        {
            button.SetActive(!locked);
        }

        if (locked)
        {
            resumeWindow.Close();
            manualWindow.Close();
            letterWindow.Close();
            callWindow.Close();
        }
    }

    // Shows a countdown strip. At 0 or below it reads "Time's up!". Hidden when not visible.
    public void SetTimer(float secondsLeft, bool visible, string label = "Time left", int urgentBelow = 15)
    {
        timerStrip.SetActive(visible);
        if (!visible)
        {
            lastTickSecond = -1;
            return;
        }

        if (secondsLeft <= 0f)
        {
            timerText.text = "Time's up!";
            timerText.color = OfficeTheme.RedFlag;
            lastTickSecond = 0;
            return;
        }

        int whole = Mathf.CeilToInt(secondsLeft);
        timerText.text = $"{label}  {whole / 60}:{whole % 60:00}";
        timerText.color = whole <= urgentBelow ? OfficeTheme.RedFlag : OfficeTheme.Ink;

        // Tick once per second while the timer is in its red zone.
        if (whole <= urgentBelow && whole != lastTickSecond)
        {
            AudioManager.Play(Sfx.TimerTick);
        }

        lastTickSecond = whole;
    }

    public void SetScreen(DeskScreen screen)
    {
        bool visible = screen != DeskScreen.Hidden;
        layer.gameObject.SetActive(visible);
        toolbar.SetActive(visible);

        if (!visible)
        {
            return;
        }

        notesWindow.Open();
        if (screen == DeskScreen.Resume)
        {
            resumeWindow.Open();
        }
        else if (screen == DeskScreen.Interview)
        {
            // The interview screen needs the left side of the desk; the resume is one click away.
            resumeWindow.Close();
        }
    }

    private void PlaceCall()
    {
        if (applicant == null || calledThisApplicant || ChecksLeft <= 0
            || string.IsNullOrWhiteSpace(applicant.referenceCheckTranscript))
        {
            return;
        }

        checksUsed++;
        calledThisApplicant = true;
        AudioManager.Play(Sfx.Phone);
        RefreshCallWindow();
    }

    private void RefreshCallWindow()
    {
        bool hasNumber = applicant != null && !string.IsNullOrWhiteSpace(applicant.referenceCheckTranscript);
        string left = $"Reference checks left: <b>{ChecksLeft}</b> of {maxReferenceChecks} this campaign.";

        if (calledThisApplicant)
        {
            callStatus.text = $"You called the reference.\n{left}";
            callButton.gameObject.SetActive(false);
        }
        else if (!hasNumber)
        {
            callStatus.text = $"There's no contact number on this application, so there's nobody to call.\n{left}";
            callButton.gameObject.SetActive(false);
        }
        else if (ChecksLeft <= 0)
        {
            callStatus.text = $"You've used every reference check.\n{left}";
            callButton.gameObject.SetActive(false);
        }
        else
        {
            callStatus.text = $"Calling uses one check and can't be undone.\n{left}";
            callButton.gameObject.SetActive(true);
        }

        callSheet.gameObject.SetActive(calledThisApplicant);
        callToolbarLabel.text = $"Check References ({ChecksLeft})";
    }

    private void OnClaimMarked(ResumeInvestigator sheet, string source, ResumeClaim claim, ClaimMark mark)
    {
        RecordMark($"{sheet.Sheet}:{claim.id}", source, claim.text, mark);
    }

    private void OnRuleClicked(string id)
    {
        ClaimMark current = manualMarks.TryGetValue(id, out ClaimMark mark) ? mark : ClaimMark.None;
        ClaimMark next = (ClaimMark)(((int)current + 1) % 3);
        manualMarks[id] = next;
        manualSheet.Refresh();

        RecordMark($"Manual:{id}", "Manual rule", RuleText(id), next);
    }

    private string RuleText(string id)
    {
        if (id.StartsWith("rule_") && int.TryParse(id.Substring(5), out int index)
            && index >= 0 && index < manualRules.Count)
        {
            return manualRules[index];
        }

        return null;
    }

    private string DecorateRule(string id, string display)
    {
        if (!manualMarks.TryGetValue(id, out ClaimMark mark) || mark == ClaimMark.None)
        {
            return display;
        }

        Color color = mark == ClaimMark.Suspicious ? resumeSheet.SuspiciousColor : resumeSheet.GoodColor;
        return $"<mark=#{ColorUtility.ToHtmlStringRGBA(color)}>{display}</mark>";
    }

    // Red = a problem, green = good. Taking the mark off removes the note.
    private void RecordMark(string key, string source, string text, ClaimMark mark)
    {
        string plain = RichTextTag.Replace(text ?? "", "").Trim();
        switch (mark)
        {
            case ClaimMark.Suspicious:
                notes.Set(key, $"{Tag("RED FLAG", OfficeTheme.RedFlag)} {source}: \"{plain}\"");
                break;
            case ClaimMark.Good:
                notes.Set(key, $"{Tag("GOOD", OfficeTheme.Good)} {source}: \"{plain}\"");
                break;
            default:
                notes.Remove(key);
                break;
        }
    }

    private static string Tag(string label, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}><b>{label}</b></color>";
    }
}
