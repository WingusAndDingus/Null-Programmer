using UnityEngine;

[System.Serializable]
public struct ChallengeDialogueLine
{
    [Header("Speaker Info")]
    public string speakerName;
    public HeadPersonality personality;

    [Header("Dialogue Content")]
    [TextArea(2, 5)]
    public string dialogueText;
}

[CreateAssetMenu(fileName = "NewSteveChallenge", menuName = "Narrative/Steve Coding Challenge")]
public class SteveChallengeData : ScriptableObject
{
    [Header("Call Details")]
    public string challengeTitle = "SFML Window Initialization";
    [TextArea(3, 5)]
    public string steveDialoguePrompt = "Hey, let me see your window setup code. Type it out into the IDE.";

    [Header("Editor Template")]
    [TextArea(6, 15)]
    [Tooltip("The initial template code displayed in the editor when the challenge begins.")]
    public string startingCode = "#include <iostream>\n\nint main()\n{\n    // Type your code here:\n\n    return 0;\n}";

    [Header("Internal Thoughts / Hints / Jokes")]
    [Tooltip("Add as many internal dialogue or hint lines as you want, tied to specific head personalities.")]
    public ChallengeDialogueLine[] internalVoices = new ChallengeDialogueLine[0];

    [Header("Accepted Answers")]
    [Tooltip("Add multiple valid multi-line string variations to cover different student coding styles.")]
    [TextArea(6, 15)]
    public string[] acceptedAnswers = new string[]
    {
        "#include <iostream>\n\nint main()\n{\n    // Expected Answer Example\n    return 0;\n}"
    };
}
