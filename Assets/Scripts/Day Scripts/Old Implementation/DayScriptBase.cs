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
        GameManager.EnsureInstance();

        // Execute narrative sequence safely on start
        _ = ExecuteDaySequenceAsync();
    }

    private async Task ExecuteDaySequenceAsync()
    {
        await RunDaySequenceAsync();
    }

    /// <summary>
    /// Executes the full async flow for the day safely.
    /// </summary>
    protected abstract Task RunDaySequenceAsync();

    /// <summary>
    /// Displays a line via DialogueManager only if it hasn't already completed/played.
    /// </summary>
    protected async Task PlayLineAsync(string speaker, HeadPersonality personality, string text, float duration = 3.5f)
    {
        // If this exact text is currently active or typing, don't re-trigger!
        if (DialogueManager.Instance != null && DialogueManager.Instance.CurrentText == text)
        {
            if (DialogueManager.Instance.IsTypingComplete)
            {
                return; // Line already finished playing earlier in this step
            }
        }

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

    protected void SwitchMonitorScene(string targetSceneName)
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
