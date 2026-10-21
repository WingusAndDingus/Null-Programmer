using System;
using UnityEngine;

public class GameplayEvents : MonoBehaviour
{
    public static event Action OnZoomCallStarted;
    public static event Action OnZoomCallEnded;
    public static event Action<string> OnCodeCompiled; // e.g., passes script name or status
    public static event Action OnScreenShareEnded;

    // Small generic hook for narrative interactions such as reading an email
    // or completing a mini-game. The narrative layer decides which key it is waiting for.
    public static event Action<string> OnNarrativeEvent;

    public static void TriggerZoomCallStarted() => OnZoomCallStarted?.Invoke();
    public static void TriggerZoomCallEnded() => OnZoomCallEnded?.Invoke();
    public static void TriggerScreenShareEnded() => OnScreenShareEnded?.Invoke();
    public static void TriggerCodeCompiled(string status) => OnCodeCompiled?.Invoke(status);

    public static void TriggerNarrativeEvent(string eventKey)
    {
        if (string.IsNullOrWhiteSpace(eventKey)) return;
        OnNarrativeEvent?.Invoke(eventKey);
    }
}
