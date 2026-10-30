using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A movable box: drag the title bar to move it, click anywhere on it to bring it to the front,
// and the close button hides it.
public class DraggableWindow : MonoBehaviour, IPointerDownHandler
{
    private const float VisibleMargin = 120f;

    [SerializeField] private Button closeButton;

    public bool IsOpen => gameObject.activeSelf;

    private RectTransform Rect => (RectTransform)transform;
    private RectTransform parentRect;
    private Vector2 dragOffset;

    private void Awake()
    {
        // Remember the desk area so the window can stay inside it while dragging.
        parentRect = transform.parent as RectTransform;
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                AudioManager.Play(Sfx.Paper);
                Close();
            });
        }
    }

    public void Open()
    {
        // Put this window in front of the other desk windows.
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    // Used by the toolbar buttons, i.e. only when the player opens or closes a box.
    public void Toggle()
    {
        AudioManager.Play(Sfx.Paper);
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Clicking a window brings it to the front.
        transform.SetAsLastSibling();
    }

    // Called by the title bar's WindowDragHandle.
    internal void BeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        if (ScreenToParent(eventData, out Vector2 local))
        {
            dragOffset = Rect.anchoredPosition - local;
        }
    }

    internal void Drag(PointerEventData eventData)
    {
        // Update the position while the player is dragging the title bar.
        if (ScreenToParent(eventData, out Vector2 local))
        {
            Rect.anchoredPosition = ClampToParent(local + dragOffset);
        }
    }

    private bool ScreenToParent(PointerEventData eventData, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out local);
    }

    // Keeps the title bar on screen so a box can never be lost off the edge.
    private Vector2 ClampToParent(Vector2 position)
    {
        Vector2 half = parentRect.rect.size * 0.5f;
        Vector2 size = Rect.rect.size;

        position.x = Mathf.Clamp(
            position.x, -half.x - size.x * 0.5f + VisibleMargin, half.x + size.x * 0.5f - VisibleMargin);
        position.y = Mathf.Clamp(
            position.y, -half.y + VisibleMargin - size.y * 0.5f, half.y - size.y * 0.5f);
        return position;
    }
}
