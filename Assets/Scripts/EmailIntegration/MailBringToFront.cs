using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Clicks inside the mail contents focus the existing draggable window.</summary>
public sealed class MailBringToFront : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private DraggableWindow window;
    public void SetWindow(DraggableWindow value) { window = value; }
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && window != null)
            window.BringToFront();
    }
}
