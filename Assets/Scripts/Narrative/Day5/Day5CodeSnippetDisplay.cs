using TMPro;
using UnityEngine;

/// <summary>
/// Controls the Day 5 IDE-style screen share.
///
/// This component should live on the always-active Day5ScreenShare object.
/// IDECanvas itself can remain disabled until Steve starts sharing his screen.
/// </summary>
public class Day5CodeSnippetDisplay : MonoBehaviour
{
    [Header("Day 5 IDE")]
    [SerializeField] private GameObject ideCanvas;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text codeText;

    private string lastStep;
    private bool isVisible;

    private void Awake()
    {
        if (ideCanvas != null)
        {
            ideCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        RefreshSnippet();
    }

    /// <summary>
    /// Called by ZoomScreenShareController when Day 5 screen sharing begins.
    /// </summary>
    public void ShowDay5ScreenShare()
    {
        isVisible = true;

        if (ideCanvas != null)
        {
            ideCanvas.SetActive(true);
        }

        lastStep = null;
        RefreshSnippet();
    }

    /// <summary>
    /// Called when Steve stops sharing his screen.
    /// </summary>
    public void HideDay5ScreenShare()
    {
        isVisible = false;

        if (ideCanvas != null)
        {
            ideCanvas.SetActive(false);
        }
    }

    private void RefreshSnippet()
    {
        GameManager manager = GameManager.Instance;

        if (manager == null || manager.CurrentDay != 5)
        {
            return;
        }

        string step = manager.CurrentStep ?? "";

        if (step == lastStep)
        {
            return;
        }

        lastStep = step;

        if (IsFrameLoopStep(step))
        {
            SetSnippet(
                "SFML frame loop vs. Battleship turns",
                "while (window.isOpen())\n" +
                "{\n" +
                "    float dt = delta_clock.restart().asSeconds();\n\n" +
                "    processEvents();\n" +
                "    update(dt);\n" +
                "    render();\n" +
                "}");
        }
        else if (IsFinalTestStep(step))
        {
            SetSnippet(
                "Corrected turn flow",
                "ai_board.Attack_Tile(grid_y, grid_x);\n\n" +
                "if (ai_board.Get_Sunk_Ships() == SHIP_COUNT)\n" +
                "{\n" +
                "    game_over = true;\n" +
                "}\n\n" +
                "if (!game_over)\n" +
                "{\n" +
                "    executeAITurn();\n" +
                "}");
        }
        else
        {
            SetSnippet(
                "Steve's broken turn flow",
                "ai_board.Attack_Tile(grid_y, grid_x);\n\n" +
                "executeAITurn();\n\n" +
                "if (ai_board.Get_Sunk_Ships() == SHIP_COUNT)\n" +
                "{\n" +
                "    game_over = true;\n" +
                "}");
        }
    }

    private static bool IsFrameLoopStep(string step)
    {
        return step == "D5_FrameQuestion" ||
               step == "D5_PragmatistAdvice" ||
               step == "D5_FrameChoices" ||
               step.StartsWith("D5_Frame_");
    }

    private static bool IsFinalTestStep(string step)
    {
        return step.StartsWith("D5_Final") ||
               step.StartsWith("D5_Close") ||
               step.StartsWith("D5_End");
    }

    private void SetSnippet(string title, string code)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (codeText != null)
        {
            codeText.text = code;
        }
    }
}