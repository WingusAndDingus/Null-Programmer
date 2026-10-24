using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.VectorGraphics;
using NullProgrammer.Day2;
using UnityEngine;

public class DayNarrativeExecutor : MonoBehaviour
{
    [Header("Day Sequences Config")]
    [Tooltip("Assign day assets here in order: Index 0 = Day 1, Index 1 = Day 2, etc.")]
    [SerializeField] private List<DaySequenceData> daySequences = new List<DaySequenceData>();

    private DaySequenceData currentDayData;
    private string lastExecutedStepID = null;
    private SceneSwitcher sceneSwitch;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private CancellationToken cancellationToken;

    private void Awake()
    {
        cancellationToken = lifetime.Token;
    }

    private void OnDestroy()
    {
        lifetime.Cancel();
        lifetime.Dispose();
    }

    private void Start()
    {
        GameManager.EnsureInstance();
        sceneSwitch = FindAnyObjectByType<SceneSwitcher>();
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
        _ = RunCurrentStepSafelyAsync();
    }

    private async Task RunCurrentStepSafelyAsync()
    {
        try { await RunCurrentStepAsync(); }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private void ApplySuspicion(int amount)
    {
        if (amount == 0 || GameManager.Instance == null) return;
        GameManager.Instance.ChangeSteveSuspicion(amount);
    }

    private async Task RunCurrentStepAsync()
    {
        if (currentDayData == null || lifetime.IsCancellationRequested) return;

        string currentStepID = GameManager.Instance.CurrentStep;
        Debug.Log($"[DayNarrativeExecutor] Attempting to run step: '{currentStepID}'");

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

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.DisplayLine(line);
            }
            await Task.Delay((int)(stepData.displayDuration * 1000), cancellationToken);
        }

        // 2. Handle Skill Rewards
        if (!string.IsNullOrEmpty(stepData.awardSkillKey))
        {
            PersonalitySkills.TryAwardMilestone(stepData.awardSkillKey);
        }

        // 3. WAIT FOR GAMEPLAY EVENTS
        switch (stepData.stepType)
        {
            case NarrativeStepType.DelaySeconds:
                float waitTime = stepData.delayDuration > 0 ? stepData.delayDuration : 5f;
                Debug.Log($"[DayNarrativeExecutor] Delaying for {waitTime} seconds on step '{stepData.stepID}'...");

                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    await Task.Yield();
                }
                break;

            case NarrativeStepType.WaitUntilZoomCall:
                Task callStarted = NarrativeWaitHelpers.WaitForZoomCall(cancellationToken);
                if (ZoomScreenShareController.Instance != null)
                {
                    ZoomScreenShareController.Instance.TriggerIncomingCall();
                }
                await callStarted;
                break;

            case NarrativeStepType.StartScreenShare:
                if (ZoomScreenShareController.Instance != null)
                {
                    ZoomScreenShareController.Instance.StartScreenShare();
                }
                break;

            case NarrativeStepType.WaitUntilScreenShareEnd:
                await NarrativeWaitHelpers.WaitForScreenShareEnd(cancellationToken);
                break;

            case NarrativeStepType.WaitUntilCodeCompiled:
                await NarrativeWaitHelpers.WaitForCodeCompilation(cancellationToken);
                break;

            case NarrativeStepType.Day2Exercise:
                await Day2ExerciseWindow.RunAsync(stepData.exerciseIndex, cancellationToken);
                break;

            case NarrativeStepType.TriggerGameEnd:
                Debug.Log("[DayNarrativeExecutor] Triggering Final Evaluation...");
                GameEndEvaluator evaluator = FindAnyObjectByType<GameEndEvaluator>();
                if (evaluator != null)
                {
                    evaluator.TriggerFinalEvaluation();
                }
                break;
            case NarrativeStepType.SetSteveExpression:
                if (ZoomScreenShareController.Instance != null)
                {
                    ZoomScreenShareController.Instance.SetSteveExpression(stepData.NewSteveExpression);
                }
                break;
            case NarrativeStepType.SwitchToNightScene:
                string sceneToLoad = !string.IsNullOrEmpty(stepData.targetSceneName) ? stepData.targetSceneName : "NightScene";
                Debug.Log($"[DayNarrativeExecutor] Transitioning to night scene: '{sceneToLoad}'");

                if (sceneSwitch != null)
                {
                    sceneSwitch.LoadScene(sceneToLoad);
                }
                else
                {
                    // Fallback to Unity's SceneManager if SceneSwitcher reference wasn't bound
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                }
                break;
            case NarrativeStepType.SteveCodingChallenge:
                if (stepData.steveChallenge != null)
                {
                    await RunSteveChallengeAsync(stepData.steveChallenge);
                }
                else
                {
                    Debug.LogWarning($"[DayNarrativeExecutor] Step '{stepData.stepID}' is set to SteveCodingChallenge, but no SteveChallengeData asset was assigned!");
                }
                break;
            case NarrativeStepType.DialogueLine:
            case NarrativeStepType.PresentChoices:
                break;
        }

        if (lifetime.IsCancellationRequested) return;

        // Apply suspicion shift after the step condition resolves
        ApplySuspicion(stepData.suspicionChange);

        // 4. Present Choices or Auto-Advance
        if (stepData.choices != null && stepData.choices.Count > 0)
        {
            var levelCache = new Dictionary<HeadPersonality, int>();
            List<DialogueStepChoice> availableChoices = NarrativeChoiceFilter.GetAvailableChoices(
                stepData.choices, personality =>
                {
                    if (!levelCache.TryGetValue(personality, out int level))
                    {
                        level = GetPersonalityLevel(personality);
                        levelCache.Add(personality, level);
                    }
                    return level;
                });

            if (availableChoices.Count == 0)
            {
                Debug.LogError($"[DayNarrativeExecutor] No unlocked choices for '{stepData.stepID}'. Add a Novice fallback.");
                return;
            }

            Debug.Log($"[DayNarrativeExecutor] Step '{stepData.stepID}' has {availableChoices.Count} unlocked choices.");

            (string text, HeadPersonality personality)[] choiceArray = new (string, HeadPersonality)[availableChoices.Count];
            for (int i = 0; i < availableChoices.Count; i++)
            {
                choiceArray[i] = (availableChoices[i].choiceText, availableChoices[i].personality);
            }

            int selectedIndex = await PresentChoicesAsync(choiceArray);
            if (lifetime.IsCancellationRequested) return;

            if (selectedIndex >= 0 && selectedIndex < availableChoices.Count)
            {
                DialogueStepChoice chosen = availableChoices[selectedIndex];
                ApplyPersonalityStat(chosen.personality, 1);

                if (!string.IsNullOrEmpty(chosen.targetStepOnClick))
                {
                    GameManager.Instance.setStep(chosen.targetStepOnClick);
                    _ = RunCurrentStepSafelyAsync();
                }
            }
        }
        else if (!string.IsNullOrEmpty(stepData.nextStepID))
        {
            GameManager.Instance.setStep(stepData.nextStepID);
            _ = RunCurrentStepSafelyAsync();
        }
    }

    private int GetPersonalityLevel(HeadPersonality personality)
    {
        Personality skill;
        switch (personality)
        {
            case HeadPersonality.GrandArchitect: skill = Personality.Architect; break;
            case HeadPersonality.Pragmatist: skill = Personality.Pragmatist; break;
            case HeadPersonality.Sentinel: skill = Personality.Sentinel; break;
            case HeadPersonality.Genius: skill = Personality.Genius; break;
            case HeadPersonality.Detective: skill = Personality.Detective; break;
            default: return 0;
        }

        PersonalitySkills skills = FindAnyObjectByType<PersonalitySkills>();
        return skills != null ? skills.GetLevel(skill) : PersonalitySkills.GetSavedLevel(skill);
    }

    private async Task<int> PresentChoicesAsync((string text, HeadPersonality personality)[] choices)
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[DayNarrativeExecutor] DialogueManager.Instance is NULL! Cannot present choice bubbles.");
            return -1;
        }

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
    // Step type handler for the Steve Skill checks
    public async Task RunSteveChallengeAsync(SteveChallengeData challenge)
    {
        // 1. Trigger Steve's incoming call UI / Audio
        // 2. Open the IDE interface (waiting for your teammate's final IDE UI plugin)

        // For now, you can hook this task to wait until the player submits text:
        string playerCode = await SteveScreenShareManager.Instance.PromptPlayerForCodeAsync(challenge);

        // 3. Validate against the accepted answers
        bool success = ValidatePlayerCode(playerCode, challenge);

        if (success)
        {
            Debug.Log("Steve: 'Looks solid! Good job.'");
            // Reward personality points or proceed to next step
        }
        else
        {
            Debug.Log("Steve: 'Hmm, that's not quite right. Let's review.'");
            // Handle retry or failure path
        }
    }
    public bool ValidatePlayerCode(string playerInput, SteveChallengeData challengeData)
    {
        if (string.IsNullOrEmpty(playerInput) || challengeData == null || challengeData.acceptedAnswers == null)
            return false;

        // Normalize player input (trim whitespace, normalize internal spacing if needed)
        string cleanPlayerInput = playerInput.Trim();

        foreach (string accepted in challengeData.acceptedAnswers)
        {
            if (string.IsNullOrEmpty(accepted)) continue;

            string cleanAccepted = accepted.Trim();
            if (cleanPlayerInput == cleanAccepted)
            {
                return true; // Match found!
            }
        }

        return false; // No matching pattern found
    }
}
