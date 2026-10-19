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

    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;
    private bool isScreenSharingActive = false;

    [Header("Steve Expressions")]
    [SerializeField] private SpriteRenderer zoomAvatarImage; // The UI Image component displaying Steve's window
    [SerializeField] private List<SteveSpriteMapping> expressionMappings = new List<SteveSpriteMapping>();
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

    public void TriggerIncomingCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(true);
    }

    public void AcceptZoomCall()
    {
        if (incomingCallPopup != null) incomingCallPopup.SetActive(false);
        if (zoomPanel != null) zoomPanel.SetActive(true);

        GameplayEvents.TriggerZoomCallStarted();

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

    public void StartScreenShare()
    {
        StartCoroutine(ScreenShareRoutine());
    }

    private IEnumerator ScreenShareRoutine()
    {
        if (screensharePopup != null) screensharePopup.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (screensharePopup != null) screensharePopup.SetActive(false);
        if (screenShareDisplay != null) screenShareDisplay.SetActive(true);

        isScreenSharingActive = true;
        currentCueIndex = 0;

        if (screenShareVideoPlayer != null)
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
        isScreenSharingActive = false;

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.enabled)
        {
            screenShareVideoPlayer.Stop();
        }

        if (screenShareDisplay != null) screenShareDisplay.SetActive(false);
        if (screensharePopup != null) screensharePopup.SetActive(false);

        // FIRE EVENT TO UNBLOCK NARRATIVE WAITING FOR SCREEN SHARE END
        GameplayEvents.TriggerScreenShareEnded();
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

        if (screenShareVideoPlayer != null && screenShareVideoPlayer.enabled && screenShareVideoPlayer.gameObject.activeInHierarchy)
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
