using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PersistentSkipHandler : MonoBehaviour
{
    private Button skipButton;

    private void Awake()
    {
        skipButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Re-bind the click listener dynamically every time the button is enabled or a scene changes
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipClicked);
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }

    private void OnDisable()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipClicked);
        }
    }

    private void OnSkipClicked()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.SkipTutorial();
        }
        else
        {
            Debug.LogWarning("[SkipButton] Clicked, but no active ZoomScreenShareController instance was found!");
        }
    }
}
