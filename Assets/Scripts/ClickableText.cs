using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Renders text containing {phraseId} placeholders as clickable TMP links and
// underlines the phrase under the mouse. Used by the resume and interview answers.
[RequireComponent(typeof(TMP_Text))]
public class ClickableText : MonoBehaviour,
    IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor = new Color(0.55f, 0.75f, 1f);

    public event Action<string> PhraseClicked;

    private static readonly Regex PhrasePattern = new Regex(@"\{(\w+)\}");

    private TMP_Text text;
    private Canvas canvas;
    private string body = "";
    private Func<string, string> getPhraseText;
    private Func<string, string, string> decoratePhrase;
    private string hoveredId;
    private bool interactable;

    private TMP_Text Text => text != null ? text : text = GetComponent<TMP_Text>();

    // getText returns the visible text for a phrase id (null if unknown).
    // decorate optionally wraps a phrase's visible text, e.g. with a highlight.
    public void Show(string newBody, Func<string, string> getText, Func<string, string, string> decorate = null)
    {
        body = newBody ?? "";
        getPhraseText = getText;
        decoratePhrase = decorate;
        hoveredId = null;
        interactable = getText != null;
        Text.raycastTarget = true;
        Refresh();
    }

    public void ShowPlain(string newBody)
    {
        Show(newBody, null);
    }

    public void Refresh()
    {
        // Replace placeholders such as {claim_1} with clickable TMP links.
        Text.text = getPhraseText == null
            ? body
            : PhrasePattern.Replace(body, FormatPhrase);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Tell the script using this text which phrase was clicked.
        string id = GetPhraseIdAt(eventData.position);
        if (id != null)
        {
            PhraseClicked?.Invoke(id);
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        SetHovered(GetPhraseIdAt(eventData.position));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(null);
    }

    private void SetHovered(string id)
    {
        if (id == hoveredId)
        {
            return;
        }

        hoveredId = id;
        Refresh();
    }

    private string GetPhraseIdAt(Vector2 screenPosition)
    {
        if (!interactable)
        {
            return null;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        Camera eventCamera =
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        // TMP checks which link is underneath the mouse position.
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(Text, screenPosition, eventCamera);
        return linkIndex < 0 ? null : Text.textInfo.linkInfo[linkIndex].GetLinkID();
    }

    private string FormatPhrase(Match match)
    {
        string id = match.Groups[1].Value;
        string display = getPhraseText(id);
        if (display == null)
        {
            return match.Value;
        }

        if (id == hoveredId)
        {
            display = $"<u><color=#{ColorUtility.ToHtmlStringRGB(hoverColor)}>{display}</color></u>";
        }

        if (decoratePhrase != null)
        {
            display = decoratePhrase(id, display);
        }

        return $"<link=\"{id}\">{display}</link>";
    }
}
