using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueStepChoice
{
    public string choiceText;
    public HeadPersonality personality;
    public string targetStepOnClick;

    [Header("Choice Consequences")]
    public int suspicionChange = 0;
    public int friendshipChange = 0;
}

[Serializable]
public class NarrativeStep
{
    [Tooltip("Unique identifier matching GameManager.Instance.CurrentStep (e.g. 'Start', 'Task1_In_Progress')")]
    public string stepID;

    [Header("Dialogue Line")]
    public string speakerName;
    public HeadPersonality personality = HeadPersonality.None;
    [TextArea(2, 5)] public string lineText;
    public float displayDuration = 3.5f;

    [Header("Personality Skill Variant (Optional)")]
    [Tooltip("When enabled, the displayed text is chosen from lowSkillText/highSkillText using the selected personality's current GameManager stat.")]
    public bool usePersonalitySkillVariant = false;
    [Min(0)] public int highSkillThreshold = 3;
    [TextArea(2, 6)] public string lowSkillText;
    [TextArea(2, 6)] public string highSkillText;

    [Header("Choices (Optional)")]
    public List<DialogueStepChoice> choices = new List<DialogueStepChoice>();

    [Header("State Transition")]
    [Tooltip("Step ID to transition to after line completes (Leave empty if choices control step or if step doesn't auto-advance)")]
    public string nextStepID;

    [Header("Events / Skills")]
    public string awardSkillKey;

    [Tooltip("Used by WaitUntilNarrativeEvent. The sequence pauses until GameplayEvents.TriggerNarrativeEvent sends this exact key.")]
    public string narrativeEventKey;

    [Header("Zoom Call Options")]
    [Tooltip("Only used by WaitUntilZoomCall. Prevents ZoomScreenShareController's built-in default greeting from playing for this one call.")]
    public bool suppressZoomAcceptGreeting = false;

    [Tooltip("Only used by StartScreenShare. Prevents a configured VideoPlayer from playing for this one screen share (useful for code/text shares).")]
    public bool suppressScreenShareVideo = false;

    [Header("What needs to happen for the script to continue?")]
    public NarrativeStepType stepType;

    [Header("Suspicion Settings")]
    public int suspicionChange = 0;

    [Header("Delay Settings")]
    [Tooltip("Custom delay in seconds for DelaySeconds step type")]
    public float delayDuration = 10f;
}

public enum NarrativeStepType
{
    // IMPORTANT: keep the original entries in their original order so existing
    // serialized DaySequenceData assets retain their enum values.
    DialogueLine,
    WaitUntilZoomCall,
    PresentChoices,
    WaitUntilCodeCompiled,
    DelaySeconds,            // For simulated reading / pacing delays
    StartScreenShare,        // Calls ZoomScreenShareController.Instance.StartScreenShare()
    WaitUntilScreenShareEnd, // Halts until screen share ends
    TriggerGameEnd,

    // New entries are appended for backwards-compatible expansion.
    WaitUntilNarrativeEvent,
    EndScreenShare,
    EndZoomCall
}

[CreateAssetMenu(fileName = "DaySequence_Day0", menuName = "Narrative/Day Sequence Data")]
public class DaySequenceData : ScriptableObject
{
    public int dayNumber = 1;
    public List<NarrativeStep> steps = new List<NarrativeStep>();

    public NarrativeStep GetStep(string stepID)
    {
        return steps.Find(s => s.stepID.Equals(stepID, StringComparison.OrdinalIgnoreCase));
    }
}
