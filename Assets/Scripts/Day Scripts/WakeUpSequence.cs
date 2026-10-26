using System;
using System.Collections;
using Unity.Tutorials.Editor;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class WakeUpSequence : MonoBehaviour
{
    [Header("Desktop Reference")]
    [SerializeField] private Startbutton desktopButton;
    [SerializeField] private Startbutton sleepButton;

    private void Awake()
    {
        if (desktopButton != null)
        {
            desktopButton.canInteract = false;
        }

        if (sleepButton != null)
        {
            sleepButton.canInteract = false;
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "Me",
            text = "Ugh... My head is pounding. Where am I?",
            personality = HeadPersonality.None
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Detective",
            text = "It seems we have no memory of ourselves, how curious...",
            personality = HeadPersonality.Detective
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Grand Architect",
            text = "We should collect our thoughts and think of how best to move forward.",
            personality = HeadPersonality.GrandArchitect
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Pragmatist",
            text = "Oh hey, shouldn't that be our desktop? Let's just go over there.",
            personality = HeadPersonality.Pragmatist
        }));

        yield return StartCoroutine(PlayAndWait(new DialogueLine
        {
            speakerName = "The Detective",
            text = "Perhaps we could find more clues about our past.",
            personality = HeadPersonality.Detective
        }));

        // Re-enable interactions only after ALL dialogue lines have been clicked through
        if (desktopButton != null)
        {
            desktopButton.canInteract = true;
        }

        if (sleepButton != null)
        {
            sleepButton.canInteract = true;
        }
    }

    private IEnumerator PlayAndWait(DialogueLine line)
    {
        if (DialogueManager.Instance == null) yield break;

        DialogueManager.Instance.DisplayLine(line);

        // 1. Wait until the typewriter effect finishes writing out the text
        yield return new WaitUntil(() => DialogueManager.Instance.IsTypingComplete &&
                                            DialogueManager.Instance.CurrentText == line.text);

        // 2. Wait for user input using the new Input System package
        bool advanced = false;
        while (!advanced)
        {
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool keyPressed = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);

            if (mouseClicked || keyPressed)
            {
                advanced = true;
            }
            yield return null;
        }
    }
}
