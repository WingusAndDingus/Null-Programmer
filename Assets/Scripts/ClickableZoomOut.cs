using UnityEngine;

public class ClickableZoomOut : MonoBehaviour
{
    [SerializeField] private CameraZoom cameraZoom;

    private void OnMouseDown()
    {
        if (DialogueManager.BlocksMonitorClick() || RefactoFightApp.IsFightActive || GameEndManager.IsEndScreenActive) return;
        cameraZoom.ZoomOut();
    }
}
