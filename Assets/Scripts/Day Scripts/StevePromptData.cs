using UnityEngine;

[CreateAssetMenu(fileName = "NewStevePrompt", menuName = "Narrative/Steve Screenshare Prompt")]
public class StevePromptData : ScriptableObject
{
    [Header("Call Info")]
    public string promptTitle = "SFML Setup Check";
    [TextArea(3, 5)]
    public string steveDialoguePrompt = "Hey, let me see your window initialization code. Type it out so I know you didn't just copy-paste.";

    [Header("Expected Code")]
    [Tooltip("The exact line(s) of code the player needs to type, or key tokens required.")]
    public string targetCodeSnippet = "sf::RenderWindow window(sf::VideoMode(800, 600), \"SFML Game\");";

    [Header("Validation Rules")]
    [Tooltip("If true, requires exact character match. If false, checks for key structural keywords.")]
    public bool strictMatch = false;
    public string[] requiredKeywords = new string[] { "sf::RenderWindow", "VideoMode" };
}
