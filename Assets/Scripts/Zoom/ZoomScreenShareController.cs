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
    [SerializeField] private GameObject incomingCallPopup;   // Popup with "Accept Call" button
    [SerializeField] private GameObject zoomPanel;            // Steve's avatar/Zoom window frame
    [SerializeField] private GameObject screensharePopup;     // "Steve is screen sharing..." notification
    [SerializeField] private GameObject screenShareDisplay;   // Game Object containing the VideoPlayer / Quad / SpriteRenderer

    [Header("Video Settings")]
    [SerializeField] private VideoPlayer screenShareVideoPlayer;

    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;
    private bool isScreenSharingActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Hide all Zoom UI elements by default on startup
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);
    }

    private void Update()
    {
        if (!isScreenSharingActive || isPausedForDialogue || currentCueIndex >= dialogueCues.Count) return;

        // Monitor video playback time during active screenshare
        if (screenShareVideoPlayer != null && screenShareVideoPlayer.time >= dialogueCues[currentCueIndex].triggerTimestampSeconds)
        {
            VideoDialogueCue currentCue = dialogueCues[currentCueIndex];
            currentCueIndex++;

            if (currentCue.pauseVideo)
            {
                // Pause video and wait for dialogue line to complete
                StartCoroutine(ExecutePauseAndDialogue(currentCue));
            }
            else
            {
                // Play dialogue asynchronously without pausing video playback
                StartCoroutine(DialogueManager.Instance.PlayDialogueLine(currentCue.dialogue));
            }
        }
    }

    // ------------------------------------------------------------------------
    // 1. ZOOM CALL FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    /// <summary>
    /// Displays the incoming call popup ("Steve is starting a Zoom session").
    /// </summary>
    public void TriggerIncomingCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(true);
    }

    /// <summary>
    /// Hook this function to the "Pick Up Call" button OnClick event.
    /// </summary>
    public void AcceptZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(true);

        // FIRE EVENT TO UNBLOCK NarrativeWaitHelpers
        GameplayEvents.TriggerZoomCallStarted();

        // Trigger opening dialogue from Steve
        DialogueLine initialGreeting = new DialogueLine
        {
            speakerName = "Steve",
            personality = HeadPersonality.None,
            text = "Hey! Thanks for jumping on the call. Let's get your C++ project environment configured.",
            displayDuration = 3.5f
        };

        StartCoroutine(DialogueManager.Instance.PlayDialogueLine(initialGreeting));
    }

    // ------------------------------------------------------------------------
    // 2. SCREEN SHARE FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    /// <summary>
    /// Public function to trigger Steve's screen sharing sequence.
    /// </summary>
    public void StartScreenShare()
    {
        StartCoroutine(ScreenShareRoutine());
    }

    private IEnumerator ScreenShareRoutine()
    {
        // Show "Steve is sharing his screen..." popup
        if (screensharePopup != null) screensharePopup.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(true);

        isScreenSharingActive = true;
        currentCueIndex = 0;

        if (screenShareVideoPlayer != null)
        {
            // Bind video end callback without altering your play order
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

    /// <summary>
    /// Public function to stop and hide Steve's screen share.
    /// </summary>
    public void EndScreenShare()
    {
        isScreenSharingActive = false;

        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Stop();
        }

        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);
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

        yield return StartCoroutine(DialogueManager.Instance.PlayDialogueLine(cue.dialogue));

        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Play();
        }

        isPausedForDialogue = false;
    }
}
