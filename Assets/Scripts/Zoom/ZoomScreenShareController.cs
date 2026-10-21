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

    [Header("Day 5 Screen Share")]
    [SerializeField] private Day5CodeSnippetDisplay day5CodeSnippetDisplay;

    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;
    private bool isScreenSharingActive = false;
    private bool useVideoForCurrentShare = true;

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
        if (!isScreenSharingActive || !useVideoForCurrentShare || isPausedForDialogue || currentCueIndex >= dialogueCues.Count)
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
    }

    public void AcceptZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(true);

        GameplayEvents.TriggerZoomCallStarted();

        int currentDay = GameManager.EnsureInstance().CurrentDay;

        // Days 2 and 5 own their greeting in DaySequenceData.
        if (currentDay == 2 || currentDay == 5)
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
    }

    // ------------------------------------------------------------------------
    // 2. SCREEN SHARE FLOW FUNCTIONS
    // ------------------------------------------------------------------------

    public void StartScreenShare()
    {
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
        isPausedForDialogue = false;

        if (screenShareVideoPlayer != null)
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

        yield return StartCoroutine(DialogueManager.Instance.PlayDialogueLine(cue.dialogue));

        if (screenShareVideoPlayer != null && useVideoForCurrentShare)
        {
            screenShareVideoPlayer.Play();
        }

        isPausedForDialogue = false;
    }
}
