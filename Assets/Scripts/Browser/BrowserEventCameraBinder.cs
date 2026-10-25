using UnityEngine;
using UnityEngine.SceneManagement;

public class BrowserEventCameraBinder : MonoBehaviour
{
    [SerializeField]
    private Canvas browserCanvas;

    private void Awake()
    {
        if (browserCanvas == null)
        {
            browserCanvas = GetComponent<Canvas>();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        TryAssignCamera();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAssignCamera();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (browserCanvas != null &&
            browserCanvas.worldCamera != null &&
            browserCanvas.worldCamera.gameObject.scene == scene)
        {
            browserCanvas.worldCamera = null;
        }
    }

    private void TryAssignCamera()
    {
        LeftMonitorCameraMarker marker =
            FindFirstObjectByType<LeftMonitorCameraMarker>();

        if (marker == null)
        {
            return;
        }

        Camera leftMonitorCamera = marker.GetComponent<Camera>();

        if (leftMonitorCamera != null && browserCanvas != null)
        {
            browserCanvas.worldCamera = leftMonitorCamera;

            Debug.Log(
                "[BrowserEventCameraBinder] Assigned browser event camera: " +
                leftMonitorCamera.name
            );
        }
    }
}