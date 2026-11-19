using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The seven intended outcomes. This enum replaces the duplicate definition
/// that currently lives in Assets/Scripts/Day Scripts/GameEndingManager.cs.
/// </summary>
public enum GameEndingType
{
    ObsoleteAndDeprecated,
    ObsoleteSoftRelaunch,
    ImposterProtocol,
    PerfectionistTrap,
    UltimateMentor,
    ArchitectRedemption,
    SegmentationFaultOfSoul
}

/// <summary>
/// Plays the Day 7 review for the outcome chosen by GameManager, then opens
/// the existing GameEndScreen scene.
/// </summary>
public sealed class GameEndEvaluator : MonoBehaviour
{
    [Header("UML Asset")]
    [Tooltip("The clean/reference UML shown for Steve and for high-quality player work.")]
    public Sprite standardUmlSprite;

    [Header("End Screen")]
    [SerializeField] private string endScreenSceneName = "GameEndScreen";

    [Header("Evaluation Media")]
    [SerializeField] private FinalEvaluationMedia evaluationMedia = new FinalEvaluationMedia();

    // Retained so existing serialized Inspector data is not needlessly discarded.
    [Header("Legacy two-ending fields (not used by the seven-ending evaluator)")]
    [HideInInspector] public string successTitle = "Ending: The Perfectionist Trap";
    [HideInInspector] public Color successTitleColor = Color.white;
    [HideInInspector, TextArea(3, 10)] public string successText = "";
    [HideInInspector] public string failTitle = "Ending: Coupled and Deprecated";
    [HideInInspector] public Color failTitleColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [HideInInspector, TextArea(3, 10)] public string failText = "";

    private static GameEndEvaluator activePresentation;
    public static bool IsPresentationActive => activePresentation != null;

    private FinalEvaluationPresentation presentation;
    private GameEndingType evaluatedEnding;
    private bool triggerInProgress;
    private bool waitingForEndScreen;

    public void TriggerFinalEvaluation()
    {
        if (triggerInProgress || IsPresentationActive)
        {
            Debug.LogWarning("[GameEndEvaluator] Final evaluation was already triggered.", this);
            return;
        }

        triggerInProgress = true;
        evaluatedEnding = GameManager.EnsureInstance().DetermineEnding();
        Debug.Log("[GameEndEvaluator] Final ending decided: " + evaluatedEnding, this);

        FinalEvaluationScript script = null;
        try
        {
            script = FinalEvaluationScript.Load();
        }
        catch (ArgumentException exception)
        {
            Debug.LogException(exception, this);
        }

        FinalEvaluationScript.Route route = script?.FindRoute(evaluatedEnding);
        DialogueManager dialogue = DialogueManager.Instance;
        if (route?.beats == null || route.beats.Length == 0 || dialogue == null)
        {
            Debug.LogError("[GameEndEvaluator] Missing review dialogue or route. Opening the ending screen.", this);
            OpenEndScreen();
            return;
        }

        StartCoroutine(PlayReview(script, route, dialogue));
    }

    private IEnumerator PlayReview(FinalEvaluationScript script, FinalEvaluationScript.Route route,
        DialogueManager dialogue)
    {
        FinalEvaluationMedia media = evaluationMedia ?? new FinalEvaluationMedia();
        bool memoriesOnly = evaluatedEnding == GameEndingType.SegmentationFaultOfSoul;
        FinalEvaluationMedia.RouteMedia routeMedia = memoriesOnly ? null : media.ForEnding(evaluatedEnding);
        Sprite[] stills = memoriesOnly ? media.segmentationFaultOfSoul?.epilogueStills : routeMedia?.epilogueStills;
        int sceneIndex = 0;

        presentation = FinalEvaluationPresentation.Create(transform, media, includeCall: !memoriesOnly);
        activePresentation = this;
        if (!memoriesOnly)
        {
            NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.IncomingCall);
            while (!presentation.Joined)
                yield return null;

            presentation.ShowCall("Battleship project review", "Waiting for Bjarne to share his screen");
            yield return PlayLines(dialogue, script.opening);
        }

        foreach (FinalEvaluationScript.Beat beat in route.beats)
        {
            if (beat == null || (memoriesOnly && beat.view != "scene")) continue;

            switch (beat.view)
            {
                case "call":
                    presentation.ShowCall(beat.title, beat.detail);
                    break;
                case "project":
                    presentation.ShowDemo(beat.title, beat.detail, routeMedia?.projectVideo);
                    break;
                case "backup":
                    presentation.ShowDemo(beat.title, beat.detail, media.steveBackupVideo);
                    break;
                case "steve":
                    presentation.ShowSteveCall();
                    break;
                case "scene":
                    Sprite still = stills != null && sceneIndex < stills.Length ? stills[sceneIndex] : null;
                    presentation.ShowScene(beat.title, beat.detail, still);
                    sceneIndex++;
                    break;
                case "email":
                    presentation.ShowEmail(beat.title, beat.detail);
                    break;
            }

            yield return PlayLines(dialogue, beat.lines);
        }

        OpenEndScreen();
    }

    private static IEnumerator PlayLines(DialogueManager dialogue, FinalEvaluationScript.Line[] lines)
    {
        if (lines == null) yield break;

        foreach (FinalEvaluationScript.Line line in lines)
        {
            if (dialogue == null) yield break;
            if (line == null) continue;

            Task playback = dialogue.DisplayLineAndWaitAsync(line.ToDialogueLine());
            while (!playback.IsCompleted && dialogue != null)
                yield return null;

            if (playback.IsFaulted)
                Debug.LogException(playback.Exception.GetBaseException());
        }
    }

    private void OpenEndScreen()
    {
        Scene loadedScene = SceneManager.GetSceneByName(endScreenSceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            InjectEndingData();
            ClosePresentation();
            return;
        }

        waitingForEndScreen = true;
        SceneManager.sceneLoaded += OnEndScreenLoaded;
        if (!Application.CanStreamedLevelBeLoaded(endScreenSceneName))
        {
            Debug.LogError("[GameEndEvaluator] Add " + endScreenSceneName + " to the active build profile.", this);
            SceneManager.sceneLoaded -= OnEndScreenLoaded;
            waitingForEndScreen = false;
            triggerInProgress = false;
            ClosePresentation();
            return;
        }

        SceneManager.LoadScene(endScreenSceneName, LoadSceneMode.Additive);
    }

    private void OnEndScreenLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForEndScreen || scene.name != endScreenSceneName)
            return;

        waitingForEndScreen = false;
        SceneManager.sceneLoaded -= OnEndScreenLoaded;
        InjectEndingData();
        ClosePresentation();
    }

    private void InjectEndingData()
    {
        GameEndManager uiManager = FindAnyObjectByType<GameEndManager>();
        if (uiManager == null)
        {
            Debug.LogError("[GameEndEvaluator] GameEndManager was not found in GameEndScreen.", this);
            triggerInProgress = false;
            return;
        }

        string title;
        string body;
        Color titleColor;

        switch (evaluatedEnding)
        {
            case GameEndingType.ObsoleteAndDeprecated:
                title = "Ending: Obsolete & Deprecated";
                body = "Steve ships his version of Battleship. Your name never makes it onto the build.";
                titleColor = new Color(0.92f, 0.30f, 0.30f, 1f);
                break;

            case GameEndingType.ObsoleteSoftRelaunch:
                title = "Ending: Obsolete, with a Soft Relaunch";
                body = "The email arrives at 8:03 a.m. Your first meeting of the day disappears from the calendar a minute later.";
                titleColor = new Color(0.91f, 0.62f, 0.30f, 1f);
                break;

            case GameEndingType.ImposterProtocol:
                title = "Ending: Imposter Protocol";
                body = "Your badge still works on Monday. Steve has booked half an hour to walk you through the code.";
                titleColor = new Color(0.88f, 0.75f, 0.40f, 1f);
                break;

            case GameEndingType.PerfectionistTrap:
                title = "Ending: The Perfectionist Trap";
                body = "The build is ready to ship. At eleven, you are still at your desk.";
                titleColor = new Color(0.72f, 0.82f, 0.95f, 1f);
                break;

            case GameEndingType.UltimateMentor:
                title = "Ending: The Ultimate Mentor";
                body = "Both names go on the release. After work, Steve texts you the name of the coffee place.";
                titleColor = new Color(0.48f, 0.84f, 0.67f, 1f);
                break;

            case GameEndingType.ArchitectRedemption:
                title = "Ending: The Architect's Redemption";
                body = "The project shipped. When the next call came, your laptop was already closed.";
                titleColor = new Color(0.48f, 0.84f, 0.67f, 1f);
                break;

            case GameEndingType.SegmentationFaultOfSoul:
                title = "Secret Ending: Segmentation Fault of the Soul";
                body = "You remember enough to tell the truth. You pick up the phone.";
                titleColor = new Color(0.70f, 0.78f, 1f, 1f);
                break;

            default:
                title = "Ending: Obsolete, with a Soft Relaunch";
                body = "The evaluation is complete. Your result leaves the future uncertain, and there is still work to do to rebuild both your skills and your relationships.";
                titleColor = new Color(0.91f, 0.62f, 0.30f, 1f);
                break;
        }

        uiManager.SetupEndScreen(title, body, standardUmlSprite, titleColor,
            showUml: evaluatedEnding != GameEndingType.SegmentationFaultOfSoul);
    }

    private void ClosePresentation()
    {
        if (activePresentation == this)
            activePresentation = null;
        if (presentation == null) return;

        presentation.gameObject.SetActive(false);
        Destroy(presentation.gameObject);
        presentation = null;
    }

    private void OnDestroy()
    {
        if (waitingForEndScreen)
            SceneManager.sceneLoaded -= OnEndScreenLoaded;
        ClosePresentation();
    }
}

