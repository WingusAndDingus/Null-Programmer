using UnityEngine;

public class ClickableZoom : MonoBehaviour
{
    [SerializeField] private CameraZoom cameraZoom;

    private void OnMouseDown()
    {
        if (DialogueManager.BlocksMonitorClick() || RefactoFightApp.IsFightActive || GameEndManager.IsEndScreenActive) return;
        cameraZoom.ZoomIn();
    }
}
