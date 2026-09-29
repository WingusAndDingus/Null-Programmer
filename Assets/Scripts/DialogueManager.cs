using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video; // Added for VideoPlayer control

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Panel References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image panelImage;                 // Changed: Target the panel background Image
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject internalMonologueOverlay;
    [SerializeField] private AudioSource speechAudioSource;

    [Header("Video Control")]
    [SerializeField] private VideoPlayer screenShareVideoPlayer; // Reference your in-game screen share video player

    [Header("Personality Colors")]
    [SerializeField] private Color defaultPanelColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color architectColor = new Color(0.1f, 0.3f, 0.4f, 0.9f); // Cyan dark tint
    [SerializeField] private Color pragmatistColor = new Color(0.4f, 0.2f, 0.1f, 0.9f); // Orange dark tint
    [SerializeField] private Color sentinelColor = new Color(0.1f, 0.4f, 0.1f, 0.9f); // Green dark tint
    [SerializeField] private Color geniusColor = new Color(0.4f, 0.4f, 0.1f, 0.9f); // Yellow dark tint
    [SerializeField] private Color detectiveColor = new Color(0.3f, 0.1f, 0.4f, 0.9f); // Purple dark tint

    private Coroutine activeDialogueCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideDialogueUI();
    }

    public void DisplayLine(DialogueLine line)
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
        }
        activeDialogueCoroutine = StartCoroutine(PlayDialogueLine(line));
    }

    public IEnumerator PlayDialogueLine(DialogueLine line)
    {
        // 1. Setup UI elements
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        speakerText.text = line.speakerName;

        // Apply background styling & pause video if it's an internal monologue
        SetPersonalityStyle(line.personality);

        // 2. Play Audio if assigned
        float audioDuration = 0f;
        if (line.voiceClip != null && speechAudioSource != null)
        {
            speechAudioSource.clip = line.voiceClip;
            speechAudioSource.Play();
            audioDuration = line.voiceClip.length;
        }

        // 3. Typewriter Effect
        dialogueText.text = "";
        char[] characters = line.text.ToCharArray();
        float typeSpeed = 0.02f;
        float typingTotalTime = characters.Length * typeSpeed;

        foreach (char c in characters)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        // 4. Calculate remaining display duration
        // Uses the explicit line duration or falls back to audio clip length / default minimum
        float targetDuration = line.displayDuration > 0 ? line.displayDuration : Mathf.Max(2.0f, audioDuration);

        // Subtract the time already spent typing so display duration is accurate
        float remainingHoldTime = Mathf.Max(0.5f, targetDuration - typingTotalTime);

        yield return new WaitForSecondsRealtime(remainingHoldTime);

        // 5. Cleanup
        HideDialogueUI();
        activeDialogueCoroutine = null;
    }

    public void HideDialogueUI()
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
            activeDialogueCoroutine = null;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (internalMonologueOverlay != null) internalMonologueOverlay.SetActive(false);

        // Resume video playback when dialogue closes
        if (screenShareVideoPlayer != null && !screenShareVideoPlayer.isPlaying)
        {
            screenShareVideoPlayer.Play();
        }
    }

    private void SetPersonalityStyle(HeadPersonality personality)
    {
        bool isInternal = (personality != HeadPersonality.None);

        // Overlay setup
        if (internalMonologueOverlay != null)
            internalMonologueOverlay.SetActive(isInternal);

        // Control Video Player specifically instead of altering global Time.timeScale
        if (screenShareVideoPlayer != null)
        {
            if (isInternal)
                screenShareVideoPlayer.Pause();
            else if (!screenShareVideoPlayer.isPlaying)
                screenShareVideoPlayer.Play();
        }

        // Apply background panel color
        Color selectedColor = personality switch
        {
            HeadPersonality.GrandArchitect => architectColor,
            HeadPersonality.Pragmatist => pragmatistColor,
            HeadPersonality.Sentinel => sentinelColor,
            HeadPersonality.Genius => geniusColor,
            HeadPersonality.Detective => detectiveColor,
            _ => defaultPanelColor
        };

        if (panelImage != null)
        {
            panelImage.color = selectedColor;
        }
    }
}
