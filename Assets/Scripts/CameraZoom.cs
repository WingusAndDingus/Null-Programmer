using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float zoomedOutSize = 8f;
    [SerializeField] private float zoomedInSize = 5f;

    public void ZoomIn()
    {
        if (mainCamera.orthographicSize >= 7)
        {
            mainCamera.orthographicSize = zoomedInSize;
        }
    }

    public void ZoomOut()
    {
        if (mainCamera.orthographicSize < 7)
        {
            mainCamera.orthographicSize = zoomedOutSize;
        }
    }
}