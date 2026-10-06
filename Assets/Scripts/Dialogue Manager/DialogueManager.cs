using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // Track active state for seamless transitions across monitor views/scenes
    public DialogueState activeDialogueState = new DialogueState();
    public string CurrentText { get; private set; } = "";
    public int CurrentVisibleCharacters { get; private set; } = 0;
    public bool IsTypingComplete { get; private set; } = false;

    [Header("UI Panel References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image panelImage;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject internalMonologueOverlay;
    [SerializeField] private AudioSource speechAudioSource;

    [Header("Choice Bubbles Setup")]
    [SerializeField] private Transform choiceContainer;       // Canvas/Parent transform for bubbles
    [SerializeField] private GameObject choiceBubblePrefab;  // Prefab with DialogueBubble attached

    [SerializeField]
    private List<Vector2> bubblePositions = new List<Vector2>
    {
        new Vector2(-200, 0),
        new Vector2(200, 0),
        new Vector2(0, 150),
        new Vector2(0, -150)
    };

    // Zoom & Tutorial are only on one monitor
    private VideoPlayer activeVideoPlayer;

    public void RegisterVideoPlayer(VideoPlayer player)
    {
        activeVideoPlayer = player;
    }

    public void UnregisterVideoPlayer(VideoPlayer player)
    {
        if (activeVideoPlayer == player)
            activeVideoPlayer = null;
    }

    [Header("Personality Colors")]
    [SerializeField] private Color defaultPanelColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color architectColor = new Color(0.1f, 0.3f, 0.4f, 0.9f);
    [SerializeField] private Color pragmatistColor = new Color(0.4f, 0.2f, 0.1f, 0.9f);
    [SerializeField] private Color sentinelColor = new Color(0.1f, 0.4f, 0.1f, 0.9f);
    [SerializeField] private Color geniusColor = new Color(0.4f, 0.4f, 0.1f, 0.9f);
    [SerializeField] private Color detectiveColor = new Color(0.3f, 0.1f, 0.4f, 0.9f);

    private Coroutine activeDialogueCoroutine;
    private List<GameObject> activeBubbles = new List<GameObject>();
    private DialogueLine currentPlayingLine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Ensure this object sits at root so DontDestroyOnLoad works
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        HideDialogueUI();
    }

    public void DisplayLine(DialogueLine line)
    {
        if (line == null) return;

        // If the exact same text is currently typing, don't restart the coroutine!
        if (CurrentText == line.text && activeDialogueCoroutine != null && !IsTypingComplete)
        {
            return;
        }

        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
        }

        currentPlayingLine = line;
        activeDialogueCoroutine = StartCoroutine(PlayDialogueLine(line));
    }

    public IEnumerator PlayDialogueLine(DialogueLine line)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerText != null) speakerText.text = line.speakerName;

        SetPersonalityStyle(line.personality);

        float audioDuration = 0f;
        if (line.voiceClip != null && speechAudioSource != null)
        {
            // Don't restart audio if the exact same clip is already playing
            if (!speechAudioSource.isPlaying || speechAudioSource.clip != line.voiceClip)
            {
                speechAudioSource.clip = line.voiceClip;
                speechAudioSource.Play();
            }
            audioDuration = line.voiceClip.length;
        }

        // Assign full text to TMP
        if (dialogueText != null)
        {
            dialogueText.text = line.text;
        }

        // Determine starting character index:
        // If we are re-entering the exact same text and it wasn't finished, resume typing from CurrentVisibleCharacters
        int startIndex = 0;
        if (CurrentText == line.text && !IsTypingComplete)
        {
            startIndex = CurrentVisibleCharacters;
        }
        else
        {
            UpdateProgress(line.text, 0, false);
        }

        if (dialogueText != null)
        {
            dialogueText.maxVisibleCharacters = startIndex;
        }

        float typeSpeed = 0.02f;
        int totalChars = line.text != null ? line.text.Length : 0;

        for (int i = startIndex; i <= totalChars; i++)
        {
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = i;
            }

            UpdateProgress(line.text, i, i == totalChars);
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        float typingTotalTime = totalChars * typeSpeed;
        float targetDuration = line.displayDuration > 0 ? line.displayDuration : Mathf.Max(2.0f, audioDuration);
        float remainingHoldTime = Mathf.Max(0.5f, targetDuration - typingTotalTime);

        yield return new WaitForSecondsRealtime(remainingHoldTime);

        HideDialogueUI();
        activeDialogueCoroutine = null;
        currentPlayingLine = null;
    }

    public void UpdateProgress(string text, int visibleChars, bool complete)
    {
        CurrentText = text;
        CurrentVisibleCharacters = visibleChars;
        IsTypingComplete = complete;
    }

    /// <summary>
    /// Displays floating choice bubbles and waits until the player picks one.
    /// Returns the selected index (0 to options.Length - 1).
    /// </summary>
    public async Task<int> PresentChoices(DialogueChoiceOption[] options)
    {
        ClearChoices();

        if (internalMonologueOverlay != null)
            internalMonologueOverlay.SetActive(true);

        if (activeVideoPlayer != null)
            activeVideoPlayer.Pause();

        TaskCompletionSource<int> choiceTask = new TaskCompletionSource<int>();

        for (int i = 0; i < options.Length; i++)
        {
            GameObject bubbleObj = Instantiate(choiceBubblePrefab, choiceContainer);
            activeBubbles.Add(bubbleObj);

            // Position bubble
            Vector3 targetPos = (i < bubblePositions.Count)
                ? (Vector3)bubblePositions[i]
                : new Vector3(0, i * -80f, 0);

            bubbleObj.transform.localPosition = targetPos;

            DialogueBubble bubble = bubbleObj.GetComponent<DialogueBubble>();
            Color personalityColor = GetPersonalityColor(options[i].personality);

            bubble.Setup(options[i], i, personalityColor, (selectedIndex) =>
            {
                choiceTask.TrySetResult(selectedIndex);
            });

            // Lock in position for floating sine wave animation
            bubble.SetInitialPosition(targetPos);
        }

        int selectedOption = await choiceTask.Task;

        ClearChoices();

        if (internalMonologueOverlay != null)
            internalMonologueOverlay.SetActive(false);

        // Resume video playback when dialogue choice resolves
        if (activeVideoPlayer != null && !activeVideoPlayer.isPlaying)
            activeVideoPlayer.Play();

        return selectedOption;
    }

    private void ClearChoices()
    {
        foreach (var bubble in activeBubbles)
        {
            if (bubble != null) Destroy(bubble);
        }
        activeBubbles.Clear();
    }

    public void HideDialogueUI()
    {
        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
            activeDialogueCoroutine = null;
        }

        currentPlayingLine = null;
        ClearChoices();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (internalMonologueOverlay != null) internalMonologueOverlay.SetActive(false);

        if (activeVideoPlayer != null && !activeVideoPlayer.isPlaying)
        {
            activeVideoPlayer.Play();
        }
    }

    public Color GetPersonalityColor(HeadPersonality personality)
    {
        return personality switch
        {
            HeadPersonality.GrandArchitect => architectColor,
            HeadPersonality.Pragmatist => pragmatistColor,
            HeadPersonality.Sentinel => sentinelColor,
            HeadPersonality.Genius => geniusColor,
            HeadPersonality.Detective => detectiveColor,
            _ => defaultPanelColor
        };
    }

    private void SetPersonalityStyle(HeadPersonality personality)
    {
        bool isInternal = (personality != HeadPersonality.None);

        if (internalMonologueOverlay != null)
            internalMonologueOverlay.SetActive(isInternal);

        if (activeVideoPlayer != null)
        {
            if (isInternal)
                activeVideoPlayer.Pause();
            else if (!activeVideoPlayer.isPlaying)
                activeVideoPlayer.Play();
        }

        if (panelImage != null)
        {
            panelImage.color = GetPersonalityColor(personality);
        }
    }
}
