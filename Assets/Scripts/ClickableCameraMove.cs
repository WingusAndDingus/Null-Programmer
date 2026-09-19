using UnityEngine;

public class ClickableCameraMove : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector2 newCameraPosition;

    private void OnMouseDown()
    {
        Vector3 currentPosition = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(
            newCameraPosition.x,
            newCameraPosition.y,
            currentPosition.z
        );
    }
}