using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueStepChoice
{
    public string choiceText;
    public HeadPersonality personality;
    [Tooltip("Personality stat gain on selection. Use 0 for an incorrect suggestion that loops back to retry; existing choices default to 1.")]
    public int personalityStatChange = 1;
    public string targetStepOnClick;
    public int suspicionChange = 0;

    [Header("Skill Requirement")]
    public PersonalityLevel requiredLevel = PersonalityLevel.Novice;
    [Tooltip("Variants with the same nonempty group show only the highest unlocked level. Leave empty for independent choices.")]
    public string choiceGroup;
}

[Serializable]
public class NarrativeLineVariant
{
    [Tooltip("This text is available when the speaking personality has at least this level.")]
    public PersonalityLevel requiredLevel = PersonalityLevel.Novice;

    [TextArea(2, 6)]
    public string lineText;
}

public static class NarrativeChoiceFilter
{
    public static List<DialogueStepChoice> GetAvailableChoices(
        List<DialogueStepChoice> choices, Func<HeadPersonality, int> getLevel)
    {
        var available = new List<DialogueStepChoice>();
        var groupIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        if (choices == null) return available;

        foreach (DialogueStepChoice choice in choices)
        {
            if (choice == null || getLevel(choice.personality) < (int)choice.requiredLevel)
                continue;

            // Existing assets without a group retain all of their independent options.
            if (string.IsNullOrWhiteSpace(choice.choiceGroup))
            {
                available.Add(choice);
                continue;
            }

            if (!groupIndices.TryGetValue(choice.choiceGroup, out int index))
            {
                groupIndices.Add(choice.choiceGroup, available.Count);
                available.Add(choice);
            }
            else if ((int)choice.requiredLevel > (int)available[index].requiredLevel)
            {
                available[index] = choice;
            }
        }

        return available;
    }
}

[Serializable]
public class NarrativeStep
{
    [Header("Receive Message")]
    public NarrativeMessageData message = new NarrativeMessageData();

    [Tooltip("Unique identifier matching GameManager.Instance.CurrentStep (e.g. 'Start', 'Task1_In_Progress')")]
    public string stepID;

    [Header("Dialogue Line")]
    public string speakerName;
    public HeadPersonality personality = HeadPersonality.None;
    [TextArea(2, 5)] public string lineText;
    [Tooltip("Obsolete: Dialogue now waits for player clicks instead of using timers.")]
    [HideInInspector] public float displayDuration = 3.5f;

    [Header("Level-Based Dialogue (Optional)")]
    [Tooltip("For personality advice. The executor automatically uses the highest unlocked variant. Leave empty to use Line Text normally.")]
    public List<NarrativeLineVariant> lineVariants = new List<NarrativeLineVariant>();

    [Header("Choices (Optional)")]
    public List<DialogueStepChoice> choices = new List<DialogueStepChoice>();

    [Header("State Transition")]
    [Tooltip("Step ID to transition to after line completes (Leave empty if choices control step or if step doesn't auto-advance)")]
    public string nextStepID;

    [Tooltip("Step ID to transition to if the player FAILS a minigame/task")]
    public string alternateNextStepID;

    [Header("Events / Skills")]
    public string awardSkillKey;

    [Tooltip("Used by WaitUntilNarrativeEvent steps.")]
    public string narrativeEventKey;

    [Header("What needs to happen for the script to continue?")]
    public NarrativeStepType stepType;

    [Tooltip("Used only for Day2Exercise steps: 0 = positioning, 1 = loops, 2 = resizing, 3 = regression tests.")]
    public int exerciseIndex;

    [Header("Suspicion Settings")]
    public int suspicionChange = 0;

    [Header("Delay Settings")]
    [Tooltip("Custom delay in seconds for DelaySeconds step type")]
    public float delayDuration = 10f;

    [Header("Delay Settings")]
    public SteveExpression NewSteveExpression;

    [Header("Scene Transition Settings")]
    [Tooltip("Name of the night/sleep scene to load")]
    public string targetSceneName = "Night Screen";
    [Header("Steve Coding Challenge (C++ Skill Check)")]
    public SteveChallengeData steveChallenge;
}

public enum NarrativeStepType
{
    // Explicit values protect serialized day assets from future additions.
    DialogueLine = 0,
    WaitUntilZoomCall = 1,
    PresentChoices = 2,
    WaitUntilCodeCompiled = 3,
    DelaySeconds = 4,
    StartScreenShare = 5,
    WaitUntilScreenShareEnd = 6,
    TriggerGameEnd = 7,
    Day2Exercise = 8,
    SetSteveExpression = 9,
    SwitchToNightScene = 10,
    SteveCodingChallenge = 11,
    WaitUntilNarrativeEvent = 12,
    EndScreenShare = 13,
    EndZoomCall = 14,
    WaitUntilRefactoFightCompleted = 15,
    ReceiveMessage = 1000,
    Day3BoardExercise = 1001,
    Day3BoardEmail = 1002
}

[CreateAssetMenu(fileName = "DaySequence_Day0", menuName = "Narrative/Day Sequence Data")]
public class DaySequenceData : ScriptableObject
{
    private void OnValidate() { NarrativeMessageIds.Ensure(this); }

    public int dayNumber = 1;
    public List<NarrativeStep> steps = new List<NarrativeStep>();

    public NarrativeStep GetStep(string stepID)
    {
        return steps.Find(s => s.stepID.Equals(stepID, StringComparison.OrdinalIgnoreCase));
    }
}

