using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class DayScriptBase : MonoBehaviour
{
    [Header("Day Configuration")]
    [SerializeField] protected int dayNumber = 1;

    public int DayNumber => dayNumber;

    protected virtual void Start()
    {
        // Ensure GameManager exists in memory
        GameManager.EnsureInstance();
        // Run the narrative loop for this specific monitor/day
        RunDaySequence();
    }

    /// <summary>
    /// Executes the full async flow for the day.
    /// </summary>
    protected abstract void RunDaySequence();

    /// <summary>
    /// Displays a line via DialogueManager.
    /// </summary>
    protected async Task PlayLineAsync(string speaker, HeadPersonality personality, string text, float duration = 3.5f)
    {
        DialogueLine line = new DialogueLine
        {
            speakerName = speaker,
            personality = personality,
            text = text,
            displayDuration = duration
        };

        DialogueManager.Instance.DisplayLine(line);
        await Task.Delay((int)(duration * 1000));
    }

    /// <summary>
    /// Presents dialogue choices to the player.
    /// </summary>
    protected async Task<int> PresentChoicesAsync(params (string text, HeadPersonality personality)[] choices)
    {
        DialogueChoiceOption[] options = new DialogueChoiceOption[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            options[i] = new DialogueChoiceOption
            {
                text = choices[i].text,
                personality = choices[i].personality
            };
        }

        return await DialogueManager.Instance.PresentChoices(options);
    }

    /// <summary>
    /// Modifies personality stats in GameManager without altering GameManager.cs.
    /// </summary>
    protected void ApplyPersonalityStat(HeadPersonality personality, int points = 1)
    {
        if (GameManager.Instance == null) return;

        switch (personality)
        {
            case HeadPersonality.GrandArchitect:
                GameManager.Instance.ChangeGrandArchitectStat(points);
                break;
            case HeadPersonality.Pragmatist:
                GameManager.Instance.ChangePragmatistStat(points);
                break;
            case HeadPersonality.Sentinel:
                GameManager.Instance.ChangeSentinelStat(points);
                break;
            case HeadPersonality.Genius:
                GameManager.Instance.ChangeGeniusStat(points);
                break;
            case HeadPersonality.Detective:
                GameManager.Instance.ChangeDetectiveStat(points);
                break;
        }
    }

    /// <summary>
    /// Helper to transition between monitors/scenes while preserving window state in GameManager.
    /// </summary>
    protected void SwitchMonitorScene(string targetSceneName)
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
