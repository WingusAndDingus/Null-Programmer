using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public enum SteveExpression
{
    DefaultSteve,
    ImpressedSteve,
    SuspiciousSteve,
    HelloSteve,
    ByeSteve,
    SteveWatchingOk,
    SteveWatchingSus
}
[System.Serializable]
public struct SteveSpriteMapping
{
    public SteveExpression expression;
    public Sprite expressionSprite;
}
[System.Serializable]
public class VideoDialogueCue
{
    [Tooltip("Timestamp in seconds when this dialogue triggers")]
    public double triggerTimestampSeconds;

    [Tooltip("If checked, the video freezes while dialogue plays. If unchecked, the video continues playing.")]
    public bool pauseVideo = true;

    public DialogueLine dialogue;
}

public class ZoomScreenShareController : MonoBehaviour
{
    public static ZoomScreenShareController Instance;

    [Header("UI & Window Panels")]
    [SerializeField] private GameObject incomingCallPopup;   // Popup with "Accept Call" button
    [SerializeField] private GameObject zoomPanel;           // Steve's avatar/Zoom window frame
    [SerializeField] private GameObject screensharePopup;    // "Steve is screen sharing..." notification
    [SerializeField] private GameObject screenShareDisplay;  // Game Object containing the VideoPlayer / Quad / SpriteRenderer

    [Header("Video Settings")]
    [SerializeField] private VideoPlayer screenShareVideoPlayer;

    [Header("Day 5 Screen Share")]
    [SerializeField] private Day5CodeSnippetDisplay day5CodeSnippetDisplay;

    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;
    private bool isScreenSharingActive = false;
    private bool useVideoForCurrentShare = true;
    private bool shareStarting;
    private bool restoring;
    private SteveExpression currentExpression;
    private bool hasExpression;

    [Header("Steve Expressions")]
    [SerializeField] private SpriteRenderer zoomAvatarImage; // The UI Image component displaying Steve's window
    [SerializeField] private List<SteveSpriteMapping> expressionMappings = new List<SteveSpriteMapping>();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.ZoomState != null && gm.ZoomState.day == gm.CurrentDay)
            StartCoroutine(RestoreZoomSession(gm.ZoomState));
    }

    private void LateUpdate()
    {
        if (!restoring) SaveZoomSession();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        // Latest snapshot was saved while objects were still active in LateUpdate.
        // Do not overwrite it with deactivated scene objects during destruction.
        if (screenShareVideoPlayer != null)
            screenShareVideoPlayer.loopPointReached -= OnVideoEnd;
        Instance = null;
    }

    private void SaveZoomSession()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        // During a video restore, time can temporarily be zero; never overwrite the snapshot then.
        gm.SaveZoomState(new GameManager.ZoomShareState
        {
            day = gm.CurrentDay,
            callVisible = zoomPanel != null && zoomPanel.activeSelf,
            incomingCallVisible = incomingCallPopup != null && incomingCallPopup.activeSelf,
            shareActive = isScreenSharingActive,
            shareStarting = shareStarting,
            usesVideo = useVideoForCurrentShare,
            videoTime = screenShareVideoPlayer != null && useVideoForCurrentShare && isScreenSharingActive
                ? screenShareVideoPlayer.time : 0,
            videoWasPlaying = screenShareVideoPlayer != null && screenShareVideoPlayer.isPlaying,
            videoPausedForDialogue = isPausedForDialogue,
            nextVideoCueIndex = currentCueIndex,
            expression = currentExpression,
            hasExpression = hasExpression
        });
    }

    private IEnumerator RestoreZoomSession(GameManager.ZoomShareState state)
    {
        // Wait a frame so the scene's UI components can initialize.
        restoring = true;
        yield return null;
        if (zoomPanel != null) zoomPanel.SetActive(state.callVisible);
        if (incomingCallPopup != null) incomingCallPopup.SetActive(state.incomingCallVisible);
        if (state.hasExpression) SetSteveExpression(state.expression);
        if (state.shareStarting && !state.shareActive)
        {
            useVideoForCurrentShare = state.usesVideo;
            StartCoroutine(ScreenShareRoutine());
        }
        else if (state.shareActive)
        {
            isScreenSharingActive = true;
            useVideoForCurrentShare = state.usesVideo;
            currentCueIndex = state.nextVideoCueIndex;
            if (screenShareDisplay != null) screenShareDisplay.SetActive(true);
            if (useVideoForCurrentShare)
            {
                if (screenShareVideoPlayer == null)
                    Debug.LogError("[Zoom] Cannot restore Day 1: VideoPlayer is not assigned.");
                else
                {
                    screenShareVideoPlayer.enabled = true;
                    screenShareVideoPlayer.loopPointReached -= OnVideoEnd;
                    screenShareVideoPlayer.loopPointReached += OnVideoEnd;
                    screenShareVideoPlayer.Prepare();
                    while (!screenShareVideoPlayer.isPrepared) yield return null;
                    screenShareVideoPlayer.time = state.videoTime;
                    // Unity seeks asynchronously; let the video player apply the requested time.
                    yield return null;
                    screenShareVideoPlayer.Play();
                    if (!state.videoWasPlaying || state.videoPausedForDialogue)
                        screenShareVideoPlayer.Pause();
                    // Don't re-run already fired cues. A cue dialogue coroutine cannot
                    // safely be resurrected without coordinating with DialogueManager.
                    isPausedForDialogue = state.videoPausedForDialogue;
                }
            }
            else
            {
                if (screenShareVideoPlayer != null) screenShareVideoPlayer.enabled = false;
                if (day5CodeSnippetDisplay != null)
                    day5CodeSnippetDisplay.ShowDay5ScreenShare();
            }
        }
        restoring = false;
        SaveZoomSession();
    }

    private void Update()
    {
        if (restoring || !isScreenSharingActive || !useVideoForCurrentShare || isPausedForDialogue || currentCueIndex >= dialogueCues.Count)
            return;

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.time >= dialogueCues[currentCueIndex].triggerTimestampSeconds)
        {
            VideoDialogueCue currentCue = dialogueCues[currentCueIndex];
            currentCueIndex++;

            if (currentCue.pauseVideo)
            {
                StartCoroutine(ExecutePauseAndDialogue(currentCue));
            }
            else
            {
                StartCoroutine(DialogueManager.Instance.PlayDialogueLine(currentCue.dialogue));
            }
        }
    }

    // ------------------------------------------------------------------------
    // 1. ZOOM CALL FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    public void TriggerIncomingCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(true);
        SaveZoomSession();
    }

    public void AcceptZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(true);

        GameplayEvents.TriggerZoomCallStarted();
        SaveZoomSession();

        int currentDay = GameManager.EnsureInstance().CurrentDay;

        // Days 2 and 5 own their greeting in DaySequenceData.
        if (currentDay == 2 || currentDay == 3 || currentDay == 5)
        {
            return;
        }

        DialogueLine initialGreeting = new DialogueLine
        {
            speakerName = "Steve",
            personality = HeadPersonality.None,
            text = "Hey! Thanks for jumping on the call. Let's get your C++ project environment configured.",
            displayDuration = 3.5f
        };

        StartCoroutine(DialogueManager.Instance.PlayDialogueLine(initialGreeting));
    }

    public void EndZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(false);
        GameplayEvents.TriggerZoomCallEnded();
        SaveZoomSession();
    }

    // ------------------------------------------------------------------------
    // 2. SCREEN SHARE FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    public void StartScreenShare()
    {
        if (shareStarting || isScreenSharingActive) return;
        shareStarting = true;
        int currentDay = GameManager.EnsureInstance().CurrentDay;

        useVideoForCurrentShare = currentDay != 5;

        // Stop the old video before activating the shared-screen mesh.
        // This prevents Play On Awake / OnEnable behavior from restarting
        // the Day 1 SFML video during Day 5.
        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Stop();

            if (!useVideoForCurrentShare)
            {
                screenShareVideoPlayer.enabled = false;
            }
            else
            {
                screenShareVideoPlayer.enabled = true;
            }
        }

        SaveZoomSession();
        StartCoroutine(ScreenShareRoutine());
    }

    private IEnumerator ScreenShareRoutine()
    {
        if (screensharePopup != null)
        {
            screensharePopup.SetActive(true);
        }

        yield return new WaitForSeconds(2.0f);

        if (screensharePopup != null)
        {
            screensharePopup.SetActive(false);
        }

        // Keep the existing screen-share mesh/window visible.
        if (screenShareDisplay != null)
        {
            screenShareDisplay.SetActive(true);
        }

        isScreenSharingActive = true;
        shareStarting = false;
        currentCueIndex = 0;

        if (useVideoForCurrentShare)
        {
            // Normal Day 1 / other-day video behavior.
            if (screenShareVideoPlayer != null)
            {
                screenShareVideoPlayer.enabled = true;

                screenShareVideoPlayer.loopPointReached -= OnVideoEnd;
                screenShareVideoPlayer.loopPointReached += OnVideoEnd;

                screenShareVideoPlayer.Play();
            }
        }
        else
        {
            // Day 5: show our separate World Space IDE Canvas.
            if (day5CodeSnippetDisplay != null)
            {
                day5CodeSnippetDisplay.ShowDay5ScreenShare();
            }
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoEnd;
        EndScreenShare();
    }

    public void EndScreenShare()
    {
        bool wasActive =
            isScreenSharingActive ||
            (screenShareDisplay != null && screenShareDisplay.activeSelf) ||
            (screensharePopup != null && screensharePopup.activeSelf);

        isScreenSharingActive = false;
        shareStarting = false;
        isPausedForDialogue = false;

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.enabled)
        {
            screenShareVideoPlayer.loopPointReached -= OnVideoEnd;
            screenShareVideoPlayer.Stop();

            // Restore the component so earlier/later days can use it normally.
            screenShareVideoPlayer.enabled = true;
        }

        if (day5CodeSnippetDisplay != null)
        {
            day5CodeSnippetDisplay.HideDay5ScreenShare();
        }

        if (screenShareDisplay != null)
        {
            screenShareDisplay.SetActive(false);
        }

        if (screensharePopup != null)
        {
            screensharePopup.SetActive(false);
        }

        SaveZoomSession();
        if (wasActive)
        {
            GameplayEvents.TriggerScreenShareEnded();
        }
    }

    // ------------------------------------------------------------------------
    // 3. DIALOGUE CUE EXECUTION
    // ------------------------------------------------------------------------

    private IEnumerator ExecutePauseAndDialogue(VideoDialogueCue cue)
    {
        isPausedForDialogue = true;

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.enabled)
        {
            screenShareVideoPlayer.Pause();
        }

        yield return StartCoroutine(DialogueManager.Instance.PlayDialogueLine(cue.dialogue));

        if (screenShareVideoPlayer != null && useVideoForCurrentShare)
        {
            screenShareVideoPlayer.Play();
        }

        isPausedForDialogue = false;
    }
    /// <summary>
    /// Changes Steve's active Zoom window image based on the selected expression enum.
    /// </summary>
    public void SetSteveExpression(SteveExpression expression)
    {
        currentExpression = expression;
        hasExpression = true;
        Sprite targetSprite = null;

        foreach (var mapping in expressionMappings)
        {
            if (mapping.expression == expression)
            {
                targetSprite = mapping.expressionSprite;
                break;
            }
        }

        if (zoomAvatarImage != null && targetSprite != null)
        {
            zoomAvatarImage.sprite = targetSprite;
        }
        else
        {
            Debug.LogWarning($"[ZoomScreenShareController] No sprite found for expression: {expression}");
        }
    }
}
