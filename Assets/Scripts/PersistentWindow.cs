using UnityEngine;

public class PersistentWindow : MonoBehaviour
{
    [SerializeField] private WindowIdentity windowIdentity;
    [SerializeField] private GameObject windowRoot;
    //[SerializeField] private DraggableWindow draggableWindow;

    private void Awake()
    {
        if (windowIdentity == null &&
            windowRoot != null)
        {
            windowIdentity =
                windowRoot.GetComponent<WindowIdentity>();
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null)
            return;

        if (windowIdentity == null ||
            !windowIdentity.HasValidId)
        {
            Debug.LogError(
                "PersistentWindow needs a valid WindowIdentity.",
                this);

            return;
        }

        bool shouldBeOpen =
            GameManager.Instance.IsWindowOpen(
                windowIdentity.Id);

        windowRoot.SetActive(shouldBeOpen);
    }

    public void CloseWindow()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetWindowOpen(
                windowIdentity.Id,
                false);

            // Closed windows reopen at normal size
            GameManager.Instance.SetWindowMaximized(
                windowIdentity.Id,
                false);
        }

        windowRoot.SetActive(false);
    }

    public void OpenWindow()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetWindowOpen(
                windowIdentity.Id,
                true);

            // Reopening puts it in the foreground
            GameManager.Instance.BringWindowToFront(
                windowIdentity.Id);
        }

        windowRoot.SetActive(true);
    }
}