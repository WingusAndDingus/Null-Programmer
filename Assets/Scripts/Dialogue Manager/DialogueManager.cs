using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

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
    private RectTransform choiceViewport;
    private RectTransform choiceContent;
    private ScrollRect choiceScroll;
    private Canvas choiceCanvas;
    private Vector2 lastChoiceViewportSize;
    private float lastChoiceScale;

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

    private DialoguePanelPresentation panelPresentation;
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

        panelPresentation = GetComponent<DialoguePanelPresentation>();
        HideDialogueUI();
    }

    public void DisplayLine(DialogueLine line)
    {
        if (line == null) return;

        // If the exact same text is currently typing, don't restart the coroutine!
        if (CurrentText == line.text && activeDialogueCoroutine != null && !IsTypingComplete &&
            currentPlayingLine != null && currentPlayingLine.speakerName == line.speakerName &&
            currentPlayingLine.personality == line.personality)
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
    /// <summary>
    /// Called when the player clicks the dialogue panel to skip typing or speed up pacing.
    /// </summary>
    public void OnDialogueClicked()
    {
        if (currentPlayingLine == null) return;

        // If typing is still in progress, instantly finish displaying all characters
        if (!IsTypingComplete)
        {
            // STOP the coroutine so it stops looping and overwriting characters!
            if (activeDialogueCoroutine != null)
            {
                StopCoroutine(activeDialogueCoroutine);
                activeDialogueCoroutine = null;
            }

            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = dialogueText.text.Length;
                if (panelPresentation != null)
                    panelPresentation.FollowTyping(dialogueText.text.Length);
            }
            UpdateProgress(currentPlayingLine.text, dialogueText.text.Length, true);
        }
        else
        {
            // If typing is already complete, clicking closes the line immediately!
            if (activeDialogueCoroutine != null)
            {
                StopCoroutine(activeDialogueCoroutine);
                activeDialogueCoroutine = null;
            }
            currentPlayingLine = null;
            HideDialoguePanel();
        }
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

        if (panelPresentation != null) panelPresentation.Show(line);

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
                if (panelPresentation != null) panelPresentation.FollowTyping(i);
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

        if (options == null || options.Length == 0)
            throw new ArgumentException("At least one dialogue choice is required.", nameof(options));
        if (choiceBubblePrefab == null)
            throw new InvalidOperationException("Assign the Thought Bubble prefab on the Dialogue Canvas.");

        ClearChoices();
        // ENSURE THE VIEWPORT IS GENERATED IN THE SCENE AT RUNTIME
        EnsureChoiceViewport();

        // Team's new viewport/content setup
        if (choiceViewport != null)
            choiceViewport.gameObject.SetActive(true);

        if (activeVideoPlayer != null)
            activeVideoPlayer.Pause();

        var choiceTask = new TaskCompletionSource<int>();
        try
        {
            for (int i = 0; i < options.Length; i++)
            {
                Transform parentTransform = choiceContent != null ? choiceContent : choiceContainer;
                // Instantiate without a parent first to avoid the persistent parent error
                GameObject bubbleObj = Instantiate(choiceBubblePrefab);
                if (parentTransform != null)
                {
                    bubbleObj.transform.SetParent(parentTransform, false);
                }

                activeBubbles.Add(bubbleObj);

                DialogueBubble bubble = bubbleObj.GetComponent<DialogueBubble>();
                if (bubble == null)
                    throw new InvalidOperationException("Choice prefab needs DialogueBubble.");

                Color personalityColor = GetPersonalityColor(options[i].personality);
                int optionIndex = i; // Local copy for closure

                bubble.Setup(options[i], optionIndex, personalityColor, (selectedIndex) =>
                {
                    choiceTask.TrySetResult(selectedIndex);
                });
            }

            Canvas.ForceUpdateCanvases();

            // If your team's system uses LayoutChoices, call it here, 
            // or apply your dynamic positions if needed.
            LayoutChoices(true);

            return await choiceTask.Task;
        }
        finally
        {
            // Proper cleanup logic goes here
            ClearChoices();

            if (choiceViewport != null)
                choiceViewport.gameObject.SetActive(false);

            if (internalMonologueOverlay != null)
                internalMonologueOverlay.SetActive(false);

            // Resume video playback when dialogue choice resolves
            if (activeVideoPlayer != null && !activeVideoPlayer.isPlaying)
                activeVideoPlayer.Play();
        }
    }
    private void EnsureChoiceViewport()
    {
        if (choiceViewport != null) return;
        choiceCanvas = GetComponentInParent<Canvas>();
        if (choiceCanvas == null && choiceContainer != null)
            choiceCanvas = choiceContainer.GetComponentInParent<Canvas>();
        if (choiceCanvas == null)
            throw new InvalidOperationException("Dialogue choices must be displayed under a Canvas.");

        // The old scene container is only 100 x 100 and offset above the screen.
        // Use a viewport relative to the canvas instead of inheriting those offsets.
        var viewport = new GameObject("Responsive Dialogue Choices", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.layer = choiceCanvas.gameObject.layer;
        choiceViewport = viewport.GetComponent<RectTransform>();
        choiceViewport.SetParent(choiceCanvas.transform, false);
        choiceViewport.anchorMin = new Vector2(0.04f, 0.26f);
        choiceViewport.anchorMax = new Vector2(0.96f, 0.94f);
        choiceViewport.offsetMin = choiceViewport.offsetMax = Vector2.zero;
        var overlayCanvas = viewport.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1500; // Above the desktop call, below exercise windows.
        var hitArea = viewport.GetComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true; // Lets the player drag a long list between cards.

        var content = new GameObject("Choice Content", typeof(RectTransform));
        content.layer = viewport.layer;
        choiceContent = content.GetComponent<RectTransform>();
        choiceContent.SetParent(choiceViewport, false);
        choiceContent.anchorMin = choiceContent.anchorMax = new Vector2(0.5f, 1f);
        choiceContent.pivot = new Vector2(0.5f, 1f);
        choiceScroll = viewport.GetComponent<ScrollRect>();
        choiceScroll.viewport = choiceViewport;
        choiceScroll.content = choiceContent;
        choiceScroll.horizontal = false;
        choiceScroll.movementType = ScrollRect.MovementType.Clamped;
        choiceScroll.scrollSensitivity = 45f;
    }

    private void LayoutChoices(bool animate)
    {
        Vector2 size = choiceViewport.rect.size;
        float scale = choiceCanvas.rootCanvas.scaleFactor;
        var plan = DialogueChoiceLayout.Calculate(size, activeBubbles.Count, scale,
            (i, width, fontSize) => activeBubbles[i].GetComponent<DialogueBubble>().MeasureHeight(width, fontSize));
        choiceContent.sizeDelta = new Vector2(size.x, plan.contentHeight);
        choiceContent.anchoredPosition = Vector2.zero;
        choiceScroll.StopMovement();
        choiceScroll.vertical = plan.contentHeight > size.y + 0.5f;
        for (int i = 0; i < activeBubbles.Count; i++)
        {
            var bubble = activeBubbles[i].GetComponent<DialogueBubble>();
            bubble.FitToText(plan.width, plan.fontSize, plan.heights[i]);
            bubble.SetLayoutPosition(plan.positions[i], animate);
        }
        lastChoiceViewportSize = size;
        lastChoiceScale = scale;
    }

    private void LateUpdate()
    {
        if (activeBubbles.Count == 0 || choiceViewport == null) return;
        if ((choiceViewport.rect.size - lastChoiceViewportSize).sqrMagnitude > 0.01f ||
            !Mathf.Approximately(choiceCanvas.rootCanvas.scaleFactor, lastChoiceScale))
            LayoutChoices(false);
    }
    private void Update()
    {
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool enterPressed = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;

            bool clickedOnPanel = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Check if the mouse click is actually inside the dialogue panel's RectTransform bounds
                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
                    // If the click is inside the panel, count it!
                    if (RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos, null))
                    {
                        clickedOnPanel = true;
                    }
                }
            }

            if (clickedOnPanel || spacePressed || enterPressed)
            {
                OnDialogueClicked();
            }
        }
    }
    private void ClearChoices()
    {
        foreach (var bubble in activeBubbles)
        {
            if (bubble == null) continue;
            bubble.SetActive(false);
            Destroy(bubble);
        }
        activeBubbles.Clear();
        if (choiceViewport != null) choiceViewport.gameObject.SetActive(false);
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
    //helper method to check if the mouse is currently inside either the dialogue box or the choices viewport:
    public bool IsPointerOverDialogueUI(Vector2 mousePosition)
    {
        // Check main dialogue panel
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition, null))
            {
                return true;
            }
        }

        // Check choice viewport if active
        if (choiceViewport != null && choiceViewport.gameObject.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(choiceViewport, mousePosition, null))
            {
                return true;
            }
        }

        return false;
    }
}
