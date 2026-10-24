using UnityEngine;

[CreateAssetMenu(fileName = "NewSteveChallenge", menuName = "Narrative/Steve Coding Challenge")]
public class SteveChallengeData : ScriptableObject
{
    [Header("Call Details")]
    public string challengeTitle = "SFML Window Initialization";
    [TextArea(3, 5)]
    public string steveDialoguePrompt = "Hey, let me see your window setup code. Type it out into the IDE.";

    [Header("Accepted Answers")]
    [Tooltip("Add multiple valid string variations to cover different student coding styles.")]
    public string[] acceptedAnswers = new string[]
    {
        "sf::RenderWindow window(sf::VideoMode(800, 600), \"SFML Game\");",
        "sf::RenderWindow window(sf::VideoMode(800,600), \"SFML Game\");",
        "RenderWindow window(VideoMode(800, 600), \"SFML Game\");"
    };
}
