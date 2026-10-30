using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClaimCategory
{
    General,
    EmploymentDates,
    JobTitle,
    Certification,
    TechnicalSkill,
    Reference
}

// The documents the player can read and mark. Each has its own body and claims on ResumeData.
public enum SheetKind
{
    Resume,
    ReferenceLetter,
    ReferenceCheck
}

public enum Demeanor
{
    Neutral,
    Confident,
    Nervous,
    Scared,
    Defensive,
    Evasive
}

// A clickable phrase inside an interview answer that the player can call out.
[Serializable]
public class AnswerPhrase
{
    [Tooltip("Placed in the answer as {id}. Letters, numbers and underscores only.")]
    public string id;
    public string text;
    [Tooltip("True if this phrase contradicts the resume or the manual.")]
    public bool isContradiction;
    [Tooltip("What the applicant says when called out on this phrase.")]
    [TextArea] public string callOutResponse;
    public Demeanor callOutDemeanor;
}

[Serializable]
public class InterviewQuestion
{
    [TextArea] public string question;
    [Tooltip("Insert clickable phrases with {phraseId}.")]
    [TextArea] public string answer;
    public Demeanor demeanor;
    public List<AnswerPhrase> phrases = new List<AnswerPhrase>();

    public AnswerPhrase GetPhrase(string id)
    {
        // Find the interview phrase with this id.
        return phrases.Find(p => p.id == id);
    }
}

[Serializable]
public class DemeanorPortrait
{
    public Demeanor demeanor;
    public Sprite sprite;
}

[Serializable]
public class ResumeClaim
{
    [Tooltip("Placed in the resume body as {id}. Letters, numbers and underscores only.")]
    public string id;
    public string text;
    public bool isSuspicious;
    public ClaimCategory category;
    [TextArea] public string designerNote;

    [Tooltip("Asked when the player marks this claim Suspicious. Leave the question empty for none.")]
    public InterviewQuestion suspiciousQuestion;

    [Tooltip("Asked when the player marks this claim Good. Leave the question empty for none.")]
    public InterviewQuestion goodQuestion;

    public InterviewQuestion GetQuestion(ClaimMark mark)
    {
        // Use a different question depending on whether the claim was good or suspicious.
        InterviewQuestion question =
            mark == ClaimMark.Suspicious ? suspiciousQuestion :
            mark == ClaimMark.Good ? goodQuestion :
            null;

        return question != null && !string.IsNullOrWhiteSpace(question.question) ? question : null;
    }
}

[CreateAssetMenu(fileName = "NewResume", menuName = "ATS/Resume")]
public class ResumeData : ScriptableObject
{
    public string applicantName;

    [Tooltip("Resume text. Insert clickable claims with {claimId}. TMP rich text is allowed.")]
    [TextArea(10, 30)]
    public string body;

    public List<ResumeClaim> claims = new List<ResumeClaim>();

    [Header("Reference")]
    [Tooltip("Letter from a previous employer, shown in its own box. Insert claims with {claimId}.")]
    [TextArea(6, 20)] public string referenceLetter;
    [Tooltip("Claim ids must be unique across the resume and both reference sheets.")]
    public List<ResumeClaim> referenceLetterClaims = new List<ResumeClaim>();

    [Tooltip("What the reference says when the player calls them (limited uses). Empty = no number on file.")]
    [TextArea(6, 20)] public string referenceCheckTranscript;
    public List<ResumeClaim> referenceCheckClaims = new List<ResumeClaim>();

    [Header("Interview")]
    public Sprite neutralPortrait;
    public List<DemeanorPortrait> portraits = new List<DemeanorPortrait>();
    public Color portraitTint = Color.white;

    [Header("Consequences")]
    [Tooltip("HR memo shown at the end of the day if this applicant is hired.")]
    [TextArea(3, 8)] public string hiredMemo;
    [Tooltip("Added to the company score if hired. Negative is bad.")]
    [Range(-3, 3)] public int hirePerformance;
    [Tooltip("Optional memo shown at the end of the day if this applicant is rejected.")]
    [TextArea(2, 6)] public string rejectedMemo;

    // Looks in the resume and both reference sheets.
    public ResumeClaim GetClaim(string id)
    {
        return claims.Find(c => c.id == id)
            ?? referenceLetterClaims.Find(c => c.id == id)
            ?? referenceCheckClaims.Find(c => c.id == id);
    }

    public string GetBody(SheetKind sheet)
    {
        // Return the text for the document currently being displayed.
        switch (sheet)
        {
            case SheetKind.ReferenceLetter: return referenceLetter;
            case SheetKind.ReferenceCheck: return referenceCheckTranscript;
            default: return body;
        }
    }

    public List<ResumeClaim> GetClaims(SheetKind sheet)
    {
        // Return the claims that belong to the selected document.
        switch (sheet)
        {
            case SheetKind.ReferenceLetter: return referenceLetterClaims;
            case SheetKind.ReferenceCheck: return referenceCheckClaims;
            default: return claims;
        }
    }

    public Sprite GetPortrait(Demeanor demeanor)
    {
        // Use the neutral portrait if a specific expression was not assigned.
        DemeanorPortrait match = portraits.Find(p => p.demeanor == demeanor);
        return match != null && match.sprite != null ? match.sprite : neutralPortrait;
    }
}
