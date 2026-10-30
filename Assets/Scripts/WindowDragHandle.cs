using UnityEngine;
using UnityEngine.EventSystems;

// Lives on a window's title bar and forwards drags to the window.
public class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public DraggableWindow window;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Tell the window that dragging has started.
        window.BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        window.Drag(eventData);
    }
}
