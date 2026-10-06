using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DayNarrativeExecutor : MonoBehaviour
{
    [Header("Day Sequences Config")]
    [Tooltip("Assign day assets here in order: Index 0 = Day 1, Index 1 = Day 2, etc.")]
    [SerializeField] private List<DaySequenceData> daySequences = new List<DaySequenceData>();

    private DaySequenceData currentDayData;
    private string lastExecutedStepID = null;

    private void Start()
    {
        GameManager.EnsureInstance();
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        LoadAndExecuteDay(currentDay);
    }

    public void LoadAndExecuteDay(int dayNumber)
    {
        int targetIndex = dayNumber - 1;
        if (targetIndex < 0 || targetIndex >= daySequences.Count)
        {
            Debug.LogError($"[DayNarrativeExecutor] No DaySequenceData assigned for Day {dayNumber}.");
            return;
        }

        currentDayData = daySequences[targetIndex];
        _ = RunCurrentStepAsync();
    }

    private async Task RunCurrentStepAsync()
    {
        if (currentDayData == null) return;

        string currentStepID = GameManager.Instance.CurrentStep;

        if (currentStepID == lastExecutedStepID && DialogueManager.Instance != null && DialogueManager.Instance.IsTypingComplete)
        {
            return;
        }

        NarrativeStep stepData = currentDayData.GetStep(currentStepID);
        if (stepData == null)
        {
            Debug.LogWarning($"[DayNarrativeExecutor] Step '{currentStepID}' not found in Day {currentDayData.dayNumber} data.");
            return;
        }

        lastExecutedStepID = currentStepID;

        // 1. Play Dialogue Line if text exists
        if (!string.IsNullOrEmpty(stepData.lineText))
        {
            DialogueLine line = new DialogueLine
            {
                speakerName = stepData.speakerName,
                personality = stepData.personality,
                text = stepData.lineText,
                displayDuration = stepData.displayDuration
            };

            DialogueManager.Instance.DisplayLine(line);
            await Task.Delay((int)(stepData.displayDuration * 1000));
        }

        // 2. Handle Skill Rewards
        if (!string.IsNullOrEmpty(stepData.awardSkillKey))
        {
            PersonalitySkills skills = FindAnyObjectByType<PersonalitySkills>();
            if (skills != null)
            {
                skills.AwardPoint(stepData.awardSkillKey);
            }
        }

        // 3. WAIT FOR GAMEPLAY EVENTS (NEW BLOCK)
        switch (stepData.stepType)
        {
            case NarrativeStepType.WaitUntilZoomCall:
                // Optional: Automatically prompt the call UI if assigned
                if (ZoomScreenShareController.Instance != null)
                {
                    ZoomScreenShareController.Instance.TriggerIncomingCall();
                }
                // Halted until user clicks Accept Call!
                await NarrativeWaitHelpers.WaitForZoomCall();
                break;

            case NarrativeStepType.WaitUntilCodeCompiled:
                // Halted until your IDE script fires GameplayEvents.TriggerCodeCompiled("SUCCESS")
                await NarrativeWaitHelpers.WaitForCodeCompilation();
                break;

            case NarrativeStepType.DialogueLine:
            case NarrativeStepType.PresentChoices:
                // Standard progression
                break;
        }

        // 4. Present Choices or Auto-Advance
        if (stepData.choices != null && stepData.choices.Count > 0)
        {
            (string text, HeadPersonality personality)[] choiceArray = new (string, HeadPersonality)[stepData.choices.Count];
            for (int i = 0; i < stepData.choices.Count; i++)
            {
                choiceArray[i] = (stepData.choices[i].choiceText, stepData.choices[i].personality);
            }

            int selectedIndex = await PresentChoicesAsync(choiceArray);

            if (selectedIndex >= 0 && selectedIndex < stepData.choices.Count)
            {
                DialogueStepChoice chosen = stepData.choices[selectedIndex];
                ApplyPersonalityStat(chosen.personality, 1);

                if (!string.IsNullOrEmpty(chosen.targetStepOnClick))
                {
                    GameManager.Instance.setStep(chosen.targetStepOnClick);
                    _ = RunCurrentStepAsync();
                }
            }
        }
        else if (!string.IsNullOrEmpty(stepData.nextStepID))
        {
            GameManager.Instance.setStep(stepData.nextStepID);
            _ = RunCurrentStepAsync(); // Ensure execution loops to next step automatically
        }
    }

    private async Task<int> PresentChoicesAsync((string text, HeadPersonality personality)[] choices)
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

    private void ApplyPersonalityStat(HeadPersonality personality, int points)
    {
        if (GameManager.Instance == null) return;

        switch (personality)
        {
            case HeadPersonality.GrandArchitect: GameManager.Instance.ChangeGrandArchitectStat(points); break;
            case HeadPersonality.Pragmatist: GameManager.Instance.ChangePragmatistStat(points); break;
            case HeadPersonality.Sentinel: GameManager.Instance.ChangeSentinelStat(points); break;
            case HeadPersonality.Genius: GameManager.Instance.ChangeGeniusStat(points); break;
            case HeadPersonality.Detective: GameManager.Instance.ChangeDetectiveStat(points); break;
        }
    }
}
