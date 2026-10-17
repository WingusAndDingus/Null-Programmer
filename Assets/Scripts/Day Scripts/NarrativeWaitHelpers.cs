using System;
using System.Threading.Tasks;
using UnityEngine;

public class NarrativeWaitHelpers
{
    /// <summary>
    /// Pauses sequence until Steve's Zoom call is accepted.
    /// </summary>
    public static Task WaitForZoomCall()
    {
        var tcs = new TaskCompletionSource<bool>();

        Action handler = null;
        handler = () =>
        {
            GameplayEvents.OnZoomCallStarted -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnZoomCallStarted += handler;
        return tcs.Task;
    }

    /// <summary>
    /// Pauses sequence until code compilation reports a result.
    /// </summary>
    public static Task<string> WaitForCodeCompilation()
    {
        var tcs = new TaskCompletionSource<string>();

        Action<string> handler = null;
        handler = (result) =>
        {
            GameplayEvents.OnCodeCompiled -= handler;
            tcs.TrySetResult(result);
        };

        GameplayEvents.OnCodeCompiled += handler;
        return tcs.Task;
    }

    /// <summary>
    /// Pauses sequence until Steve's screen share ends.
    /// </summary>
    public static Task WaitForScreenShareEnd()
    {
        var tcs = new TaskCompletionSource<bool>();

        Action handler = null;
        handler = () =>
        {
            GameplayEvents.OnScreenShareEnded -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnScreenShareEnded += handler;
        return tcs.Task;
    }

    /// <summary>
    /// Pauses the narrative until a named gameplay/narrative event is fired.
    /// This keeps mini-games decoupled from DayNarrativeExecutor.
    /// </summary>
    public static Task WaitForNarrativeEvent(string expectedEventKey)
    {
        if (string.IsNullOrWhiteSpace(expectedEventKey))
        {
            Debug.LogError("[NarrativeWaitHelpers] Cannot wait for an empty narrative event key.");
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();

        Action<string> handler = null;
        handler = (receivedKey) =>
        {
            if (!string.Equals(receivedKey, expectedEventKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            GameplayEvents.OnNarrativeEvent -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnNarrativeEvent += handler;
        return tcs.Task;
    }
}
