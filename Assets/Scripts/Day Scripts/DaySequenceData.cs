using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueStepChoice
{
    public string choiceText;
    public HeadPersonality personality;
    public string targetStepOnClick;
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

    [Header("Choices (Optional)")]
    public List<DialogueStepChoice> choices = new List<DialogueStepChoice>();

    [Header("State Transition")]
    [Tooltip("Step ID to transition to after line completes (Leave empty if choices control step or if step doesn't auto-advance)")]
    public string nextStepID;

    [Header("Events / Skills")]
    public string awardSkillKey;

    [Header("What needs to happen for the script to continue?")]
    public NarrativeStepType stepType;
}

public enum NarrativeStepType
{
    DialogueLine,
    WaitUntilZoomCall,
    PresentChoices,
    WaitUntilCodeCompiled
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
