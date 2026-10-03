using UnityEngine;

public enum HeadPersonality
{
    None,              // Standard spoken dialogue (Steve, NPC)
    InternalMonologue, // General inner thought
    GrandArchitect,    // OOP / Design patterns
    Pragmatist,        // Fast/Hacky code
    Sentinel,          // Memory safety / Leaks
    Genius,            // Red herring C++ trivia
    Detective          // Amnesia/Past history
}

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public HeadPersonality personality;
    [TextArea(3, 5)] public string text;
    public AudioClip voiceClip; // Optional audio clip for Steve/NPCs
    public float displayDuration = 3.0f; // Default time or click-to-advance
}