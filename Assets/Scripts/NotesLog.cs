using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The player's notepad. Other systems add entries; it renders them into a TMP text.
// Keyed entries (Set/Remove) can be updated or taken back, e.g. when a highlight is changed.
[RequireComponent(typeof(TMP_Text))]
public class NotesLog : MonoBehaviour
{
    [SerializeField] private string header = "<b>Notes</b>";
    [SerializeField] private string emptyMessage = "No evidence recorded yet.";

    private readonly List<string> keys = new List<string>();
    private readonly List<string> entries = new List<string>();
    private TMP_Text text;

    private TMP_Text Text => text != null ? text : text = GetComponent<TMP_Text>();

    private void Awake()
    {
        // Show the empty-notes message when the scene starts.
        Refresh();
    }

    public void Add(string entry)
    {
        keys.Add(null);
        entries.Add(entry);
        Refresh();
    }

    // Adds the entry, or replaces the one already stored under this key.
    public void Set(string key, string entry)
    {
        int index = keys.IndexOf(key);
        if (index >= 0)
        {
            entries[index] = entry;
        }
        else
        {
            keys.Add(key);
            entries.Add(entry);
        }

        Refresh();
    }

    public void Remove(string key)
    {
        int index = keys.IndexOf(key);
        if (index < 0)
        {
            return;
        }

        keys.RemoveAt(index);
        entries.RemoveAt(index);
        Refresh();
    }

    public void Clear()
    {
        keys.Clear();
        entries.Clear();
        Refresh();
    }

    private void Refresh()
    {
        // Rebuild the visible notes text after a note is added or removed.
        string body = entries.Count == 0 ? emptyMessage : string.Join("\n\n", entries);
        Text.text = header + "\n\n" + body;
    }
}
