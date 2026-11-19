using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomedOutSize = 8f;
    [SerializeField] private float zoomedInSize = 5f;

    [Header("Camera Positions")]
    [SerializeField] private Vector2 zoomedOutPosition;
    [SerializeField] private Vector2 zoomedInPosition;

    private void Start()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        // Day 7 can inherit the zoomed-out view from the previous desktop scene.
        if (game.CurrentDay == 7 && gameObject.scene.name == SceneSwitcher.CenterMonitorScene
            && !game.IsZoomedIn)
        {
            game.SetZoomedIn(true);
        }

        ApplyCurrentView();
    }

    public void ZoomIn()
    {
        if (GameManager.Instance.IsZoomedIn)
        {
            return;
        }

        GameManager.Instance.SetZoomedIn(true);
        ApplyCurrentView();
    }

    public void ZoomOut()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.CurrentDay == 7 || !game.IsZoomedIn) return;

        game.SetZoomedIn(false);
        ApplyCurrentView();
    }

    private void ApplyCurrentView()
    {
        Vector3 currentPosition = mainCamera.transform.position;

        if (GameManager.Instance.IsZoomedIn)
        {
            mainCamera.orthographicSize = zoomedInSize;

            mainCamera.transform.position = new Vector3(
                zoomedInPosition.x,
                zoomedInPosition.y,
                currentPosition.z
            );
        }
        else
        {
            mainCamera.orthographicSize = zoomedOutSize;

            mainCamera.transform.position = new Vector3(
                zoomedOutPosition.x,
                zoomedOutPosition.y,
                currentPosition.z
            );
        }
    }
}
