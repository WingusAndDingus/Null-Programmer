using System;
using UnityEngine;

public class GameplayEvents : MonoBehaviour
{
    public static event Action OnZoomCallStarted;
    public static event Action OnZoomCallEnded;
    public static event Action<string> OnCodeCompiled; // e.g., passes script name or status

    public static void TriggerZoomCallStarted() => OnZoomCallStarted?.Invoke();
    public static void TriggerZoomCallEnded() => OnZoomCallEnded?.Invoke();
    public static event Action OnScreenShareEnded;

    public static void TriggerScreenShareEnded() => OnScreenShareEnded?.Invoke();
    public static void TriggerCodeCompiled(string status) => OnCodeCompiled?.Invoke(status);
}
