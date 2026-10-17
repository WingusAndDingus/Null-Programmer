#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or refreshes the Day 5 DaySequenceData asset used by the implementation
/// in this package. Put this file under an Editor folder.
/// </summary>
public static class Day5SequenceBuilder
{
    private const string FolderPath = "Assets/Narrative";
    private const string AssetPath = FolderPath + "/DaySequence_Day5.asset";

    [MenuItem("Null Programmer/Narrative/Create or Refresh Day 5 Sequence")]
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

        Debug.Log($"[Day5SequenceBuilder] Day 5 sequence created/refreshed at {AssetPath}");
    }

    private static List<NarrativeStep> BuildSteps()
    {
        var steps = new List<NarrativeStep>();

        // ------------------------------------------------------------------
        // Incoming Friday call
        // ------------------------------------------------------------------
        NarrativeStep start = Step("Start", NarrativeStepType.WaitUntilZoomCall, next: "D5_Steve_GoodNews");
        start.suppressZoomAcceptGreeting = true;
        steps.Add(start);

        steps.Add(Line("D5_Steve_GoodNews", "Steve",
            "Hey. Good news. I cleaned up the turn logic.",
            "D5_Protagonist_BadNews", 2.6f));

        steps.Add(Line("D5_Protagonist_BadNews", "The Protagonist",
            "And the bad news?",
            "D5_Steve_Bug", 2.0f));

        steps.Add(Line("D5_Steve_Bug", "Steve",
            "Sometimes the computer shoots you after you've already won.",
            "D5_Steve_Posthumous", 3.0f));

        steps.Add(Line("D5_Steve_Posthumous", "Steve",
            "I'm calling it \"posthumous retaliation.\"",
            "D5_Internal_Opening", 2.8f));

        steps.Add(Line("D5_Internal_Opening", "The Protagonist",
            "Wonderful. He's debugging something he doesn't understand with someone who can't remember understanding it.",
            "D5_Steve_ExplainChange", 3.6f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_Steve_ExplainChange", "Steve",
            "I moved a few things around. The original ordering looked unnecessarily picky.",
            "D5_StartShare", 3.1f));

        NarrativeStep startShare = Line("D5_StartShare", "Steve",
            "Here, look at what I changed.",
            "D5_ShareDelay", 2.2f);
        startShare.stepType = NarrativeStepType.StartScreenShare;
        startShare.suppressScreenShareVideo = true;
        steps.Add(startShare);

        NarrativeStep shareDelay = Step("D5_ShareDelay", NarrativeStepType.DelaySeconds, next: "D5_ArchitectAdvice");
        shareDelay.delayDuration = 2.0f;
        steps.Add(shareDelay);

        // ------------------------------------------------------------------
        // Interaction 1: broken turn ordering + Grand Architect
        // ------------------------------------------------------------------
        NarrativeStep architect = Line("D5_ArchitectAdvice", "The Grand Architect", string.Empty,
            "D5_BugQuestion", 3.5f, HeadPersonality.GrandArchitect);
        architect.usePersonalitySkillVariant = true;
        architect.highSkillThreshold = 3;
        architect.lowSkillText =
            "Clearly this requires an Input Controller, Turn Controller, State Controller, and perhaps a Controller Controller.";
        architect.highSkillText =
            "Ignore the classes. Trace the sequence: an event occurs, the action resolves, the state changes, and only then should control move elsewhere.";
        steps.Add(architect);

        NarrativeStep bugQuestion = Line("D5_BugQuestion", "Steve",
            "The win check is still there. Why does moving it down break anything?",
            null, 3.2f);
        bugQuestion.stepType = NarrativeStepType.PresentChoices;
        bugQuestion.choices = new List<DialogueStepChoice>
        {
            Choice("Because you've started the AI's turn before checking whether the player's attack ended the match.", "D5_BugCorrect"),
            Choice("executeAITurn() is in the wrong place.", "D5_BugIncomplete"),
            Choice("SFML is probably processing the player and AI attacks at the same time.", "D5_BugWrong", suspicion: 2)
        };
        steps.Add(bugQuestion);

        steps.Add(Line("D5_BugCorrect", "Steve",
            "So the bug isn't that I forgot the win condition. I evaluated it too late.",
            "D5_SFMLSteve", 3.2f));

        steps.Add(Line("D5_BugIncomplete", "Steve",
            "Right, but what's wrong with that place?",
            "D5_BugIncompleteExplain", 2.5f));

        steps.Add(Line("D5_BugIncompleteExplain", "The Protagonist",
            "The previous action hasn't finished resolving yet. We handed control to the AI before checking its consequences.",
            "D5_SFMLSteve", 3.5f));

        steps.Add(Line("D5_BugWrong", "Steve",
            "But the AI doesn't use SFML input.",
            "D5_BugWrongThought", 2.5f));

        steps.Add(Line("D5_BugWrongThought", "The Protagonist",
            "He's asking distressingly good questions today.",
            "D5_SFMLSteve", 2.4f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_SFMLSteve", "Steve",
            "So SFML basically tells us, \"the mouse was clicked.\"",
            "D5_SFMLProtagonist", 2.8f));

        steps.Add(Line("D5_SFMLProtagonist", "The Protagonist",
            "Right. SFML reports the input event. Our game decides whether the click is a legal attack and what happens afterward.",
            "D5_TraceIntro", 3.8f));

        // ------------------------------------------------------------------
        // Mini-game: Trace the Turn
        // ------------------------------------------------------------------
        steps.Add(Line("D5_TraceIntro", "The Protagonist",
            "Input. Resolve the action. Update state. Check whether the match ended. Then hand over the turn.",
            "D5_TraceTurn", 3.4f, HeadPersonality.InternalMonologue));

        NarrativeStep traceTurn = Step("D5_TraceTurn", NarrativeStepType.WaitUntilNarrativeEvent, next: "D5_Steve_CheckEverywhere");
        traceTurn.narrativeEventKey = Day5TraceTurnMiniGame.DefaultCompletionEventKey;
        steps.Add(traceTurn);

        // ------------------------------------------------------------------
        // Interaction 2: terminal-state invariant + Sentinel
        // ------------------------------------------------------------------
        steps.Add(Line("D5_Steve_CheckEverywhere", "Steve",
            "So should I just keep adding if (!game_over) everywhere?",
            "D5_SentinelAdvice", 3.0f));

        NarrativeStep sentinel = Line("D5_SentinelAdvice", "The Sentinel", string.Empty,
            "D5_Protagonist_Invariant", 3.5f, HeadPersonality.Sentinel);
        sentinel.usePersonalitySkillVariant = true;
        sentinel.highSkillThreshold = 3;
        sentinel.lowSkillText =
            "Yes. Check it everywhere. Before attacks. After attacks. During attacks. Perhaps before lunch.";
        sentinel.highSkillText =
            "Think in invariants. Once the match reaches a terminal state, neither player may receive another turn.";
        steps.Add(sentinel);

        steps.Add(Line("D5_Protagonist_Invariant", "The Protagonist",
            "The important rule isn't to check the Boolean everywhere. Once the match enters a terminal state, nothing should transition it into another turn.",
            "D5_Steve_StateTransition", 4.0f));

        steps.Add(Line("D5_Steve_StateTransition", "Steve",
            "So we're defining which state changes are legal.",
            "D5_Protagonist_Exactly", 2.7f));

        steps.Add(Line("D5_Protagonist_Exactly", "The Protagonist",
            "Exactly.",
            "D5_FrameQuestion", 1.8f));

        // ------------------------------------------------------------------
        // Interaction 3: SFML frame loop != gameplay turn
        // ------------------------------------------------------------------
        NarrativeStep frameQuestion = Line("D5_FrameQuestion", "Steve",
            "If Battleship alternates turns, shouldn't the main loop alternate too? Player loop, AI loop, player loop? Why does this keep running while we're waiting?",
            null, 4.2f);
        frameQuestion.stepType = NarrativeStepType.PresentChoices;
        frameQuestion.choices = new List<DialogueStepChoice>
        {
            Choice("Because the SFML frame loop and the Battleship turn system are separate. Frames keep running while the game waits for an action.", "D5_FrameCorrect"),
            Choice("Because SFML needs to keep redrawing the window.", "D5_FramePartial"),
            Choice("Each frame is basically one Battleship turn.", "D5_FrameWrong", suspicion: 2)
        };
        steps.Add(frameQuestion);

        steps.Add(Line("D5_FrameCorrect", "Steve",
            "So we could draw sixty frames without completing a single turn.",
            "D5_FrameJoke", 2.9f));

        steps.Add(Line("D5_FrameJoke", "Steve",
            "Finally, software that accurately simulates meetings.",
            "D5_FinalIntro", 2.7f));

        steps.Add(Line("D5_FramePartial", "The Protagonist",
            "Rendering is part of it, but input and updates keep running too. The important distinction is that a frame isn't a game turn.",
            "D5_FinalIntro", 3.8f));

        steps.Add(Line("D5_FrameWrong", "Steve",
            "At sixty frames per second?",
            "D5_FrameWrongThought", 2.2f));

        steps.Add(Line("D5_FrameWrongThought", "The Protagonist",
            "Competitive Battleship has become considerably more dangerous.",
            "D5_FinalIntro", 2.6f, HeadPersonality.InternalMonologue));

        // ------------------------------------------------------------------
        // Final test + friendship beat
        // ------------------------------------------------------------------
        steps.Add(Line("D5_FinalIntro", "Steve",
            "Okay. Let me run it again.",
            "D5_FinalResult", 2.2f));

        steps.Add(Line("D5_FinalResult", "Steve",
            "It didn't shoot me.",
            "D5_FinalPromising", 2.2f));

        steps.Add(Line("D5_FinalPromising", "The Protagonist",
            "Promising.",
            "D5_FinalJoke", 1.8f));

        steps.Add(Line("D5_FinalJoke", "Steve",
            "We've successfully taught artificial intelligence to recognize when it's dead.",
            "D5_FinalResume", 3.2f));

        steps.Add(Line("D5_FinalResume", "The Protagonist",
            "Put it on the résumé.",
            "D5_FriendshipIntro1", 2.1f));

        steps.Add(Line("D5_FriendshipIntro1", "Steve",
            "I think I'm actually starting to understand this stuff.",
            "D5_FriendshipIntro2", 2.8f));

        NarrativeStep friendship = Line("D5_FriendshipIntro2", "Steve",
            "Which means pretty soon I'll be able to recognize my own terrible code.",
            null, 3.1f);
        friendship.stepType = NarrativeStepType.PresentChoices;
        friendship.choices = new List<DialogueStepChoice>
        {
            Choice("Eventually you'll recognize my terrible code too.", "D5_FriendshipFriendly", friendship: 1),
            Choice("Let's merge this before either of us has another idea.", "D5_FriendshipDry"),
            Choice("Just don't rearrange anything else before Monday.", "D5_FriendshipDismissive", friendship: -1)
        };
        steps.Add(friendship);

        steps.Add(Line("D5_FriendshipFriendly", "Steve",
            "Ha. Deal.",
            "D5_EndShare", 2.0f));

        steps.Add(Line("D5_FriendshipDry", "Steve",
            "Best thing you've said all afternoon.",
            "D5_EndShare", 2.4f));

        steps.Add(Line("D5_FriendshipDismissive", "Steve",
            "...Right. Sure.",
            "D5_EndShare", 2.2f));

        steps.Add(Step("D5_EndShare", NarrativeStepType.EndScreenShare, next: "D5_EndCall"));
        steps.Add(Step("D5_EndCall", NarrativeStepType.EndZoomCall, next: "D5_Closing1"));

        steps.Add(Line("D5_Closing1", "The Protagonist",
            "Action. Resolution. Terminal state. Transition.",
            "D5_Closing2", 2.7f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_Closing2", "The Protagonist",
            "I remember the pattern.",
            "D5_Closing3", 2.4f, HeadPersonality.InternalMonologue));

        steps.Add(Line("D5_Closing3", "The Protagonist",
            "That's something.",
            null, 2.2f, HeadPersonality.InternalMonologue));

        return steps;
    }

    private static NarrativeStep Step(string id, NarrativeStepType type, string next = null)
    {
        return new NarrativeStep
        {
            stepID = id,
            stepType = type,
            nextStepID = next,
            displayDuration = 0f
        };
    }

    private static NarrativeStep Line(
        string id,
        string speaker,
        string text,
        string next,
        float duration,
        HeadPersonality personality = HeadPersonality.None)
    {
        return new NarrativeStep
        {
            stepID = id,
            stepType = NarrativeStepType.DialogueLine,
            speakerName = speaker,
            personality = personality,
            lineText = text,
            displayDuration = duration,
            nextStepID = next
        };
    }

    private static DialogueStepChoice Choice(
        string text,
        string target,
        int suspicion = 0,
        int friendship = 0)
    {
        return new DialogueStepChoice
        {
            choiceText = text,
            personality = HeadPersonality.None,
            targetStepOnClick = target,
            suspicionChange = suspicion,
            friendshipChange = friendship
        };
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
