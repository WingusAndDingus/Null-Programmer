using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Day2SkillDialogueChecks
{
    [MenuItem("Tools/Null Programmer/Validate Day 2 Skill Dialogue")]
    public static void Validate()
    {
        DaySequenceData day = AssetDatabase.LoadAssetAtPath<DaySequenceData>(
            "Assets/Scripts/Day Scripts/Day2_Data.asset");
        Require(day != null && day.dayNumber == 2, "Day 2 asset is missing or has the wrong day number.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (NarrativeStep step in day.steps)
            Require(ids.Add(step.stepID), "Duplicate step: " + step.stepID);
        Require(ids.Contains("Start"), "Day 2 needs a Start step.");

        int decisions = 0;
        int combinations = 0;
        foreach (NarrativeStep step in day.steps)
        {
            if (!string.IsNullOrEmpty(step.nextStepID))
                Require(ids.Contains(step.nextStepID), "Missing next step for " + step.stepID);
            if (step.choices == null || step.choices.Count == 0) continue;
            decisions++;
            Require(step.choices.Count == 15, step.stepID + " should have 15 authored variants.");
            foreach (DialogueStepChoice choice in step.choices)
                Require(ids.Contains(choice.targetStepOnClick), "Missing choice target: " + choice.targetStepOnClick);

            for (int architect = 0; architect <= 4; architect++)
            for (int pragmatist = 0; pragmatist <= 4; pragmatist++)
            for (int detective = 0; detective <= 4; detective++)
            {
                List<DialogueStepChoice> shown = NarrativeChoiceFilter.GetAvailableChoices(
                    step.choices, personality =>
                    {
                        switch (personality)
                        {
                            case HeadPersonality.GrandArchitect: return architect;
                            case HeadPersonality.Pragmatist: return pragmatist;
                            case HeadPersonality.Detective: return detective;
                            default: return 0;
                        }
                    });
                Require(shown.Count == 3, step.stepID + " must display exactly three bubbles.");
                var seen = new HashSet<HeadPersonality>();
                foreach (DialogueStepChoice choice in shown)
                {
                    Require(seen.Add(choice.personality), "Duplicate personality bubble in " + step.stepID);
                    int expected;
                    switch (choice.personality)
                    {
                        case HeadPersonality.GrandArchitect: expected = architect; break;
                        case HeadPersonality.Pragmatist: expected = pragmatist; break;
                        case HeadPersonality.Detective: expected = detective; break;
                        default: throw new InvalidOperationException("Unexpected Day 2 personality.");
                    }
                    Require((int)choice.requiredLevel == expected,
                        step.stepID + " selected the wrong rank for " + choice.personality);
                    Require(step.choices.Contains(choice), "Selection must preserve the authored choice and its target.");
                }
                combinations++;
            }
        }
        Require(decisions == 4, "Expected four Day 2 decisions.");

        // Ungrouped legacy answers must stay independent, even with the same personality.
        var first = new DialogueStepChoice { personality = HeadPersonality.Sentinel };
        var second = new DialogueStepChoice { personality = HeadPersonality.Sentinel };
        var independent = NarrativeChoiceFilter.GetAvailableChoices(
            new List<DialogueStepChoice> { first, second }, personality => 0);
        Require(independent.Count == 2 && independent[0] == first && independent[1] == second,
            "Independent quiz/legacy choices were incorrectly grouped.");

        Debug.Log("Day 2 skill dialogue passed: " + combinations +
            " decision/profile combinations, valid targets, and independent legacy choices.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
