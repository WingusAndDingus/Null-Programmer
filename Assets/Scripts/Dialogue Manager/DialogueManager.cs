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

    [Header("Bubble Layout Positions")]
    [SerializeField] private Vector2[] singleBubbleLayout = new Vector2[] { new Vector2(669, -400) };
    [SerializeField] private Vector2[] twoBubbleLayout = new Vector2[] { new Vector2(-350, 0), new Vector2(350, 0) };
    [SerializeField] private Vector2[] threeBubbleLayout = new Vector2[] { new Vector2(-350, -50), new Vector2(350, -50), new Vector2(0, 200) };
    [SerializeField] private Vector2[] fourBubbleLayout = new Vector2[] { new Vector2(-350, 150), new Vector2(350, 150), new Vector2(-350, -150), new Vector2(350, -150) };

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
        // Debugging
        /*if (activeBubbles == null || activeBubbles.Count == 0)
        {
            HideDialogueUI();
        }*/
        HideDialoguePanel();
        activeDialogueCoroutine = null;
        currentPlayingLine = null;
    }
    // To help with Dialogue after Fixing Choice Bubbles 
    public void HideDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (internalMonologueOverlay != null)
        {
            internalMonologueOverlay.SetActive(false);
        }
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
        Debug.Log($"[DialogueManager] PresentChoices called with {options?.Length ?? 0} options.");
        ClearChoices();

        if (activeVideoPlayer != null)
            activeVideoPlayer.Pause();

        TaskCompletionSource<int> choiceTask = new TaskCompletionSource<int>();
        int count = options.Length;

        for (int i = 0; i < count; i++)
        {
            GameObject bubbleObj = Instantiate(choiceBubblePrefab, choiceContainer);
            activeBubbles.Add(bubbleObj);

            // Dynamically compute vector based on total choice count
            Vector3 targetPos = GetDynamicBubblePosition(i, count);

            bubbleObj.transform.localPosition = targetPos;

            DialogueBubble bubble = bubbleObj.GetComponent<DialogueBubble>();
            Color personalityColor = GetPersonalityColor(options[i].personality);

            int optionIndex = i; // Local copy for closure
            bubble.Setup(options[i], optionIndex, personalityColor, (selectedIndex) =>
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
    /// <summary>
    /// Returns screen coordinates mapped cleanly based on total choice count.
    /// </summary>
    // private Vector3 GetDynamicBubblePosition(int index, int totalChoices)
    // {
    //     switch (totalChoices)
    //     {
    //         case 1:
    //             // Single choice in the center
    //             return new Vector2(0, 0);

    //         case 2:
    //             // Left and Right
    //             Vector2[] layout2 = new Vector2[]
    //             {
    //             new Vector2(-350, 0), // Choice 1: Left
    //             new Vector2(350, 0)   // Choice 2: Right
    //             };
    //             return layout2[Mathf.Clamp(index, 0, layout2.Length - 1)];

    //         case 3:
    //             // Left, Right, Center-Top
    //             Vector2[] layout3 = new Vector2[]
    //             {
    //             new Vector2(-350, -50), // Choice 1: Left
    //             new Vector2(350, -50),  // Choice 2: Right
    //             new Vector2(0, 200)     // Choice 3: Center Top
    //             };
    //             return layout3[Mathf.Clamp(index, 0, layout3.Length - 1)];

    //         case 4:
    //             // 4 Quadrants / Corners
    //             Vector2[] layout4 = new Vector2[]
    //             {
    //             new Vector2(-350, 150),  // Choice 1: Top-Left
    //             new Vector2(350, 150),   // Choice 2: Top-Right
    //             new Vector2(-350, -150), // Choice 3: Bottom-Left
    //             new Vector2(350, -150)   // Choice 4: Bottom-Right
    //             };
    //             return layout4[Mathf.Clamp(index, 0, layout4.Length - 1)];

    //         default:
    //             // Vertical stack fallback for 5+ items
    //             return new Vector3(0, 150 - (index * 100f), 0);
    //     }
    // }
    private Vector3 GetDynamicBubblePosition(int index, int totalChoices)
    {
        switch (totalChoices)
        {
            case 1:
                return singleBubbleLayout[Mathf.Clamp(index, 0, singleBubbleLayout.Length - 1)];
            case 2:
                return twoBubbleLayout[Mathf.Clamp(index, 0, twoBubbleLayout.Length - 1)];
            case 3:
                return threeBubbleLayout[Mathf.Clamp(index, 0, threeBubbleLayout.Length - 1)];
            case 4:
                return fourBubbleLayout[Mathf.Clamp(index, 0, fourBubbleLayout.Length - 1)];
            default:
                return new Vector3(0, 150 - (index * 100f), 0);
        }
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
