using System;
using UnityEngine;
using UnityEngine.UI;

// The title screen: Play starts the game, How to Play and Settings flip to their own cards.
// The panel and its cards live in the scene; this just switches between them.
public class TitleScreen : MonoBehaviour
{
    [Header("Cards")]
    [SerializeField] private GameObject titleCard;
    [SerializeField] private GameObject howToPlayCard;
    [SerializeField] private GameObject settingsCard;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button howToPlayBackButton;
    [SerializeField] private Button settingsBackButton;

    private Action onPlay;

    private void Awake()
    {
        // Connect each title-screen button to its matching action.
        playButton.onClick.AddListener(Play);
        howToPlayButton.onClick.AddListener(() => ShowCard(howToPlayCard));
        settingsButton.onClick.AddListener(() => ShowCard(settingsCard));
        howToPlayBackButton.onClick.AddListener(() => ShowCard(titleCard));
        settingsBackButton.onClick.AddListener(() => ShowCard(titleCard));
    }

    // Shows the title screen; onPlay runs when the player presses Play.
    public void Show(Action play)
    {
        onPlay = play;
        gameObject.SetActive(true);
        ShowCard(titleCard);
    }

    private void Play()
    {
        gameObject.SetActive(false);
        onPlay?.Invoke();
    }

    private void ShowCard(GameObject card)
    {
        // Only one title-screen card should be visible at a time.
        titleCard.SetActive(card == titleCard);
        howToPlayCard.SetActive(card == howToPlayCard);
        settingsCard.SetActive(card == settingsCard);
    }
}
