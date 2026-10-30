using UnityEngine;
using UnityEngine.UI;

// Lives on a toolbar button: clicking it opens or closes its window.
[RequireComponent(typeof(Button))]
public class WindowToggleButton : MonoBehaviour
{
    public DraggableWindow window;

    private void Awake()
    {
        // Let the toolbar button control its assigned window.
        GetComponent<Button>().onClick.AddListener(() => window.Toggle());
    }
}
