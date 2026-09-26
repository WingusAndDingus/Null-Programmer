using UnityEngine;

public class PersistentWindow : MonoBehaviour
{
    [SerializeField] private string windowId;
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private DraggableWindow draggableWindow;

    private void Start()
    {
        if (GameManager.Instance == null)
            return;

        bool shouldBeOpen =
            GameManager.Instance.IsWindowOpen(windowId);

        windowRoot.SetActive(shouldBeOpen);
    }

    public void CloseWindow()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetWindowOpen(windowId, false);

            // Closing the window also forgets its maximized state.
            GameManager.Instance.SetWindowMaximized(windowId, false);
        }

        windowRoot.SetActive(false);
    }

    public void OpenWindow()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetWindowOpen(windowId, true);

            // Reopening counts as bringing this window to the foreground.
            GameManager.Instance.BringWindowToFront(windowId);
        }

        windowRoot.SetActive(true);
    }
}