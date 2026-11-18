using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteveScreenShareManager : MonoBehaviour
{
    public static SteveScreenShareManager Instance { get; private set; }

    // These values intentionally outlive the DesktopUI scene, but the visual IDE
    // and this MonoBehaviour do not. A fresh controller is used after returning.
    private static TaskCompletionSource<string> activeChallengeTask;
    private static SteveChallengeData pendingChallenge;
    private static string savedCode;
    private static int savedCursor;
    private static bool hasSavedCode;

    public static bool HasPendingChallenge =>
        activeChallengeTask != null && !activeChallengeTask.Task.IsCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (HasPendingChallenge)
            StartCoroutine(RestoreChallengeNextFrame());
    }

    private IEnumerator RestoreChallengeNextFrame()
    {
        yield return null; // SteveIdeWindowController.Start closes the initial panel.
        if (!HasPendingChallenge) yield break;
        var ide = FindAnyObjectByType<SteveIdeWindowController>(FindObjectsInactive.Include);
        if (ide == null) yield break;
        ide.OpenIde(pendingChallenge);
        if (hasSavedCode && ide.CodeTyping != null)
            ide.CodeTyping.RestoreEditing(savedCode, savedCursor);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        if (HasPendingChallenge)
        {
            var ide = FindAnyObjectByType<SteveIdeWindowController>(FindObjectsInactive.Include);
            if (ide != null && ide.CodeTyping != null)
            {
                savedCode = ide.CodeTyping.CurrentCode;
                savedCursor = ide.CodeTyping.CursorIndex;
                hasSavedCode = true;
            }
        }
        Instance = null;
    }

    public static void SaveEditingState(string code, int cursor)
    {
        if (!HasPendingChallenge)
            return;

        savedCode = code;
        savedCursor = cursor;
        hasSavedCode = true;
    }

    public Task<string> PromptPlayerForCodeAsync(SteveChallengeData challenge)
    {
        if (challenge == null) throw new ArgumentNullException(nameof(challenge));
        if (HasPendingChallenge)
            return activeChallengeTask.Task;

        pendingChallenge = challenge;
        savedCode = null;
        savedCursor = 0;
        hasSavedCode = false;
        activeChallengeTask = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var ide = FindAnyObjectByType<SteveIdeWindowController>(FindObjectsInactive.Include);
        if (ide != null) ide.OpenIde(challenge);
        else Debug.LogError("[SteveScreenShareManager] Missing scene IDE controller.");
        return activeChallengeTask.Task;
    }

    public void SubmitPlayerCode(string submittedCode)
    {
        if (!HasPendingChallenge) return;
        var task = activeChallengeTask;
        activeChallengeTask = null;
        pendingChallenge = null;
        savedCode = null;
        hasSavedCode = false;
        task.TrySetResult(submittedCode);
    }

    // Call when starting a completely new game if your title screen supports
    // returning to the title without exiting the application.
    public static void ResetChallenge()
    {
        activeChallengeTask?.TrySetCanceled();
        activeChallengeTask = null;
        pendingChallenge = null;
        savedCode = null;
        hasSavedCode = false;
    }
}
