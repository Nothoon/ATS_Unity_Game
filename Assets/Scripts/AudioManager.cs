using UnityEngine;
using UnityEngine.UI;

public enum Sfx
{
    Paper,
    Click,
    TimerTick,
    Accept,
    Reject,
    Phone
}

// Plays the game's sound effects. The clips are assigned in the Inspector; a missing clip is
// simply silent. Every button in the scene plays the click sound unless something more
// specific (paper, accept, reject, phone) played on the same frame.
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip paper;
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip timerTick;
    [SerializeField] private AudioClip accept;
    [SerializeField] private AudioClip reject;
    [SerializeField] private AudioClip phone;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private static AudioManager instance;

    private AudioSource source;
    private bool clickPending;
    private bool specificSoundThisFrame;

    private void Awake()
    {
        // Save this object so other scripts can play sounds without a reference.
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void Start()
    {
        // Add the normal click sound to every button already in the scene.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            Register(button);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Buttons created while the game runs (e.g. interview questions) register themselves here.
    public static void Register(Button button)
    {
        button.onClick.AddListener(() =>
        {
            if (instance != null)
            {
                instance.clickPending = true;
            }
        });
    }

    public static void Play(Sfx sfx)
    {
        if (instance != null)
        {
            instance.PlayClip(sfx);
        }
    }

    // Runs after every button handler this frame, so a specific sound can replace the click.
    private void LateUpdate()
    {
        if (clickPending && !specificSoundThisFrame)
        {
            PlayClip(Sfx.Click);
        }

        clickPending = false;
        specificSoundThisFrame = false;
    }

    private void PlayClip(Sfx sfx)
    {
        AudioClip clip = ClipFor(sfx);
        if (clip == null)
        {
            return;
        }

        if (sfx != Sfx.Click && sfx != Sfx.TimerTick)
        {
            specificSoundThisFrame = true;
        }

        source.PlayOneShot(clip, volume);
    }

    private AudioClip ClipFor(Sfx sfx)
    {
        // Match the requested sound type to the clip assigned in the Inspector.
        switch (sfx)
        {
            case Sfx.Paper: return paper;
            case Sfx.Click: return click;
            case Sfx.TimerTick: return timerTick;
            case Sfx.Accept: return accept;
            case Sfx.Reject: return reject;
            default: return phone;
        }
    }
}
