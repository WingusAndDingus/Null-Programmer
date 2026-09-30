using UnityEngine;

public enum Personality
{
    Architect,
    Pragmatist,
    Sentinel,
    Genius,
    Detective
}

[CreateAssetMenu(menuName = "Null Programmer/Personality")]
public class PersonalityDefinition : ScriptableObject
{
    public Personality personality;
    public string displayName;
    [TextArea] public string description;
    [TextArea] public string[] levelUnlocks = new string[4];
}
