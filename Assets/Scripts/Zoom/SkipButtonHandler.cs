using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkipButtonHandler : MonoBehaviour
{
    private Button skipButton;

    private void Awake()
    {
        skipButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
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
        // 1. Try the static instance first
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.SkipTutorial();
            return;
        }

        // 2. Fallback: Search the active scene directly for any ZoomScreenShareController
        ZoomScreenShareController activeController = FindAnyObjectByType<ZoomScreenShareController>();
        if (activeController != null)
        {
            activeController.SkipTutorial();
        }
        else
        {
            Debug.LogError("[SkipButtonHandler] Clicked, but NO ZoomScreenShareController exists in the active scene!");
        }
    }
}
