using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AutoWireSkipButton : MonoBehaviour
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
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.SkipTutorial();
        }
        else
        {
            Debug.LogWarning("[AutoWireSkipButton] Clicked, but ZoomScreenShareController.Instance is null!");
        }
    }
}
