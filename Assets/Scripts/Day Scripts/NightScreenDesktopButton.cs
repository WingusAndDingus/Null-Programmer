using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NightScreenDesktopButton : MonoBehaviour
{
    [SerializeField] private string refusalMessage = "No way I can keep working, I need to sleep.";
    [SerializeField] private float displayDuration = 3.0f;
    [SerializeField] private HeadPersonality monologuePersonality = HeadPersonality.Pragmatist;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnDesktopClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnDesktopClicked);
        }
    }

    private void OnDesktopClicked()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueLine refusalLine = new DialogueLine
            {
                speakerName = "",
                personality = monologuePersonality,
                text = refusalMessage,
                displayDuration = displayDuration
            };

            DialogueManager.Instance.DisplayLine(refusalLine);
        }
    }
}
