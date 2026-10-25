using UnityEngine;

public class NightScreenController : MonoBehaviour
{
    [Header("Night Monologue")]
    [TextArea(2, 4)]
    [SerializeField] private string tiredMessage = "Ugh I'm so tired, better go to bed for now.";
    [SerializeField] private HeadPersonality monologuePersonality = HeadPersonality.Pragmatist;
    [SerializeField] private float displayDuration = 3.5f;

    private void Start()
    {
        PlayNightDialogue();
    }

    private void PlayNightDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueLine nightLine = new DialogueLine
            {
                speakerName = "",
                personality = monologuePersonality,
                text = tiredMessage,
                displayDuration = displayDuration
            };

            DialogueManager.Instance.DisplayLine(nightLine);
        }
    }
}
