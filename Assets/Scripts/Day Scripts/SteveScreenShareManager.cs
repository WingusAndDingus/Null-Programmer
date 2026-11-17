using System;
using System.Threading.Tasks;
using UnityEngine;

public class SteveScreenShareManager : MonoBehaviour
{
    public static SteveScreenShareManager Instance;

    private TaskCompletionSource<string> activeChallengeTask;

    private void Awake()
    {
        // IMPORTANT: this component is attached to the IDE Window itself.
        // Do NOT detach this GameObject or mark it DontDestroyOnLoad, because doing
        // so would make the entire IDE Window survive monitor scene changes.
        if (Instance != null && Instance != this)
        {
            // Never destroy the whole IDE Window just because a duplicate manager
            // exists. Remove only this duplicate component.
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Opens the challenge interface. Awaits submission from the player (or IDE UI).
    /// </summary>
    public Task<string> PromptPlayerForCodeAsync(SteveChallengeData challenge)
    {
        if (challenge == null)
            throw new ArgumentNullException(nameof(challenge));

        Debug.Log($"[SteveScreenShare] Incoming call from Steve: {challenge.challengeTitle}");
        Debug.Log($"[StevePrompt] \"{challenge.steveDialoguePrompt}\"");

        activeChallengeTask = new TaskCompletionSource<string>();

        // Directly find the IDE controller in the scene (including inactive objects) and open it.
        SteveIdeWindowController ideController =
            FindAnyObjectByType<SteveIdeWindowController>(FindObjectsInactive.Include);

        if (ideController != null)
        {
            ideController.OpenIde(challenge);
        }
        else
        {
            Debug.LogError(
                "[SteveScreenShareManager] Could not find any SteveIdeWindowController in the scene! " +
                "Make sure it is added to your IDE panel.");
        }

        return activeChallengeTask.Task;
    }

    /// <summary>
    /// Call this from your teammate's IDE UI script when the player hits submit.
    /// </summary>
    public void SubmitPlayerCode(string submittedCode)
    {
        if (activeChallengeTask != null)
        {
            activeChallengeTask.TrySetResult(submittedCode);
            activeChallengeTask = null;
        }
    }
}
