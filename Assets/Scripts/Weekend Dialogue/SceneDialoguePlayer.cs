using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays the authored scene dialogue. At the end of one of the four weekend scenes,
/// it commits Day 7 and returns to DesktopUI so FinalDay_Data can start.
/// </summary>
public class SceneDialoguePlayer : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private float startDelay = 3f;

    [Header("Weekend -> Evaluation Day")]
    [Tooltip("When enabled, finishing the weekend dialogue returns to DesktopUI and starts Day 7.")]
    [SerializeField] private bool returnToEvaluationDayAfterDialogue = true;
    [SerializeField] private string evaluationSceneName = "DesktopUI";
    [SerializeField, Min(1)] private int evaluationDayNumber = 7;

    [Header("Scene Background")]
    [InspectorName("Background Display (UI Image)")]
    [Tooltip("Assign the Image component that is actually displaying the scene background. Leave blank if the scene uses a SpriteRenderer instead.")]
    [SerializeField] private Image backgroundImage;

    [InspectorName("Background Display (Sprite Renderer)")]
    [Tooltip("Assign the SpriteRenderer that is actually displaying the scene background. Leave blank if the scene uses a UI Image instead.")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] lines;

    private bool transitionStarted;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        DialogueManager manager = DialogueManager.Instance;
        if (manager == null)
        {
            Debug.LogError("DialogueManager not found!", this);
            yield break;
        }

        foreach (DialogueLine line in lines)
        {
            if (line == null)
                continue;

            ApplyBackgroundForLine(line);
            yield return manager.PlayDialogueLine(line);
        }

        Debug.Log("Scene dialogue finished.", this);

        if (returnToEvaluationDayAfterDialogue && IsWeekendScene(SceneManager.GetActiveScene().name))
            yield return StartCoroutine(ReturnToEvaluationDay());
    }

    private static bool IsWeekendScene(string sceneName)
    {
        return sceneName == "WeekendSteve" ||
               sceneName == "WeekendStudy" ||
               sceneName == "WeekendConnections" ||
               sceneName == "WeekendWallow";
    }

    private IEnumerator ReturnToEvaluationDay()
    {
        if (transitionStarted)
            yield break;

        // This component is used by the weekend scenes. The selected choice is a
        // guard against accidentally sending a standalone dialogue test to Day 7.
        GameManager game = GameManager.EnsureInstance();
        if (!game.HasSelectedWeekendChoice)
        {
            Debug.Log("[SceneDialoguePlayer] Weekend dialogue ended without a committed weekend choice; staying in this scene.", this);
            yield break;
        }

        transitionStarted = true;
        game.setCurrentDay(evaluationDayNumber);
        game.setStep("Start");

        // Let the final line's dialogue panel finish hiding before the scene changes.
        yield return null;
        SceneManager.LoadScene(evaluationSceneName, LoadSceneMode.Single);
    }

    private void ApplyBackgroundForLine(DialogueLine line)
    {
        if (line.backgroundImage == null)
            return;

        bool applied = false;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = line.backgroundImage;
            applied = true;
        }

        if (backgroundSpriteRenderer != null)
        {
            backgroundSpriteRenderer.sprite = line.backgroundImage;
            applied = true;
        }

        if (!applied)
        {
            Debug.LogWarning(
                $"[SceneDialoguePlayer] Dialogue line '{line.speakerName}' requests background " +
                $"'{line.backgroundImage.name}', but no Background Display target is assigned. " +
                "Assign the scene's visible UI Image or SpriteRenderer on SceneDialoguePlayer.",
                this);
        }
    }
}
