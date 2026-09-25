using UnityEngine;
using UnityEngine.EventSystems;

// Attach to a sprite button with a BoxCollider2D under its window's title bar.
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class WindowMaximizeButton : MonoBehaviour,
    IPointerDownHandler, IPointerClickHandler, IDragHandler
{
    [SerializeField] private MaximizeWindow window;

    private void Awake()
    {
        if (window == null)
            window = GetComponentInParent<MaximizeWindow>();
        if (window == null)
            Debug.LogError("Assign this button's WindowMaximizer.", this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && window != null)
            window.BringToFront();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left
            && !eventData.dragging && window != null)
            window.ToggleMaximize();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Claim drags that start on this button so they don't move the title bar.
    }
}