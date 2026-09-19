using UnityEngine;

public class ClickableZoomOut : MonoBehaviour
{
    [SerializeField] private CameraZoom cameraZoom;

    private void OnMouseDown()
    {
        cameraZoom.ZoomOut();
    }
}