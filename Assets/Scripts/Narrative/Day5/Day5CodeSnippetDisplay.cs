using TMPro;
using UnityEngine;

/// <summary>
/// Updates the text inside Steve's screen-share area according to the active Day 5
/// narrative step. It only reads GameManager state; it does not advance the story.
/// </summary>
public class Day5CodeSnippetDisplay : MonoBehaviour
{
    [Header("Day 5 Code UI")]
    [SerializeField] private GameObject day5CodeRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text codeText;

    [Header("Optional Existing Screen-Share Content")]
    [Tooltip("If your screen-share area already contains a video/other content for other days, assign its root here. It will be hidden only while Day 5 is active.")]
    [SerializeField] private GameObject nonDay5ContentRoot;

    private string lastStep;

    private const string BrokenTurnCode =
@"ai_board.Attack_Tile(grid_y, grid_x);

executeAITurn();

if (ai_board.Get_Sunk_Ships() == SHIP_COUNT) {
    game_over = true;
}";

    private const string MouseEventCode =
@"if (const auto* mouseEvent =
    event->getIf<sf::Event::MouseButtonPressed>()) {
    // SFML reports the input event.
    // Game rules decide what the click means.
}";

    private const string FrameLoopCode =
@"while (window.isOpen()) {
    processEvents();
    update(dt);
    render();
}";

    private const string CorrectedTurnCode =
@"ai_board.Attack_Tile(grid_y, grid_x);

if (ai_board.Get_Sunk_Ships() == SHIP_COUNT) {
    game_over = true;
}

if (!game_over) {
    executeAITurn();
}";

    private const string TurnFlowText =
@"Receive Player Input
        ↓
Validate and Resolve Attack
        ↓
Update Game State
        ↓
Check Terminal Condition
        ↓
Begin AI Turn";

    private void OnEnable()
    {
        ApplyDayVisibility();
        lastStep = null;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        ApplyDayVisibility();
        if (gm.CurrentDay != 5) return;

        if (gm.CurrentStep == lastStep) return;

        lastStep = gm.CurrentStep;
        RefreshForStep(lastStep);
    }

    private void ApplyDayVisibility()
    {
        bool isDay5 = GameManager.Instance != null && GameManager.Instance.CurrentDay == 5;

        if (day5CodeRoot != null)
        {
            day5CodeRoot.SetActive(isDay5);
        }

        if (nonDay5ContentRoot != null)
        {
            nonDay5ContentRoot.SetActive(!isDay5);
        }
    }

    private void RefreshForStep(string step)
    {
        if (string.IsNullOrEmpty(step)) return;

        if (step == "D5_ArchitectAdvice" || step.StartsWith("D5_Bug"))
        {
            SetSnippet("Steve's Broken Revision", BrokenTurnCode);
            return;
        }

        if (step.StartsWith("D5_SFML"))
        {
            SetSnippet("SFML Input Event", MouseEventCode);
            return;
        }

        if (step == "D5_TraceIntro" || step == "D5_TraceTurn" || step.StartsWith("D5_Sentinel"))
        {
            SetSnippet("Trace the Turn", TurnFlowText);
            return;
        }

        if (step.StartsWith("D5_Frame"))
        {
            SetSnippet("SFML Application Loop", FrameLoopCode);
            return;
        }

        if (step.StartsWith("D5_Final"))
        {
            SetSnippet("Corrected Turn Ordering", CorrectedTurnCode);
        }
    }

    private void SetSnippet(string title, string code)
    {
        if (titleText != null) titleText.text = title;
        if (codeText != null) codeText.text = code;
    }
}
