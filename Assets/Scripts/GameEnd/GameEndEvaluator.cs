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
/// Uses GameManager's recorded run state to present the selected ending in the
/// existing GameEndScreen scene. Keep this component in the active DesktopUI scene
/// so DayNarrativeExecutor can find it when FinalDay_Data reaches TriggerGameEnd.
/// </summary>
public sealed class GameEndEvaluator : MonoBehaviour
{
    [Header("UML Asset")]
    [Tooltip("The clean/reference UML shown for Steve and for high-quality player work.")]
    public Sprite standardUmlSprite;

    [Header("End Screen")]
    [SerializeField] private string endScreenSceneName = "GameEndScreen";

    // Retained so existing serialized Inspector data is not needlessly discarded.
    [Header("Legacy two-ending fields (not used by the seven-ending evaluator)")]
    [HideInInspector] public string successTitle = "Ending: The Perfectionist Trap";
    [HideInInspector] public Color successTitleColor = Color.white;
    [HideInInspector, TextArea(3, 10)] public string successText = "";
    [HideInInspector] public string failTitle = "Ending: Coupled and Deprecated";
    [HideInInspector] public Color failTitleColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [HideInInspector, TextArea(3, 10)] public string failText = "";

    private bool triggerInProgress;
    private bool waitingForEndScreen;

    public void TriggerFinalEvaluation()
    {
        if (triggerInProgress)
        {
            Debug.LogWarning("[GameEndEvaluator] Final evaluation was already triggered.", this);
            return;
        }

        GameManager game = GameManager.EnsureInstance();
        GameEndingType ending = game.DetermineEnding();
        Debug.Log("[GameEndEvaluator] Final ending decided: " + ending, this);

        triggerInProgress = true;

        Scene loadedScene = SceneManager.GetSceneByName(endScreenSceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            InjectEndingData();
            return;
        }

        waitingForEndScreen = true;
        SceneManager.sceneLoaded += OnEndScreenLoaded;
        SceneManager.LoadScene(endScreenSceneName, LoadSceneMode.Additive);
    }

    private void OnEndScreenLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForEndScreen || scene.name != endScreenSceneName)
            return;

        waitingForEndScreen = false;
        SceneManager.sceneLoaded -= OnEndScreenLoaded;
        InjectEndingData();
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

        GameManager game = GameManager.EnsureInstance();
        GameEndingType ending = game.SelectedEnding ?? game.DetermineEnding();

        string title;
        string body;
        Color titleColor;

        switch (ending)
        {
            case GameEndingType.ObsoleteAndDeprecated:
                title = "Ending: Obsolete & Deprecated";
                body =
                    "Management: \"Your implementation is not at the level expected of a senior developer. Steve presented a stronger independent version of Battleship, and we have decided to replace your work. Your employment is terminated effective immediately.\"\n\n" +
                    "Steve takes over the project. The code you struggled to rebuild is left behind, and the people who tried to reach you are still waiting for an answer.";
                titleColor = new Color(0.92f, 0.30f, 0.30f, 1f);
                break;

            case GameEndingType.ObsoleteSoftRelaunch:
                title = "Ending: Obsolete, with a Soft Relaunch";
                body =
                    "The evaluation is over, but the result is not the clean recovery you hoped for. Your implementation was underwhelming for a senior developer. The following morning, an email informs you that your position is being ended; Steve later presents an alternative that management prefers.\n\n" +
                    "It is not the immediate public replacement of the worst outcome, but the direction is painfully clear.";
                titleColor = new Color(0.91f, 0.62f, 0.30f, 1f);
                break;

            case GameEndingType.ImposterProtocol:
                title = "Ending: Imposter Protocol";
                body =
                    "Steve spent the weekend helping repair the project, and together you got it across the finish line. Management accepts the result, so you keep your position.\n\n" +
                    "But you know how much of the work Steve had to rescue. Your future is safe for now, not secure. You have another chance to relearn the craft instead of pretending you never lost it.";
                titleColor = new Color(0.88f, 0.75f, 0.40f, 1f);
                break;

            case GameEndingType.PerfectionistTrap:
                title = "Ending: The Perfectionist Trap";
                body =
                    "The project is excellent. The architecture is clean, the implementation is polished, and management is impressed.\n\n" +
                    "Then the workday ends. There is no Steve beside you, no message from your friends, and no one waiting for you at home. The familiar emptiness returns. You feel a sharp sense of deja vu: you have built something impressive while quietly dismantling the rest of your life.";
                titleColor = new Color(0.72f, 0.82f, 0.95f, 1f);
                break;

            case GameEndingType.UltimateMentor:
                title = "Ending: The Ultimate Mentor";
                body =
                    "The project is strong, and Steve has become a genuine collaborator rather than someone watching for you to slip. During the presentation, you publicly share the credit and acknowledge how much you learned from working together.\n\n" +
                    "Steve does the same for you. The two of you leave the review as friends and as a team. For once, success does not require shutting everyone else out.";
                titleColor = new Color(0.48f, 0.84f, 0.67f, 1f);
                break;

            case GameEndingType.ArchitectRedemption:
                title = "Ending: The Architect's Redemption";
                body =
                    "You present a high-quality project, but this time the achievement is not the only thing that matters. You chose to reconnect with the people who care about you, and you begin to understand that being a good developer is not the same as making work your entire identity.\n\n" +
                    "The Grand Architect's best design is not another class diagram. It is a life with room for both meaningful work and meaningful relationships.";
                titleColor = new Color(0.48f, 0.84f, 0.67f, 1f);
                break;

            case GameEndingType.SegmentationFaultOfSoul:
                title = "Secret Ending: Segmentation Fault of the Soul";
                body =
                    "The recovered clues finally fit together. Your name is Bjarne. You had just turned forty. Ashley had planned a date because she wanted to talk about how far you had withdrawn from her. You stood her up. You had been under mounting pressure at work, and your circle of friends had dwindled until Mark was almost the only one still reaching out.\n\n" +
                    "That weekend, overwhelmed and unable to face the people trying to help, you drank heavily and blacked out. The memory loss was not a conspiracy or a clever trick: it followed a personal collapse you had tried to outrun. The three voices were fragments of the ways you had learned to cope: the Architect who tried to impose order, the Pragmatist who wanted to avoid the pain, and the Detective who kept searching for an explanation.\n\n" +
                    "You cannot rewrite that weekend. You can decide what you do with the truth now.";
                titleColor = new Color(0.70f, 0.78f, 1f, 1f);
                break;

            default:
                title = "Ending: Obsolete, with a Soft Relaunch";
                body = "The evaluation is complete. Your result leaves the future uncertain, and there is still work to do to rebuild both your skills and your relationships.";
                titleColor = new Color(0.91f, 0.62f, 0.30f, 1f);
                break;
        }

        uiManager.SetupEndScreen(title, body, standardUmlSprite, titleColor);
    }

    private void OnDestroy()
    {
        if (waitingForEndScreen)
            SceneManager.sceneLoaded -= OnEndScreenLoaded;
    }
}
