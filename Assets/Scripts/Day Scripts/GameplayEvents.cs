using System;
using UnityEngine;

public class GameplayEvents : MonoBehaviour
{
    public static event Action OnZoomCallStarted;
    public static event Action OnZoomCallEnded;
    public static event Action<string> OnCodeCompiled; // e.g., passes script name or status
    public static event Action OnScreenShareEnded;

    // Generic narrative hook for mini-games and future day-specific interactions.
    // A listener can wait for one named event without adding a new event type to this
    // class every time a new lesson interaction is created.
    public static event Action<string> OnNarrativeEvent;

    public static void TriggerZoomCallStarted() => OnZoomCallStarted?.Invoke();
    public static void TriggerZoomCallEnded() => OnZoomCallEnded?.Invoke();
    public static void TriggerScreenShareEnded() => OnScreenShareEnded?.Invoke();
    public static void TriggerCodeCompiled(string status) => OnCodeCompiled?.Invoke(status);
    public static void TriggerNarrativeEvent(string eventKey) => OnNarrativeEvent?.Invoke(eventKey);
}
