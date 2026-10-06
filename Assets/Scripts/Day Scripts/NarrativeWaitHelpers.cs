using System;
using System.Threading.Tasks;
using UnityEngine;

public class NarrativeWaitHelpers
{
    /// <summary>
    /// Pauses sequence until Steve's Zoom call is triggered.
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
    /// Pauses sequence until code compiles successfully.
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
}
