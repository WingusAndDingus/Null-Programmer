using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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
    [SerializeField] private GameObject incomingCallPopup;
    [SerializeField] private GameObject zoomPanel;
    [SerializeField] private GameObject screensharePopup;
    [SerializeField] private GameObject screenShareDisplay;

    [Header("Video Settings")]
    [SerializeField] private VideoPlayer screenShareVideoPlayer;

    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;
    private bool isScreenSharingActive = false;
    private bool suppressNextAcceptGreeting = false;
    private bool suppressNextScreenShareVideo = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);
    }

    private void Update()
    {
        if (!isScreenSharingActive || isPausedForDialogue || currentCueIndex >= dialogueCues.Count) return;

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.time >= dialogueCues[currentCueIndex].triggerTimestampSeconds)
        {
            VideoDialogueCue currentCue = dialogueCues[currentCueIndex];
            currentCueIndex++;

            if (currentCue.pauseVideo)
            {
                StartCoroutine(ExecutePauseAndDialogue(currentCue));
            }
            else if (DialogueManager.Instance != null)
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
    }

    /// <summary>
    /// One-shot option used by a narrative step when that day's sequence wants to
    /// provide its own first Steve line. It automatically resets after the call is accepted.
    /// </summary>
    public void SetSuppressNextAcceptGreeting(bool suppress)
    {
        suppressNextAcceptGreeting = suppress;
    }

    /// <summary>
    /// Hook this function to the "Pick Up Call" button OnClick event.
    /// </summary>
    public void AcceptZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(true);

        bool suppressGreeting = suppressNextAcceptGreeting;
        suppressNextAcceptGreeting = false;

        GameplayEvents.TriggerZoomCallStarted();

        // Preserve the existing default greeting for all days that do not opt out.
        if (!suppressGreeting && DialogueManager.Instance != null)
        {
            DialogueLine initialGreeting = new DialogueLine
            {
                speakerName = "Steve",
                personality = HeadPersonality.None,
                text = "Hey! Thanks for jumping on the call. Let's get your C++ project environment configured.",
                displayDuration = 3.5f
            };

            StartCoroutine(DialogueManager.Instance.PlayDialogueLine(initialGreeting));
        }
    }

    public void EndZoomCall()
    {
        if (isScreenSharingActive)
        {
            EndScreenShare();
        }

        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(false);

        GameplayEvents.TriggerZoomCallEnded();
    }

    // ------------------------------------------------------------------------
    // 2. SCREEN SHARE FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    public void SetSuppressNextScreenShareVideo(bool suppress)
    {
        suppressNextScreenShareVideo = suppress;
    }

    public void StartScreenShare()
    {
        bool suppressVideo = suppressNextScreenShareVideo;
        suppressNextScreenShareVideo = false;
        StartCoroutine(ScreenShareRoutine(suppressVideo));
    }

    private IEnumerator ScreenShareRoutine(bool suppressVideo)
    {
        if (screensharePopup != null) screensharePopup.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(true);

        isScreenSharingActive = true;
        currentCueIndex = 0;

        if (!suppressVideo && screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.loopPointReached -= OnVideoEnd;
            screenShareVideoPlayer.loopPointReached += OnVideoEnd;
            screenShareVideoPlayer.Play();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoEnd;
        EndScreenShare();
    }

    public void EndScreenShare()
    {
        bool wasActive = isScreenSharingActive ||
                         (screenShareDisplay != null && screenShareDisplay.activeSelf) ||
                         (screensharePopup != null && screensharePopup.activeSelf);

        isScreenSharingActive = false;

        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Stop();
        }

        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);

        // The original helper waits for this event, so make sure EndScreenShare
        // actually publishes it. Guard against duplicate end notifications.
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

        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Pause();
        }

        if (DialogueManager.Instance != null)
        {
            yield return StartCoroutine(DialogueManager.Instance.PlayDialogueLine(cue.dialogue));
        }

        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Play();
        }

        isPausedForDialogue = false;
    }
}
