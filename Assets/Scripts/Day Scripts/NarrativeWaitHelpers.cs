using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NarrativeWaitHelpers
{
    /// <summary>
    /// Pauses sequence until Steve's Zoom call is triggered.
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
        handler = (result) =>
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
    /// Pauses sequence until Steve's screen share video finishes.
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
}
