using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
    [Header("Responsive Choice Layout (editable at runtime in Inspector)")]
    [SerializeField, Min(180f)] private float singleChoiceWidth = 390f;
    [SerializeField] private Vector2 singleChoiceOffset = new Vector2(-15f, 0f);
    [SerializeField, Min(180f)] private float multipleChoiceWidth = 480f;

    private RectTransform choiceViewport;
    private RectTransform choiceContent;
    private ScrollRect choiceScroll;
    private RectMask2D choiceMask;
    private Canvas choiceCanvas;
    private Vector2 lastChoiceViewportSize;
    private float lastChoiceScale;
    private bool usingDay5ChoicePlacement;
    private CanvasGroup choiceVisibility;
    private bool dialogueHidden;
    private string choiceDialogueContext;
    private string activeDialogueContext;

    private sealed class DialoguePlayback
    {
        public DialogueLine line;
        public string context;
        public TaskCompletionSource<bool> completion;
        public Coroutine coroutine;
        public bool suspended;
        public bool typingComplete;
        public bool waitingForClick;
        public int visibleCharacters;
    }

    private DialoguePlayback activePlayback;
    private readonly Dictionary<string, DialoguePlayback> suspendedPlaybacks =
        new Dictionary<string, DialoguePlayback>(StringComparer.Ordinal);

    private Button visibilityButton;
    private TMP_Text visibilityLabel;
    private int lastAdvanceFrame = -1;

    public bool HasPendingDialogue =>
        activePlayback != null ||
        suspendedPlaybacks.Count > 0 ||
        activeBubbles.Count > 0;

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

    private NullProgrammer.Audio.DialogueBlipPlayer dialogueBlips;
    private NullProgrammer.Audio.DialogueVoiceoverPlayer dialogueVoiceover;

    private DialoguePanelPresentation panelPresentation;
    private List<GameObject> activeBubbles = new List<GameObject>();

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
        activeDialogueContext = GetCurrentDialogueContext();
        HideDialogueUI();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    //wrapper to allow old code expecting the old function
    public IEnumerator PlayDialogueLine(DialogueLine line)
    {
        // Use the same playback path as narrative steps, once per line.
        Task playback = DisplayLineAndWaitAsync(line);
        while (!playback.IsCompleted) yield return null;
    }
    public void DisplayLine(DialogueLine line)
    {
        _ = DisplayLineAndWaitAsync(line);
    }

    public Task DisplayLineAndWaitAsync(DialogueLine line)
    {
        return DisplayLineAndWaitAsync(line, GetCurrentDialogueContext());
    }

    /// <summary>
    /// Plays a dialogue line in a specific scene/monitor context.
    /// Calls that do not provide a context still use the currently active scene.
    /// This lets center narrative remain paused while dialogue intentionally
    /// triggered on Left/Right plays normally.
    /// </summary>
    public Task DisplayLineAndWaitAsync(DialogueLine line, string dialogueContext)
    {
        if (line == null) return Task.CompletedTask;

        string context = string.IsNullOrWhiteSpace(dialogueContext)
            ? GetCurrentDialogueContext()
            : dialogueContext;
        string activeContext = GetCurrentDialogueContext();

        var playback = new DialoguePlayback
        {
            line = line,
            context = context,
            completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously),
            suspended = false,
            typingComplete = false,
            waitingForClick = false,
            visibleCharacters = 0
        };

        // If this dialogue belongs to another monitor, queue it for that context
        // without disturbing dialogue that is legitimately playing here.
        if (!string.Equals(context, activeContext, StringComparison.Ordinal))
        {
            if (suspendedPlaybacks.TryGetValue(context, out DialoguePlayback stale))
                StopPlayback(stale, true);

            playback.suspended = true;
            suspendedPlaybacks[context] = playback;
            return playback.completion.Task;
        }

        SwitchDialogueContext(context);

        if (activePlayback != null)
            StopPlayback(activePlayback, true);

        if (suspendedPlaybacks.TryGetValue(context, out DialoguePlayback existing))
            StopPlayback(existing, true);

        activePlayback = playback;
        activeDialogueContext = context;
        playback.coroutine = StartCoroutine(PlayDialogueLineRoutine(playback));
        return playback.completion.Task;
    }

    public void OnDialogueClicked()
    {
        DialoguePlayback playback = activePlayback;
        if (dialogueHidden ||
            NullProgrammer.Audio.AudioSettingsPanel.IsOpen ||
            NullProgrammer.Showcase.ShowcasePanel.IsOpen ||
            playback == null ||
            playback.suspended ||
            lastAdvanceFrame == Time.frameCount) return;

        lastAdvanceFrame = Time.frameCount;

        if (!playback.typingComplete)
        {
            if (dialogueBlips != null) dialogueBlips.Stop();
            NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.DialogueReveal);
            playback.typingComplete = true;
            playback.visibleCharacters = playback.line?.text != null
                ? playback.line.text.Length
                : 0;

            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = playback.visibleCharacters;
                if (panelPresentation != null)
                    panelPresentation.FollowTyping(playback.visibleCharacters);
            }

            SyncPublicProgress(playback);
        }
        else if (playback.waitingForClick)
        {
            NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.DialogueAdvance);
            if (dialogueVoiceover != null) dialogueVoiceover.Stop();
            playback.waitingForClick = false;
        }
    }

    private IEnumerator PlayDialogueLineRoutine(DialoguePlayback playback)
    {
        DialogueLine line = playback.line;
        ShowPlayback(playback, true);

        if (dialogueBlips == null)
            dialogueBlips = GetComponent<NullProgrammer.Audio.DialogueBlipPlayer>();
        if (dialogueBlips == null)
            dialogueBlips = gameObject.AddComponent<NullProgrammer.Audio.DialogueBlipPlayer>();
        dialogueBlips.Begin(line);

        if (dialogueVoiceover == null)
            dialogueVoiceover = GetComponent<NullProgrammer.Audio.DialogueVoiceoverPlayer>();
        if (dialogueVoiceover == null)
            dialogueVoiceover = gameObject.AddComponent<NullProgrammer.Audio.DialogueVoiceoverPlayer>();
        dialogueVoiceover.Begin(line, speechAudioSource);
        dialogueVoiceover.SetHidden(dialogueHidden || playback.suspended);

        int totalChars = line.text != null ? line.text.Length : 0;
        float typeSpeed = 0.02f;

        for (int i = 0; i <= totalChars; i++)
        {
            while (playback.suspended || dialogueHidden ||
                   NullProgrammer.Showcase.ShowcasePanel.IsOpen)
            {
                yield return null;
            }

            if (playback.typingComplete)
                break;

            playback.visibleCharacters = i;

            if (activePlayback == playback && dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = i;
                if (panelPresentation != null)
                    panelPresentation.FollowTyping(i);

                if (i > 0 && i <= dialogueText.textInfo.characterCount)
                {
                    bool videoHasAudio =
                        activeVideoPlayer != null &&
                        activeVideoPlayer.isPlaying &&
                        activeVideoPlayer.audioOutputMode != VideoAudioOutputMode.None &&
                        activeVideoPlayer.audioTrackCount > 0;

                    dialogueBlips.RevealCharacter(
                        dialogueText.textInfo.characterInfo[i - 1].character,
                        isActiveAndEnabled &&
                        dialoguePanel != null &&
                        dialoguePanel.activeInHierarchy &&
                        !videoHasAudio);
                }
            }

            if (activePlayback == playback)
                SyncPublicProgress(playback);

            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        if (dialogueBlips != null && activePlayback == playback)
            dialogueBlips.Stop();

        playback.visibleCharacters = totalChars;
        playback.typingComplete = true;

        if (activePlayback == playback)
        {
            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = totalChars;
            SyncPublicProgress(playback);
        }

        playback.waitingForClick = true;
        while (playback.waitingForClick)
        {
            while (playback.suspended || dialogueHidden ||
                   NullProgrammer.Showcase.ShowcasePanel.IsOpen)
            {
                yield return null;
            }
            yield return null;
        }

        if (activePlayback == playback)
        {
            HideDialoguePanel();
            activePlayback = null;
            ApplyDialogueVisibility();
        }

        playback.coroutine = null;
        playback.completion.TrySetResult(true);
    }

    private void ShowPlayback(DialoguePlayback playback, bool resetText)
    {
        if (playback == null || playback.line == null)
            return;

        DialogueLine line = playback.line;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerText != null) speakerText.text = line.speakerName;

        SetPersonalityStyle(line.personality);

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
            if (resetText)
                dialogueText.maxVisibleCharacters = playback.visibleCharacters;
            dialogueText.ForceMeshUpdate();
        }

        if (panelPresentation != null) panelPresentation.Show(line);

        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = playback.visibleCharacters;

        SyncPublicProgress(playback);
        ApplyDialogueVisibility();
    }

    public void HideDialoguePanel()
    {
        if (dialogueBlips != null) dialogueBlips.Stop();
        if (dialogueVoiceover != null) dialogueVoiceover.Stop();
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
        activeDialogueState.currentText = text;
        activeDialogueState.visibleCharacterCount = visibleChars;
        activeDialogueState.isComplete = complete;

        if (activePlayback != null)
        {
            activePlayback.visibleCharacters = visibleChars;
            activePlayback.typingComplete = complete;
        }
    }

    private void SyncPublicProgress(DialoguePlayback playback)
    {
        if (playback == null || playback.line == null)
            return;

        UpdateProgress(
            playback.line.text ?? string.Empty,
            playback.visibleCharacters,
            playback.typingComplete);
    }

    public Task<int> PresentChoices(DialogueChoiceOption[] options)
    {
        return PresentChoices(options, GetCurrentDialogueContext());
    }

    public async Task<int> PresentChoices(DialogueChoiceOption[] options, string dialogueContext)
    {
        Debug.Log($"[DialogueManager] PresentChoices called with {options?.Length ?? 0} options.");

        if (options == null || options.Length == 0)
            throw new ArgumentException("At least one dialogue choice is required.", nameof(options));
        if (choiceBubblePrefab == null)
            throw new InvalidOperationException("Assign the Thought Bubble prefab on the Dialogue Canvas.");

        choiceDialogueContext = string.IsNullOrWhiteSpace(dialogueContext)
            ? GetCurrentDialogueContext()
            : dialogueContext;
        ClearChoices(false);
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
                    NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.Choice);
                    choiceTask.TrySetResult(selectedIndex);
                });
            }

            Canvas.ForceUpdateCanvases();
            LayoutChoices(true);
            ApplyDialogueVisibility();

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
            typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(RectMask2D),
            typeof(ScrollRect), typeof(CanvasGroup));
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
        choiceVisibility = viewport.GetComponent<CanvasGroup>();
        SetGroupVisibility(
            choiceVisibility,
            !dialogueHidden &&
            string.Equals(
                choiceDialogueContext,
                GetCurrentDialogueContext(),
                StringComparison.Ordinal));
        var overlayCanvas = viewport.GetComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1500;
        var hitArea = viewport.GetComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = false; // Transparent viewport must not intercept clicks outside bubbles.

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
            (i, width, fontSize) => activeBubbles[i].GetComponent<DialogueBubble>().MeasureHeight(width, fontSize),
            singleChoiceWidth, multipleChoiceWidth, singleChoiceOffset);
        choiceContent.sizeDelta = new Vector2(size.x, plan.contentHeight);
        choiceContent.anchoredPosition = Vector2.zero;
        choiceScroll.StopMovement();
        choiceScroll.vertical = !usingDay5ChoicePlacement && plan.contentHeight > size.y + 0.5f;
        choiceScroll.enabled = choiceScroll.vertical;
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
        string context = GetCurrentDialogueContext();
        if (!string.Equals(activeDialogueContext, context, StringComparison.Ordinal))
            SwitchDialogueContext(context);

        RefreshVisibilityButton();
        if (activeBubbles.Count == 0 || choiceViewport == null) return;
        if ((choiceViewport.rect.size - lastChoiceViewportSize).sqrMagnitude > 0.01f ||
            !Mathf.Approximately(choiceCanvas.rootCanvas.scaleFactor, lastChoiceScale))
            LayoutChoices(false);
    }

    private void Update()
    {
        if (dialogueHidden || activePlayback == null) return;
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool enterPressed = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;

            bool clickedOnPanel = false;
            // Changed from wasPressedThisFrame to wasReleasedThisFrame so clicks register on release
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    Vector2 mousePos = Mouse.current.position.ReadValue();
                    if (!IsPointerOverVisibilityButton(mousePos) &&
                        !IsPointerOverSkipTutorialButton(mousePos) &&
                        RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos, null))
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

    private void ClearChoices(bool clearContext = true)
    {
        foreach (var bubble in activeBubbles)
        {
            if (bubble == null) continue;
            bubble.SetActive(false);
            Destroy(bubble);
        }
        activeBubbles.Clear();
        if (choiceViewport != null) choiceViewport.gameObject.SetActive(false);
        if (clearContext) choiceDialogueContext = null;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (dialogueBlips != null) dialogueBlips.Stop();
        if (dialogueVoiceover != null) dialogueVoiceover.Stop();
    }

    private void StopActiveLine()
    {
        if (activePlayback != null)
            StopPlayback(activePlayback, true);
    }

    private void StopPlayback(DialoguePlayback playback, bool cancelCompletion)
    {
        if (playback == null)
            return;

        if (playback.coroutine != null)
        {
            StopCoroutine(playback.coroutine);
            playback.coroutine = null;
        }

        if (activePlayback == playback)
            activePlayback = null;

        if (!string.IsNullOrEmpty(playback.context) &&
            suspendedPlaybacks.TryGetValue(playback.context, out DialoguePlayback stored) &&
            stored == playback)
        {
            suspendedPlaybacks.Remove(playback.context);
        }

        playback.waitingForClick = false;
        playback.suspended = false;

        if (cancelCompletion)
            playback.completion?.TrySetCanceled();
    }

    private void StopAllPlaybackSessions()
    {
        if (activePlayback != null)
            StopPlayback(activePlayback, true);

        DialoguePlayback[] suspended =
            new List<DialoguePlayback>(suspendedPlaybacks.Values).ToArray();

        for (int i = 0; i < suspended.Length; i++)
            StopPlayback(suspended[i], true);

        suspendedPlaybacks.Clear();
    }

    public void HideDialogueUI()
    {
        StopAllPlaybackSessions();
        ClearChoices();
        activeDialogueContext = GetCurrentDialogueContext();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (internalMonologueOverlay != null) internalMonologueOverlay.SetActive(false);
        if (dialogueHidden) SetDialogueHidden(false);

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

    public static bool BlocksMonitorClick()
    {
        if (Instance == null) return false;
        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        return Instance.IsPointerOverDialogueUI(mousePosition);
    }

    public bool IsPointerOverDialogueUI(Vector2 mousePosition)
    {
        if (IsPointerOverVisibilityButton(mousePosition)) return true;
        if (IsPointerOverSkipTutorialButton(mousePosition)) return true;
        if (dialogueHidden) return false;
        if (!HasCurrentContextDialogue()) return false;

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
            // Block only clicks actually over a thought bubble, not over the
            // enormous transparent viewport that contains the bubbles.
            foreach (var bubbleObject in activeBubbles)
            {
                if (bubbleObject == null || !bubbleObject.activeInHierarchy) continue;
                var bubbleRect = bubbleObject.GetComponent<RectTransform>();
                if (bubbleRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(bubbleRect, mousePosition, null))
                    return true;
            }
        }

        return false;
    }

    private bool IsPointerOverVisibilityButton(Vector2 mousePosition)
    {
        return visibilityButton != null && visibilityButton.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)visibilityButton.transform, mousePosition, null);
    }

    private bool IsPointerOverSkipTutorialButton(Vector2 mousePosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
                continue;

            Button button = result.gameObject.GetComponentInParent<Button>();
            if (button != null &&
                button.gameObject.activeInHierarchy &&
                button.gameObject.name == "SkipButton")
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshVisibilityButton()
    {
        string activeSceneName = GetCurrentDialogueContext();
        bool showButton =
            HasCurrentContextDialogue() &&
            SceneSwitcher.IsMonitorScene(activeSceneName);

        if (!showButton && dialogueHidden && !HasPendingDialogue)
            SetDialogueHidden(false);

        if (showButton && visibilityButton == null)
            CreateVisibilityButton();

        if (visibilityButton != null &&
            visibilityButton.gameObject.activeSelf != showButton)
        {
            visibilityButton.gameObject.SetActive(showButton);
        }
    }

    private void CreateVisibilityButton()
    {
        var control = new GameObject("Toggle Dialogue", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(Image), typeof(Button));
        control.layer = gameObject.layer;
        var rect = (RectTransform)control.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -20f);
        rect.sizeDelta = new Vector2(248f, 48f);

        var canvas = control.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1600;
        var background = control.GetComponent<Image>();
        background.color = new Color(0.10f, 0.17f, 0.24f, 0.98f);
        visibilityButton = control.GetComponent<Button>();
        visibilityButton.targetGraphic = background;
        visibilityButton.navigation = new Navigation { mode = Navigation.Mode.None };
        visibilityButton.onClick.AddListener(() => SetDialogueHidden(!dialogueHidden));

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.layer = control.layer;
        var labelRect = (RectTransform)label.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        visibilityLabel = label.GetComponent<TMP_Text>();
        if (speakerText != null && speakerText.font != null) visibilityLabel.font = speakerText.font;
        visibilityLabel.fontSize = 25f;
        visibilityLabel.alignment = TextAlignmentOptions.Center;
        visibilityLabel.raycastTarget = false;
        visibilityLabel.text = "Hide Dialogue";
    }

    private string GetCurrentDialogueContext()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!newScene.IsValid())
            return;

        // Monitor-to-monitor changes preserve independent dialogue sessions.
        if (SceneSwitcher.IsMonitorScene(newScene.name))
        {
            SwitchDialogueContext(newScene.name);
            return;
        }

        // Leaving the three-monitor desktop is a real scene transition. Do not
        // carry paused monitor dialogue into night/weekend/other authored scenes.
        StopAllPlaybackSessions();
        ClearChoices();
        activeDialogueContext = newScene.name;
        HideDialoguePanel();
    }

    private void SwitchDialogueContext(string newContext)
    {
        if (string.IsNullOrEmpty(newContext))
            return;

        if (string.Equals(activeDialogueContext, newContext, StringComparison.Ordinal))
        {
            ApplyDialogueVisibility();
            return;
        }

        if (activePlayback != null &&
            !string.Equals(activePlayback.context, newContext, StringComparison.Ordinal))
        {
            SuspendActivePlayback();
        }

        activeDialogueContext = newContext;

        if (suspendedPlaybacks.TryGetValue(newContext, out DialoguePlayback resume))
        {
            suspendedPlaybacks.Remove(newContext);
            activePlayback = resume;
            resume.suspended = false;

            // A line explicitly assigned to this monitor may have been queued
            // while another monitor was active and therefore never started.
            if (resume.coroutine == null)
            {
                resume.coroutine = StartCoroutine(PlayDialogueLineRoutine(resume));
            }
            else
            {
                // A line that was already visible before the monitor switch
                // resumes fully revealed (SuspendActivePlayback marks it complete).
                ShowPlayback(resume, false);

                if (!resume.typingComplete && dialogueVoiceover != null)
                {
                    dialogueVoiceover.Begin(resume.line, speechAudioSource);
                    dialogueVoiceover.SetHidden(dialogueHidden);
                }

                if (!resume.typingComplete && dialogueBlips != null)
                    dialogueBlips.Begin(resume.line);
            }
        }

        ApplyDialogueVisibility();
    }

    private void SuspendActivePlayback()
    {
        DialoguePlayback playback = activePlayback;
        if (playback == null)
            return;

        // Once a visible line has been left behind on another monitor, resume it
        // fully revealed rather than restarting/resuming the typewriter mid-word.
        if (!playback.typingComplete)
        {
            playback.visibleCharacters = playback.line?.text != null
                ? playback.line.text.Length
                : 0;
            playback.typingComplete = true;
        }

        playback.suspended = true;
        suspendedPlaybacks[playback.context ?? string.Empty] = playback;
        activePlayback = null;

        if (dialogueBlips != null) dialogueBlips.Stop();
        if (dialogueVoiceover != null) dialogueVoiceover.Stop();

        ApplyDialogueVisibility();
    }

    private bool HasCurrentContextDialogue()
    {
        string context = GetCurrentDialogueContext();
        bool hasLine =
            activePlayback != null &&
            string.Equals(activePlayback.context, context, StringComparison.Ordinal);

        bool hasChoices =
            activeBubbles.Count > 0 &&
            !string.IsNullOrEmpty(choiceDialogueContext) &&
            string.Equals(choiceDialogueContext, context, StringComparison.Ordinal);

        return hasLine || hasChoices;
    }

    /// <summary>
    /// Compatibility bridge for the earlier monitor-persistence patch.
    /// Only center-origin dialogue is suspended; dialogue that originated on a
    /// side monitor is intentionally left alone. Active-scene changes perform
    /// the actual resume operation.
    /// </summary>
    public void SetMonitorSuspended(bool suspended)
    {
        if (!suspended || activePlayback == null)
            return;

        if (string.Equals(
            activePlayback.context,
            SceneSwitcher.CenterMonitorScene,
            StringComparison.Ordinal))
        {
            SuspendActivePlayback();
        }
    }

    public bool IsMonitorSuspended =>
        suspendedPlaybacks.ContainsKey(SceneSwitcher.CenterMonitorScene);

    private void SetDialogueHidden(bool hidden)
    {
        dialogueHidden = hidden;
        ApplyDialogueVisibility();

        if (visibilityLabel != null)
            visibilityLabel.text =
                dialogueHidden ? "Show Dialogue" : "Hide Dialogue";
    }

    private void ApplyDialogueVisibility()
    {
        string context = GetCurrentDialogueContext();
        bool lineVisible =
            !dialogueHidden &&
            activePlayback != null &&
            !activePlayback.suspended &&
            string.Equals(activePlayback.context, context, StringComparison.Ordinal);

        bool choicesVisible =
            !dialogueHidden &&
            activeBubbles.Count > 0 &&
            string.Equals(choiceDialogueContext, context, StringComparison.Ordinal);

        if (!lineVisible && dialogueBlips != null)
            dialogueBlips.Stop();

        if (dialogueVoiceover != null)
            dialogueVoiceover.SetHidden(!lineVisible);

        if (dialoguePanel != null)
            SetGroupVisibility(EnsureCanvasGroup(dialoguePanel), lineVisible);

        if (internalMonologueOverlay != null)
            SetGroupVisibility(EnsureCanvasGroup(internalMonologueOverlay), lineVisible);

        SetGroupVisibility(choiceVisibility, choicesVisible);
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static void SetGroupVisibility(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
    /// <summary>
    /// Persistent bridge method wired up to the Skip Button in the Inspector.
    /// Safely forwards the skip command to the active scene's ZoomScreenShareController.
    /// </summary>
    public void SkipActiveTutorial()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.SkipTutorial();
            Debug.Log("[DialogueManager] Skip button clicked: Tutorial skipped successfully.");
        }
        else
        {
            Debug.LogWarning("[DialogueManager] Skip button clicked, but no active ZoomScreenShareController found in this scene.");
        }
    }
}
