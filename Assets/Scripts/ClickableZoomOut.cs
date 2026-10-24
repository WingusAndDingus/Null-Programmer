using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableZoomOut : MonoBehaviour
{
    [SerializeField] private CameraZoom cameraZoom;

    private void OnMouseDown()
    {
        // If the player is clicking on a UI element (like the dialogue panel), ignore the click
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        cameraZoom.ZoomOut();
    }
}
