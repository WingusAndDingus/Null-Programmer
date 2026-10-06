using System.Collections;
using UnityEngine;

public class WakeUpSequence : MonoBehaviour
{
    [Header("Desktop Reference")]
    [SerializeField] private Startbutton desktopButton;

    private void Awake()
    {
        if (desktopButton != null)
        {
            desktopButton.canInteract = false;
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "Me",
            text = "Ugh... My head is pounding. Where am I?",
            personality = HeadPersonality.None,
            displayDuration = 3.5f
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Detective",
            text = "It seems we have no memory of ourselves, how curious...",
            personality = HeadPersonality.Detective,
            displayDuration = 4.0f
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Grand Architect",
            text = "We should collect our thoughts and think of how best to move forward.",
            personality = HeadPersonality.GrandArchitect,
            displayDuration = 4.0f
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Pragmatist",
            text = "Oh hey, shouldn't that be our desktop? Let's just go over there.",
            personality = HeadPersonality.Pragmatist,
            displayDuration = 4.0f
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Detective",
            text = "Perhaps we could find more clues about our past.",
            personality = HeadPersonality.Detective,
            displayDuration = 3.5f
        }));

        // Re-enable interactions when dialogue finishes
        if (desktopButton != null)
        {
            desktopButton.canInteract = true;
        }
    }

    private IEnumerator PlayAndWait(DialogueLine line)
    {
        if (DialogueManager.Instance == null) yield break;

        DialogueManager.Instance.DisplayLine(line);

        yield return new WaitUntil(() => DialogueManager.Instance.IsTypingComplete &&
                                          DialogueManager.Instance.CurrentText == line.text);

        yield return new WaitForSecondsRealtime(line.displayDuration > 0 ? line.displayDuration : 2.5f);
    }
}
