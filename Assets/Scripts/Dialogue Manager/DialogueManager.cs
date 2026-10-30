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
    private RectMask2D choiceMask;
    private Canvas choiceCanvas;
    private Vector2 lastChoiceViewportSize;
    private float lastChoiceScale;
    private bool usingDay5ChoicePlacement;

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
    private bool waitingForClickToContinue = false;

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
    //wrapper to allow old code expecting the old function
    public IEnumerator PlayDialogueLine(DialogueLine line)
    {
        bool isDone = false;
        yield return StartCoroutine(PlayDialogueLineRoutine(line, () => isDone = true));
    }
    public void DisplayLine(DialogueLine line)
    {
        _ = DisplayLineAndWaitAsync(line);
    }

    public async Task DisplayLineAndWaitAsync(DialogueLine line)
    {
        if (line == null) return;

        if (activeDialogueCoroutine != null)
        {
            StopCoroutine(activeDialogueCoroutine);
        }

        currentPlayingLine = line;
        var tcs = new TaskCompletionSource<bool>();
        activeDialogueCoroutine = StartCoroutine(PlayDialogueLineRoutine(line, () => tcs.TrySetResult(true)));

        await tcs.Task;
    }

    public void OnDialogueClicked()
    {
        if (currentPlayingLine == null) return;

        // If typing is still in progress, instantly finish displaying all characters
        if (!IsTypingComplete)
        {
            IsTypingComplete = true;
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = dialogueText.text.Length;
                if (panelPresentation != null)
                    panelPresentation.FollowTyping(dialogueText.text.Length);
            }
            UpdateProgress(currentPlayingLine.text, dialogueText.text.Length, true);
        }
        else if (waitingForClickToContinue)
        {
            // If typing is already complete, clicking proceeds to the next line!
            waitingForClickToContinue = false;
        }
    }

    private IEnumerator PlayDialogueLineRoutine(DialogueLine line, Action onComplete)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerText != null) speakerText.text = line.speakerName;

        SetPersonalityStyle(line.personality);

        if (line.voiceClip != null && speechAudioSource != null)
        {
            if (!speechAudioSource.isPlaying || speechAudioSource.clip != line.voiceClip)
            {
                speechAudioSource.clip = line.voiceClip;
                speechAudioSource.Play();
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
        }

        if (panelPresentation != null) panelPresentation.Show(line);

        UpdateProgress(line.text, 0, false);
        int totalChars = line.text != null ? line.text.Length : 0;
        float typeSpeed = 0.02f;

        // Phase 1: Type out characters progressively
        for (int i = 0; i <= totalChars; i++)
        {
            if (IsTypingComplete) break;

            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = i;
                if (panelPresentation != null) panelPresentation.FollowTyping(i);
            }

            UpdateProgress(line.text, i, i == totalChars);
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        // Ensure fully visible
        if (dialogueText != null)
        {
            dialogueText.maxVisibleCharacters = totalChars;
        }
        UpdateProgress(line.text, totalChars, true);

        // Phase 2: Wait indefinitely for player's second click to continue
        waitingForClickToContinue = true;
        while (waitingForClickToContinue)
        {
            yield return null;
        }

        HideDialoguePanel();
        activeDialogueCoroutine = null;
        currentPlayingLine = null;
        onComplete?.Invoke();
    }

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

    public async Task<int> PresentChoices(DialogueChoiceOption[] options)
    {
        Debug.Log($"[DialogueManager] PresentChoices called with {options?.Length ?? 0} options.");

        if (options == null || options.Length == 0)
            throw new ArgumentException("At least one dialogue choice is required.", nameof(options));
        if (choiceBubblePrefab == null)
            throw new InvalidOperationException("Assign the Thought Bubble prefab on the Dialogue Canvas.");

        ClearChoices();
        EnsureChoiceViewport();
        ApplyChoiceViewportForCurrentDay();

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
                int optionIndex = i;

                bubble.Setup(options[i], optionIndex, personalityColor, (selectedIndex) =>
                {
                    choiceTask.TrySetResult(selectedIndex);
                });
            }

            Canvas.ForceUpdateCanvases();
            LayoutChoices(true);

            return await choiceTask.Task;
        }
        finally
        {
            ClearChoices();

            if (choiceViewport != null)
                choiceViewport.gameObject.SetActive(false);

            if (internalMonologueOverlay != null)
                internalMonologueOverlay.SetActive(false);

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

        var viewport = new GameObject("Responsive Dialogue Choices", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.layer = choiceCanvas.gameObject.layer;
        choiceViewport = viewport.GetComponent<RectTransform>();
        choiceViewport.SetParent(choiceCanvas.transform, false);
        choiceViewport.anchorMin = new Vector2(0.04f, 0.26f);
        choiceViewport.anchorMax = new Vector2(0.96f, 0.94f);

        GameManager manager = GameManager.Instance;
        if (manager != null && manager.CurrentDay == 5)
        {
            // Day 5 layout.
            // This keeps the choices lower on the screen so they interfere
            // less with Steve's shared code.
            choiceViewport.anchorMin = new Vector2(0.04f, 0.16f);
            choiceViewport.anchorMax = new Vector2(0.96f, 0.48f);
        }

        choiceViewport.offsetMin = choiceViewport.offsetMax = Vector2.zero;
        choiceMask = viewport.GetComponent<RectMask2D>();
        var overlayCanvas = viewport.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1500;
        var hitArea = viewport.GetComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;

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

    // Reapply the choice viewport each time choices are shown because this
    // DialogueManager persists across scenes/days. Day 5 deliberately uses a
    // shallow viewport; its mask must be disabled so tall bubbles are not cut off.
    private void ApplyChoiceViewportForCurrentDay()
    {
        if (choiceViewport == null) return;

        GameManager manager = GameManager.Instance;
        usingDay5ChoicePlacement = manager != null && manager.CurrentDay == 5;

        if (usingDay5ChoicePlacement)
        {
            // Preserve the original Day 5 placement exactly.
            choiceViewport.anchorMin = new Vector2(0.04f, 0.16f);
            choiceViewport.anchorMax = new Vector2(0.96f, 0.48f);
        }
        else
        {
            // Preserve the original placement for every other day.
            choiceViewport.anchorMin = new Vector2(0.04f, 0.26f);
            choiceViewport.anchorMax = new Vector2(0.96f, 0.94f);
        }

        choiceViewport.offsetMin = choiceViewport.offsetMax = Vector2.zero;

        if (choiceMask != null)
            choiceMask.enabled = !usingDay5ChoicePlacement;
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
        choiceScroll.vertical = !usingDay5ChoicePlacement && plan.contentHeight > size.y + 0.5f;
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
                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
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

    public bool IsPointerOverDialogueUI(Vector2 mousePosition)
    {
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition, null))
            {
                return true;
            }
        }

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
