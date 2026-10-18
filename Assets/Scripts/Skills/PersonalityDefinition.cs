using UnityEngine;

public enum Personality
{
    Architect,
    Pragmatist,
    Sentinel,
    Genius,
    Detective
}

public enum PersonalityLevel
{
    Novice = 0,
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

[CreateAssetMenu(menuName = "Null Programmer/Personality")]
public class PersonalityDefinition : ScriptableObject
{
    public Personality personality;
    public string displayName;
    [TextArea] public string description;
    [TextArea] public string[] levelUnlocks = new string[4];
}
