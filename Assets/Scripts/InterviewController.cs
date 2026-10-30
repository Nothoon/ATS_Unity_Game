using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Builds interview questions from the claims the player marked on the resume.
// Each answer swaps the applicant's portrait to show their demeanor and is logged to notes.
// Phrases in an answer can be called out once per answer to catch contradictions.
public class InterviewController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button questionButtonTemplate;
    [SerializeField] private Transform questionList;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text questionsRemainingText;
    [SerializeField] private ClickableText answerText;
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private NotesLog notes;
    [SerializeField] private int maxQuestions = 3;

    [TextArea]
    [SerializeField] private string defaultFalseAccusationResponse =
        "...Yes? That's what I said. That's also what my resume says.";

    private readonly List<UnityEngine.UI.Button> questionButtons = new List<UnityEngine.UI.Button>();
    private ResumeData resume;
    private InterviewQuestion currentQuestion;
    private int questionsRemaining;

    private void Awake()
    {
        // The question template is copied when questions are created.
        questionButtonTemplate.gameObject.SetActive(false);
        answerText.PhraseClicked += CallOut;
    }

    public void Begin(ResumeInvestigator investigator)
    {
        // Start a fresh interview using the claims marked on the resume.
        resume = investigator.Resume;
        questionsRemaining = maxQuestions;
        currentQuestion = null;

        ClearQuestions();
        headerText.text = $"Interview with {resume.applicantName}";
        ShowPortrait(Demeanor.Neutral);
        UpdateQuestionsRemaining();

        foreach (ResumeClaim claim in resume.claims)
        {
            InterviewQuestion question = claim.GetQuestion(investigator.GetMark(claim.id));
            if (question != null)
            {
                AddQuestionButton(claim, question);
            }
        }

        answerText.ShowPlain(questionButtons.Count > 0
            ? $"{resume.applicantName} is waiting for your question."
            : "You didn't mark anything on the resume, so you have nothing to ask about.");
    }

    private void AddQuestionButton(ResumeClaim claim, InterviewQuestion question)
    {
        // Create one button for each question that can be asked.
        UnityEngine.UI.Button button = Instantiate(questionButtonTemplate, questionList);
        button.gameObject.SetActive(true);
        button.GetComponentInChildren<TMP_Text>().text = question.question;
        button.onClick.AddListener(() => Ask(claim, question, button));
        AudioManager.Register(button);
        questionButtons.Add(button);
    }

    private void Ask(ResumeClaim claim, InterviewQuestion question, UnityEngine.UI.Button button)
    {
        if (questionsRemaining <= 0)
        {
            return;
        }

        // A question can only be asked once.
        questionsRemaining--;
        button.interactable = false;
        currentQuestion = question;

        answerText.Show(
            $"<i>You:</i> {question.question}\n\n<b>{resume.applicantName}:</b> {question.answer}",
            id => question.GetPhrase(id)?.text);
        ShowPortrait(question.demeanor);
        notes.Add($"<b>Asked about \"{claim.text}\":</b>\n{StripPhrases(question)}");

        UpdateQuestionsRemaining();

        if (questionsRemaining == 0)
        {
            foreach (UnityEngine.UI.Button other in questionButtons)
            {
                other.interactable = false;
            }
        }
    }

    private void CallOut(string phraseId)
    {
        AnswerPhrase phrase = currentQuestion?.GetPhrase(phraseId);
        if (phrase == null)
        {
            return;
        }

        // One call-out per answer, right or wrong.
        currentQuestion = null;

        string response = string.IsNullOrWhiteSpace(phrase.callOutResponse)
            ? defaultFalseAccusationResponse
            : phrase.callOutResponse;
        Demeanor demeanor = string.IsNullOrWhiteSpace(phrase.callOutResponse)
            ? Demeanor.Neutral
            : phrase.callOutDemeanor;

        answerText.ShowPlain(
            $"<i>You:</i> Hold on. \"{phrase.text}\"? That doesn't add up.\n\n" +
            $"<b>{resume.applicantName}:</b> {response}");
        ShowPortrait(demeanor);
        notes.Add($"<b>Called out \"{phrase.text}\":</b>\n{response}");
    }

    private void ShowPortrait(Demeanor demeanor)
    {
        portrait.sprite = resume.GetPortrait(demeanor);
        portrait.color = resume.portraitTint;
    }

    private void UpdateQuestionsRemaining()
    {
        questionsRemainingText.text =
            $"Questions remaining: {questionsRemaining}\n" +
            "<size=80%><color=#3F342A>Something sound off? Click a phrase in their answer to call it out. One try per answer.</color></size>";
    }

    private static string StripPhrases(InterviewQuestion question)
    {
        string answer = question.answer;
        foreach (AnswerPhrase phrase in question.phrases)
        {
            answer = answer.Replace("{" + phrase.id + "}", phrase.text);
        }

        return answer;
    }

    private void ClearQuestions()
    {
        foreach (UnityEngine.UI.Button button in questionButtons)
        {
            Destroy(button.gameObject);
        }

        questionButtons.Clear();
    }
}
