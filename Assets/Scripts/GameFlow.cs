using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the campaign: day intro memo -> applicants (resume, interview, decision)
// -> end-of-day HR memo -> ... -> ending. Hiring someone fills the day's position.
public class GameFlow : MonoBehaviour
{
    [SerializeField] private CampaignData campaign;

    [Header("Screens")]
    [SerializeField] private TitleScreen titleScreen;
    [SerializeField] private GameObject memoPanel;
    [SerializeField] private GameObject resumePanel;
    [SerializeField] private GameObject interviewPanel;
    [SerializeField] private GameObject decisionPanel;
    [SerializeField] private GameObject sidebar;

    [Header("Timer")]
    [Tooltip("Time to look at each applicant (resume + interview). At 0 the player must decide.")]
    [SerializeField] private float secondsPerApplicant = 60f;
    [Tooltip("Time to hire or reject once on the decision screen; if it runs out the applicant is rejected. When the applicant timer runs out, the resume, manual and references are also locked.")]
    [SerializeField] private float secondsToDecide = 10f;

    [Header("Memo")]
    [SerializeField] private TMP_Text memoTitle;
    [SerializeField] private TMP_Text memoBody;
    [SerializeField] private UnityEngine.UI.Button memoContinueButton;

    [Header("Sidebar")]
    [SerializeField] private TMP_Text dayStatusText;
    [SerializeField] private NotesLog notes;

    [Header("Desk")]
    [SerializeField] private DeskWindows desk;

    [Header("Resume")]
    [SerializeField] private ResumeInvestigator resumeInvestigator;
    [SerializeField] private UnityEngine.UI.Button startInterviewButton;

    [Header("Interview")]
    [SerializeField] private InterviewController interview;
    [SerializeField] private UnityEngine.UI.Button makeDecisionButton;

    [Header("Decision")]
    [SerializeField] private TMP_Text decisionTitle;
    [SerializeField] private TMP_Text decisionResultText;
    [SerializeField] private UnityEngine.UI.Button hireButton;
    [SerializeField] private UnityEngine.UI.Button rejectButton;
    [SerializeField] private UnityEngine.UI.Button decisionContinueButton;

    private readonly List<string> hireHistory = new List<string>();
    private readonly List<string> rejectedMemosToday = new List<string>();
    private Action onMemoContinue;
    private Action onDecisionContinue;
    private int dayIndex;
    private int applicantIndex;
    private int score;
    private float timeLeft;
    private bool timerRunning;
    private float decisionTimeLeft;
    private bool decisionTimerRunning;
    private ResumeData hiredToday;

    private DayData Day => campaign.days[dayIndex];
    private ResumeData Applicant => Day.applicants[applicantIndex];

    private void Awake()
    {
        // Connect the scene buttons to the actions they should perform.
        memoContinueButton.onClick.AddListener(() => onMemoContinue?.Invoke());
        startInterviewButton.onClick.AddListener(StartInterview);
        makeDecisionButton.onClick.AddListener(ShowDecision);
        hireButton.onClick.AddListener(Hire);
        rejectButton.onClick.AddListener(Reject);
        decisionContinueButton.onClick.AddListener(() => onDecisionContinue?.Invoke());

        desk.Wire();
    }

    private void Start()
    {
        // The title screen is optional: without one in the scene the game starts on day 1.
        if (titleScreen != null)
        {
            titleScreen.Show(ShowDayIntro);
        }
        else
        {
            ShowDayIntro();
        }
    }

    private void ShowDayIntro()
    {
        // Reset information that only belongs to the previous day.
        hiredToday = null;
        rejectedMemosToday.Clear();

        var body = new StringBuilder();
        body.Append($"<b>Today we are hiring:</b> {Day.positionTitle}\n\n");
        body.Append(Day.bossMemo);
        if (Day.newRules.Count > 0)
        {
            body.Append("\n\n<b>New in the hiring manual:</b>");
            foreach (string rule in Day.newRules)
            {
                body.Append($"\n• {rule}");
            }
        }

        ShowMemo($"Day {dayIndex + 1}", body.ToString(), "Start Day", () => ShowApplicant(0));
    }

    private void Update()
    {
        // Unity calls Update every frame, so it is used to count down the timers.
        if (decisionTimerRunning)
        {
            decisionTimeLeft -= Time.deltaTime;
            if (decisionTimeLeft <= 0f)
            {
                TooSlowToDecide();
            }
            else
            {
                desk.SetTimer(decisionTimeLeft, true, "Decide now", 5);
            }

            return;
        }

        if (!timerRunning)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            TimeUp();
            return;
        }

        desk.SetTimer(timeLeft, true);
    }

    // Out of time: resources are locked (notes only) and the player has a short window to decide.
    private void TimeUp()
    {
        desk.SetResourcesLocked(true);
        ShowDecision();
        decisionResultText.text =
            $"<b>Time's up!</b> The resume, manual and references are locked. " +
            $"You have {Mathf.CeilToInt(secondsToDecide)} seconds to decide, or they walk out.";
    }

    // Nobody decided in time, so the applicant leaves and counts as rejected.
    private void TooSlowToDecide()
    {
        Reject();
        decisionResultText.text = "<b>Out of time.</b> " + decisionResultText.text;
    }

    private void ShowApplicant(int index)
    {
        // Load the selected applicant and return to the resume screen.
        applicantIndex = index;
        timeLeft = secondsPerApplicant;
        decisionTimerRunning = false;
        desk.SetResourcesLocked(false);
        desk.BeginApplicant(Applicant);
        BuildManual(out string manualBody, out List<string> manualRules);
        desk.SetManual(manualBody, manualRules);
        UpdateDayStatus();
        ShowScreen(resumePanel);
    }

    private void StartInterview()
    {
        // Show the interview screen using the marks made on the resume.
        ShowScreen(interviewPanel);
        interview.Begin(resumeInvestigator);
    }

    private void ShowDecision()
    {
        decisionTitle.text = $"Hire {Applicant.applicantName} as {Day.positionTitle}?";
        decisionResultText.text = "";
        hireButton.interactable = true;
        rejectButton.interactable = true;
        decisionContinueButton.gameObject.SetActive(false);

        // Whether the player came here early or ran out of time, they have a short window to decide.
        decisionTimeLeft = secondsToDecide;
        decisionTimerRunning = true;
        ShowScreen(decisionPanel);
    }

    private void Hire()
    {
        // Hiring ends the day immediately because the position is now filled.
        AudioManager.Play(Sfx.Accept);
        hiredToday = Applicant;
        bool othersWaiting = applicantIndex < Day.applicants.Count - 1;
        ShowDecisionResult(
            $"You hired {Applicant.applicantName}." +
            (othersWaiting ? "\nThe position is filled. Everyone else in the lobby has been sent home." : ""),
            EndDay);
    }

    private void Reject()
    {
        // Rejection moves to the next applicant, unless this was the last one.
        AudioManager.Play(Sfx.Reject);
        if (!string.IsNullOrWhiteSpace(Applicant.rejectedMemo))
        {
            rejectedMemosToday.Add(Applicant.rejectedMemo);
        }

        bool lastApplicant = applicantIndex >= Day.applicants.Count - 1;
        ShowDecisionResult(
            $"You rejected {Applicant.applicantName}." +
            (lastApplicant ? "\nThat was the last applicant today." : ""),
            lastApplicant ? (Action)EndDay : () => ShowApplicant(applicantIndex + 1));
    }

    private void ShowDecisionResult(string result, Action next)
    {
        decisionTimerRunning = false;
        decisionResultText.text = result;
        hireButton.interactable = false;
        rejectButton.interactable = false;
        decisionContinueButton.gameObject.SetActive(true);
        onDecisionContinue = next;
    }

    private void EndDay()
    {
        // Add today's result to the score and prepare the HR memo.
        var body = new StringBuilder();
        if (hiredToday != null)
        {
            score += hiredToday.hirePerformance;
            hireHistory.Add($"Day {dayIndex + 1}: {hiredToday.applicantName}, {Day.positionTitle}");
            body.Append($"<b>RE: Your new {Day.positionTitle}, {hiredToday.applicantName}</b>\n\n");
            body.Append(hiredToday.hiredMemo);
        }
        else
        {
            score += campaign.emptyPositionPerformance;
            hireHistory.Add($"Day {dayIndex + 1}: nobody ({Day.positionTitle} left empty)");
            body.Append($"<b>RE: The empty {Day.positionTitle} position</b>\n\n");
            body.Append(campaign.emptyPositionMemo);
        }

        foreach (string memo in rejectedMemosToday)
        {
            body.Append($"\n\n<b>Also:</b> {memo}");
        }

        bool lastDay = dayIndex >= campaign.days.Count - 1;
        ShowMemo($"End of Day {dayIndex + 1}: Memo from HR", body.ToString(),
            lastDay ? "See Results" : "Next Day",
            lastDay ? (Action)ShowEnding : () => { dayIndex++; ShowDayIntro(); });
    }

    private void ShowEnding()
    {
        Ending ending = campaign.GetEnding(score);

        var body = new StringBuilder();
        body.Append(ending != null ? ending.text : "");
        body.Append("\n\n<b>Your hires:</b>");
        foreach (string hire in hireHistory)
        {
            body.Append($"\n• {hire}");
        }
        body.Append($"\n\n<b>Company score:</b> {score}");

        ShowMemo(ending != null ? ending.title : "The End", body.ToString(), "Play Again",
            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
    }

    private void ShowMemo(string title, string body, string buttonLabel, Action next)
    {
        memoTitle.text = title;
        memoBody.text = body;
        memoContinueButton.GetComponentInChildren<TMP_Text>().text = buttonLabel;
        onMemoContinue = next;

        memoPanel.SetActive(true);
        sidebar.SetActive(false);
        resumePanel.SetActive(false);
        interviewPanel.SetActive(false);
        decisionPanel.SetActive(false);
        desk.SetScreen(DeskScreen.Hidden);
        timerRunning = false;
        decisionTimerRunning = false;
    }

    private void ShowScreen(GameObject screen)
    {
        // Turn on only the screen needed for the current part of the game.
        memoPanel.SetActive(false);
        sidebar.SetActive(true);
        resumePanel.SetActive(screen == resumePanel);
        interviewPanel.SetActive(screen == interviewPanel);
        decisionPanel.SetActive(screen == decisionPanel);
        DeskScreen deskScreen =
            screen == resumePanel ? DeskScreen.Resume :
            screen == interviewPanel ? DeskScreen.Interview :
            DeskScreen.Decision;
        desk.SetScreen(deskScreen);
        UpdateDayStatus(deskScreen);

        // The applicant clock runs on the resume and interview screens; the decision screen has its own countdown.
        bool deciding = deskScreen == DeskScreen.Decision;
        timerRunning = !deciding;
        if (deciding)
        {
            desk.SetTimer(decisionTimeLeft, true, "Decide now", 5);
        }
        else
        {
            desk.SetTimer(timeLeft, true);
        }
    }

    // Shows where the player is in the applicant flow, e.g. "Review > Interview > Decide" with the current step bold.
    private static string StepLine(DeskScreen screen)
    {
        string[] steps = { "Review", "Interview", "Decide" };
        int current = screen == DeskScreen.Interview ? 1 : screen == DeskScreen.Decision ? 2 : 0;
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i] = i == current ? $"<b><u>{steps[i]}</u></b>" : steps[i];
        }

        return string.Join("  >  ", steps);
    }

    private void UpdateDayStatus(DeskScreen screen = DeskScreen.Resume)
    {
        dayStatusText.text =
            $"<b>Day {dayIndex + 1}</b>  ·  {Day.positionTitle}\n" +
            $"<size=80%>Applicant {applicantIndex + 1} of {Day.applicants.Count}</size>\n" +
            $"<size=80%>{StepLine(screen)}</size>";
    }

    // Each rule is a clickable {rule_N} placeholder in the body so the player can mark it.
    private void BuildManual(out string body, out List<string> rules)
    {
        rules = new List<string>();
        var manual = new StringBuilder();
        manual.Append($"<size=130%><b>{campaign.companyName.ToUpper()} HIRING MANUAL</b></size>\n\n");
        manual.Append($"<b>Today's position:</b> {Day.positionTitle}\n{Day.jobDescription}\n\n");
        manual.Append("<b>RULES</b>");

        int ruleNumber = 1;
        for (int i = 0; i <= dayIndex; i++)
        {
            foreach (string rule in campaign.days[i].newRules)
            {
                string tag = i == dayIndex && dayIndex > 0 ? " <color=#C0392B><b>NEW</b></color>" : "";
                manual.Append($"\n{ruleNumber}. {{rule_{rules.Count}}}");
                rules.Add(rule + tag);
                ruleNumber++;
            }
        }

        body = manual.ToString();
    }
}
