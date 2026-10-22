#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or refreshes the Day 5 sequence using the CURRENT narrative architecture.
/// This intentionally does not restore the deleted Interaction Four or the old Day-5-only
/// personality-variant/friendship systems.
/// </summary>
public static class Day5SequenceBuilder
{
    private const string FolderPath = "Assets/Narrative";
    private const string AssetPath = FolderPath + "/DaySequence_Day5.asset";

    // The user requested Day 5 dialogue to remain 50% longer than the earlier timings.
    private const float DurationMultiplier = 1.5f;

    [MenuItem("Tools/Null Programmer/Create or Refresh Day 5 Sequence")]
    public static void CreateOrRefresh()
    {
        EnsureFolder("Assets", "Narrative");

        DaySequenceData asset = AssetDatabase.LoadAssetAtPath<DaySequenceData>(AssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<DaySequenceData>();
            AssetDatabase.CreateAsset(asset, AssetPath);
        }

        asset.dayNumber = 5;
        asset.steps = BuildSteps();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        Debug.Log($"[Day5SequenceBuilder] Day 5 sequence created/refreshed at {AssetPath}");
    }

    private static List<NarrativeStep> BuildSteps()
    {
        var steps = new List<NarrativeStep>();

        // ------------------------------------------------------------------
        // Email first. No incoming call appears until this named event fires.
        // ------------------------------------------------------------------
        NarrativeStep start = Step("Start", NarrativeStepType.WaitUntilNarrativeEvent, "D5_AfterEmailDelay");
        start.narrativeEventKey = Day5NarrativeEvents.EmailRead;
        steps.Add(start);

        NarrativeStep afterEmailDelay = Step("D5_AfterEmailDelay", NarrativeStepType.DelaySeconds, "D5_WaitForCall");

        afterEmailDelay.delayDuration = 3.0f;
        steps.Add(afterEmailDelay);

        steps.Add(Step("D5_WaitForCall", NarrativeStepType.WaitUntilZoomCall, "D5_Steve_Turn_Logic"));

        // ------------------------------------------------------------------
        // Opening call
        // ------------------------------------------------------------------
        steps.Add(Line("D5_Steve_Turn_Logic", "Steve",
            "Hey. Good news. I cleaned up the turn logic.", "D5_Me_BadNews", 2.6f));

        steps.Add(Line("D5_Me_BadNews", "Me",
            "And the bad news?", "D5_Steve_Bug", 2.0f));

        steps.Add(Line("D5_Steve_Bug", "Steve",
            "Sometimes the computer shoots you after you've already won.", "D5_Me_Efficient", 3.0f));

        steps.Add(Line("D5_Me_Efficient", "Me",
            "Efficient.", "D5_Steve_Posthumous", 1.7f));

        steps.Add(Line("D5_Steve_Posthumous", "Steve",
            "I'm calling it \"posthumous retaliation.\"", "D5_Internal_Opening", 2.8f));

        steps.Add(Line("D5_Internal_Opening", "Me",
            "Wonderful. He's debugging something he doesn't understand with someone who can't remember understanding it.",
            "D5_Steve_MovedThings", 3.6f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_Steve_MovedThings", "Steve",
            "I moved a few things around. The original order looked unnecessarily picky.", "D5_Me_ShowMe", 3.2f));

        steps.Add(Line("D5_Me_ShowMe", "Me",
            "Show me.", "D5_StartShare", 1.8f));

        NarrativeStep startShare = Line("D5_StartShare", "Steve",
            "Here. This is the part.", "D5_ShareDelay", 2.2f);
        startShare.stepType = NarrativeStepType.StartScreenShare;
        steps.Add(startShare);

        NarrativeStep shareDelay = Step("D5_ShareDelay", NarrativeStepType.DelaySeconds, "D5_BugQuestion");
        shareDelay.delayDuration = 2.1f;
        steps.Add(shareDelay);

        // ------------------------------------------------------------------
        // Interaction One - Detective: reconstruct the failing sequence
        // ------------------------------------------------------------------
        steps.Add(Line("D5_BugQuestion", "Steve",
            "The win check is still there. Why does moving it down break anything?", "D5_DetectiveAdvice", 3.2f));

        steps.Add(Advice("D5_DetectiveAdvice", "The Detective", HeadPersonality.Detective,
            "We know the corpse fired the gun. Perhaps begin with the deeply suspicious fact that the corpse was supposed to be dead.",
            "Start at the impossible outcome and work backward. The AI attacks after the winning shot, so trace every call between resolving that shot and declaring victory.",
            "D5_BugChoices", 3.8f));

        NarrativeStep bugChoices = Step("D5_BugChoices", NarrativeStepType.PresentChoices);
        bugChoices.choices = new List<DialogueStepChoice>
        {
            Choice("Because executeAITurn() is in the wrong place.", "D5_Bug_Incomplete"),
            Choice("Because you've already started the AI's turn before checking whether the player's attack ended the match.", "D5_Bug_Correct"),
            Choice("Because SFML is processing the player and AI attacks at the same time.", "D5_Bug_Wrong", 5)
        };
        steps.Add(bugChoices);

        steps.Add(Line("D5_Bug_Correct", "Steve",
            "Oh. So I didn't forget the win condition. I just asked the question after I'd already let the AI shoot.",
            "D5_Bug_Correct_Me", 3.7f));
        steps.Add(Line("D5_Bug_Correct_Me", "Me",
            "Exactly.", "D5_SFML_Steve", 1.8f));

        steps.Add(Line("D5_Bug_Incomplete", "Steve",
            "Okay, but why is that place wrong?", "D5_Bug_Incomplete_Me", 2.6f));
        steps.Add(Line("D5_Bug_Incomplete_Me", "Me",
            "Because the player's action hasn't finished resolving. You transferred control before checking its consequences.",
            "D5_Bug_Incomplete_Steve", 3.7f));
        steps.Add(Line("D5_Bug_Incomplete_Steve", "Steve",
            "Right. So the placement is wrong because the sequence is wrong.", "D5_SFML_Steve", 3.0f));

        steps.Add(Line("D5_Bug_Wrong", "Steve",
            "But the AI doesn't use SFML input.", "D5_Bug_Wrong_Thought", 2.5f));
        steps.Add(Line("D5_Bug_Wrong_Thought", "Me",
            "He's asking distressingly good questions today.", "D5_SFML_Steve", 2.5f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_SFML_Steve", "Steve",
            "So SFML basically tells us, \"the mouse was clicked.\"", "D5_SFML_Me", 2.9f));
        steps.Add(Line("D5_SFML_Me", "Me",
            "Right. SFML reports the input event. Our game decides whether that click is legal, what it changes, and whether another turn should happen afterward.",
            "D5_TraceIntro", 4.2f));

        // ------------------------------------------------------------------
        // Trace the Turn mini-game
        // ------------------------------------------------------------------
        steps.Add(Line("D5_TraceIntro", "Me",
            "All right. Let's trace the turn one step at a time.",
            "D5_TraceTurn", 3.0f, HeadPersonality.InternalMonologue));
        
        NarrativeStep trace = Step(
            "D5_TraceTurn",
            NarrativeStepType.WaitUntilNarrativeEvent,
            "D5_TraceReflection"
        );

        trace.narrativeEventKey = Day5NarrativeEvents.TraceTurnComplete;
        steps.Add(trace);

        steps.Add(Line("D5_TraceReflection", "Me",
            "There it is. Input, resolve the action, update the state, check whether the match ended, then hand over control.",
            "D5_CheckEverywhere", 5.1f, HeadPersonality.InternalMonologue));

        // ------------------------------------------------------------------
        // Interaction Two - Grand Architect: terminal-state invariant
        // ------------------------------------------------------------------
        steps.Add(Line("D5_CheckEverywhere", "Steve",
            "Okay. So should I just keep adding if (!game_over) everywhere?", "D5_ArchitectAdvice", 3.1f));

        steps.Add(Advice("D5_ArchitectAdvice", "The Grand Architect", HeadPersonality.GrandArchitect,
            "Checks everywhere would certainly be... thorough. A fortress with seventeen gates and no obvious front door. There ought to be a cleaner rule.",
            "Think in terms of an invariant. Once the game enters a terminal state, no valid transition should lead from that state into another turn.",
            "D5_InvariantChoices", 4.0f));

        NarrativeStep invariantChoices = Step("D5_InvariantChoices", NarrativeStepType.PresentChoices);
        invariantChoices.choices = new List<DialogueStepChoice>
        {
            Choice("Once the game reaches a terminal state, the turn sequence should stop. Don't let control transition to another player after that.", "D5_Invariant_Correct"),
            Choice("Adding more checks everywhere is probably safest.", "D5_Invariant_Wrong", 5),
            Choice("Put the game_over check somewhere central instead of repeating it everywhere.", "D5_Invariant_Incomplete")
        };
        steps.Add(invariantChoices);

        steps.Add(Line("D5_Invariant_Correct", "Steve",
            "So the important thing is the rule: game over means there is no next turn.", "D5_Invariant_Correct_Me", 3.2f));
        steps.Add(Line("D5_Invariant_Correct_Me", "Me",
            "Exactly.", "D5_StateTransition", 1.8f));

        steps.Add(Line("D5_Invariant_Incomplete", "Me",
            "That would reduce duplication, but the important part is why the check exists.", "D5_Invariant_Incomplete_Me2", 3.0f));
        steps.Add(Line("D5_Invariant_Incomplete_Me2", "Me",
            "Once the match reaches a terminal state, nothing should transition it into another turn.", "D5_Invariant_Incomplete_Steve", 3.3f));
        steps.Add(Line("D5_Invariant_Incomplete_Steve", "Steve",
            "Right. The structure matters more than where I hide the Boolean.", "D5_StateTransition", 3.0f));

        steps.Add(Line("D5_Invariant_Wrong", "Steve",
            "I already tried that. The code started looking like it was afraid of itself.", "D5_Invariant_Wrong_Me", 3.4f));
        steps.Add(Line("D5_Invariant_Wrong_Me", "Me",
            "A reasonable response under the circumstances. The cleaner rule is that a terminal state has no transition into another turn.",
            "D5_StateTransition", 4.0f));

        steps.Add(Line("D5_StateTransition", "Steve",
            "So we're really defining which state changes are legal.", "D5_StateExactly", 2.8f));
        steps.Add(Line("D5_StateExactly", "Me",
            "Exactly.", "D5_FrameQuestion", 1.8f));

        // ------------------------------------------------------------------
        // Interaction Three - Pragmatist: SFML frames != Battleship turns
        // ------------------------------------------------------------------
        steps.Add(Line("D5_FrameQuestion", "Steve",
            "If Battleship alternates turns, shouldn't the main loop alternate too? Player loop, AI loop, player loop? Why does SFML keep running while we're waiting for somebody to move?",
            "D5_PragmatistAdvice", 6.7f));

        steps.Add(Advice("D5_PragmatistAdvice", "The Pragmatist", HeadPersonality.Pragmatist,
            "Because freezing the entire program until somebody clicks would be a spectacularly efficient way to make it look broken.",
            "Don't confuse the application's heartbeat with the game's turn system. Keep polling events, updating what needs updating, and drawing frames. Advance the Battleship turn only when a valid game action occurs.",
            "D5_FrameChoices", 6.2f));

        NarrativeStep frameChoices = Step("D5_FrameChoices", NarrativeStepType.PresentChoices);
        frameChoices.choices = new List<DialogueStepChoice>
        {
            Choice("Each frame is basically one Battleship turn.", "D5_Frame_Wrong", 5),
            Choice("SFML has to keep redrawing the window.", "D5_Frame_Incomplete"),
            Choice("The SFML frame loop and the Battleship turn system are separate. Frames keep running even when no gameplay turn changes.", "D5_Frame_Correct")            
        };
        steps.Add(frameChoices);

        steps.Add(Line("D5_Frame_Correct", "Steve",
            "So we could draw sixty frames without completing a single turn.", "D5_Frame_Correct_Me", 3.1f));
        steps.Add(Line("D5_Frame_Correct_Me", "Me",
            "Or six hundred.", "D5_Frame_Joke", 2.0f));

        steps.Add(Line("D5_Frame_Incomplete", "Me",
            "Rendering is part of it, but input and updates continue too. The important distinction is that a frame isn't a gameplay turn.",
            "D5_Frame_Incomplete_Steve", 4.0f));
        steps.Add(Line("D5_Frame_Incomplete_Steve", "Steve",
            "Right. The application keeps running while the game waits.", "D5_Frame_Joke", 3.0f));

        steps.Add(Line("D5_Frame_Wrong", "Steve",
            "At sixty frames per second?", "D5_Frame_Wrong_Thought", 2.4f));
        steps.Add(Line("D5_Frame_Wrong_Thought", "Me",
            "Competitive Battleship has become considerably more dangerous.", "D5_Frame_Joke", 2.8f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_Frame_Joke", "Steve",
            "Finally, software that accurately simulates meetings.", "D5_Final_Start", 2.8f));

        // ------------------------------------------------------------------
        // Final test - Interaction Four is intentionally absent.
        // ------------------------------------------------------------------
        steps.Add(Line("D5_Final_Start", "Steve",
            "All right. I moved the terminal check back where it belongs. Running it now.", "D5_Final_Fires", 3.3f));
        steps.Add(Line("D5_Final_Fires", "Steve",
            "Player fires...", "D5_Final_ShipDown", 2.0f));
        steps.Add(Line("D5_Final_ShipDown", "Steve",
            "Last ship goes down...", "D5_Final_Victory", 2.2f));
        steps.Add(Line("D5_Final_Victory", "Steve",
            "Victory screen.", "D5_Final_NoReturnFire", 2.0f));
        steps.Add(Line("D5_Final_NoReturnFire", "Steve",
            "And the computer did not shoot me from beyond the grave.", "D5_Final_Promising", 3.1f));
        steps.Add(Line("D5_Final_Promising", "Me",
            "Promising.", "D5_Final_DeadAI", 1.8f));
        steps.Add(Line("D5_Final_DeadAI", "Steve",
            "We've successfully taught artificial intelligence to recognize when it's dead.", "D5_Final_Resume", 3.4f));
        steps.Add(Line("D5_Final_Resume", "Me",
            "Put it on the resume.", "D5_Close_Understand", 2.0f));

        // ------------------------------------------------------------------
        // Closing Steve choice - narrative only, no custom friendship stat.
        // ------------------------------------------------------------------
        steps.Add(Line("D5_Close_Understand", "Steve",
            "I think I'm actually starting to understand this stuff.", "D5_Close_TerribleCode", 3.0f));
        steps.Add(Line("D5_Close_TerribleCode", "Steve",
            "Which means pretty soon I'll be able to recognize my own terrible code.", "D5_Close_Choices", 3.3f));

        NarrativeStep closeChoices = Step("D5_Close_Choices", NarrativeStepType.PresentChoices);
        closeChoices.choices = new List<DialogueStepChoice>
        {
            Choice("Eventually you'll recognize my terrible code too.", "D5_Close_Friendly"),
            Choice("Let's merge this before either of us has another idea.", "D5_Close_Dry"),
            Choice("Just don't rearrange anything else before Monday.", "D5_Close_Dismissive", 5)
        };
        steps.Add(closeChoices);

        steps.Add(Line("D5_Close_Friendly", "Steve",
            "Ha. Deal.", "D5_End_StevePush", 2.0f));
        steps.Add(Line("D5_Close_Dry", "Steve",
            "Best thing you've said all afternoon.", "D5_End_StevePush", 2.4f));
        steps.Add(Line("D5_Close_Dismissive", "Steve",
            "...Right. Sure.", "D5_End_StevePush", 2.4f));

        // ------------------------------------------------------------------
        // End call
        // ------------------------------------------------------------------
        steps.Add(Line("D5_End_StevePush", "Steve",
            "I'll push the fix.", "D5_End_GreatWeekend", 2.3f));
        steps.Add(Line("D5_End_GreatWeekend", "Me",
            "Awesome. Have a great weekend!", "D5_End_YouToo", 2.5f));
        steps.Add(Line("D5_End_YouToo", "Steve",
            "You too.", "D5_End_RevengeEmail", 1.8f));
        steps.Add(Line("D5_End_RevengeEmail", "Steve",
            "And if the AI emails you demanding revenge, don't answer.", "D5_EndScreenShare", 3.0f));

        steps.Add(Step("D5_EndScreenShare", NarrativeStepType.EndScreenShare, "D5_EndZoom"));
        steps.Add(Step("D5_EndZoom", NarrativeStepType.EndZoomCall, "D5_Internal_Action"));

        // ------------------------------------------------------------------
        // Closing internal monologue
        // ------------------------------------------------------------------
        steps.Add(Line("D5_Internal_Action", "Me",
            "Action. Resolution. Terminal state. Transition.", "D5_Internal_Remember", 2.7f, HeadPersonality.InternalMonologue));
        steps.Add(Line("D5_Internal_Remember", "Me",
            "I remember the pattern.", "D5_Internal_NotEverything", 2.4f, HeadPersonality.InternalMonologue));
        steps.Add(Line("D5_Internal_NotEverything", "Me",
            "Not everything.", "D5_Internal_UsedToBeMine", 2.0f, HeadPersonality.InternalMonologue));
        steps.Add(Line("D5_Internal_UsedToBeMine", "Me",
            "But enough to know it used to be mine.", "D5_Internal_Something", 2.8f, HeadPersonality.InternalMonologue));
        steps.Add(Line("D5_Internal_Something", "Me",
            "That's something.", null, 2.2f, HeadPersonality.InternalMonologue));

        return steps;
    }

    private static NarrativeStep Advice(
        string id,
        string speaker,
        HeadPersonality personality,
        string lowerLevelText,
        string higherLevelText,
        string next,
        float baseDuration)
    {
        NarrativeStep step = Line(id, speaker, "", next, baseDuration, personality);
        step.lineVariants = new List<NarrativeLineVariant>
        {
            new NarrativeLineVariant
            {
                requiredLevel = PersonalityLevel.Novice,
                lineText = lowerLevelText
            },
            new NarrativeLineVariant
            {
                requiredLevel = PersonalityLevel.Intermediate,
                lineText = higherLevelText
            }
        };
        return step;
    }

    private static NarrativeStep Line(
        string id,
        string speaker,
        string text,
        string next,
        float baseDuration,
        HeadPersonality personality = HeadPersonality.None)
    {
        return new NarrativeStep
        {
            stepID = id,
            speakerName = speaker,
            personality = personality,
            lineText = text,
            displayDuration = baseDuration * DurationMultiplier,
            nextStepID = next,
            stepType = NarrativeStepType.DialogueLine
        };
    }

    private static NarrativeStep Step(string id, NarrativeStepType type, string next = null)
    {
        return new NarrativeStep
        {
            stepID = id,
            stepType = type,
            nextStepID = next
        };
    }

    private static DialogueStepChoice Choice(string text, string target, int suspicion = 0)
    {
        return new DialogueStepChoice
        {
            choiceText = text,
            personality = HeadPersonality.None,
            targetStepOnClick = target,
            suspicionChange = suspicion,
            requiredLevel = PersonalityLevel.Novice,
            choiceGroup = ""
        };
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
