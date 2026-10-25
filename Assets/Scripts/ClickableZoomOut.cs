using UnityEngine;
using UnityEngine.InputSystem;

public class ClickableZoomOut : MonoBehaviour
{
    [SerializeField] private CameraZoom cameraZoom;

    private void OnMouseDown()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        // Only ignore the click if the player is actively clicking on the dialogue panel or choices
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPointerOverDialogueUI(mousePos))
        {
            return;
        }

        cameraZoom.ZoomOut();
    }
}
