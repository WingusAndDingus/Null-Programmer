using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NarrativeWaitHelpers
{
    /// <summary>
    /// Pauses sequence until Steve's Zoom call is accepted.
    /// </summary>
    public static async Task WaitForZoomCall(CancellationToken token = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        Action handler = null;
        handler = () =>
        {
            GameplayEvents.OnZoomCallStarted -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnZoomCallStarted += handler;
        try
        {
            using (token.Register(() => tcs.TrySetCanceled())) { await tcs.Task; }
        }
        finally { GameplayEvents.OnZoomCallStarted -= handler; }
    }

    /// <summary>
    /// Pauses sequence until code compiles successfully.
    /// </summary>
    public static async Task<string> WaitForCodeCompilation(CancellationToken token = default)
    {
        var tcs = new TaskCompletionSource<string>();

        Action<string> handler = null;
        handler = result =>
        {
            GameplayEvents.OnCodeCompiled -= handler;
            tcs.TrySetResult(result);
        };

        GameplayEvents.OnCodeCompiled += handler;
        try
        {
            using (token.Register(() => tcs.TrySetCanceled())) { return await tcs.Task; }
        }
        finally { GameplayEvents.OnCodeCompiled -= handler; }
    }

    /// <summary>
    /// Pauses sequence until Steve's screen share finishes.
    /// </summary>
    public static async Task WaitForScreenShareEnd(CancellationToken token = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        Action handler = null;
        handler = () =>
        {
            GameplayEvents.OnScreenShareEnded -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnScreenShareEnded += handler;
        try
        {
            using (token.Register(() => tcs.TrySetCanceled())) { await tcs.Task; }
        }
        finally { GameplayEvents.OnScreenShareEnded -= handler; }
    }

    /// <summary>
    /// Waits for one named narrative event. Used by Day 5 for the email and mini-game,
    /// but intentionally generic so later days can reuse it.
    /// </summary>
    public static async Task WaitForNarrativeEvent(string eventKey, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
            throw new ArgumentException("A narrative event key is required.", nameof(eventKey));

        var tcs = new TaskCompletionSource<bool>();

        Action<string> handler = null;
        handler = raisedKey =>
        {
            if (!string.Equals(raisedKey, eventKey, StringComparison.Ordinal)) return;
            GameplayEvents.OnNarrativeEvent -= handler;
            tcs.TrySetResult(true);
        };

        GameplayEvents.OnNarrativeEvent += handler;
        try
        {
            using (token.Register(() => tcs.TrySetCanceled())) { await tcs.Task; }
        }
        finally { GameplayEvents.OnNarrativeEvent -= handler; }
    }
}
