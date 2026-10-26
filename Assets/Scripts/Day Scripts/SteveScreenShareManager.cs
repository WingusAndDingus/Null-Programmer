using System;
using System.Threading.Tasks;
using UnityEngine;

public class SteveScreenShareManager : MonoBehaviour
{
    public static SteveScreenShareManager Instance;

    private TaskCompletionSource<string> activeChallengeTask;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
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

        // Directly find the IDE controller in the scene (including inactive objects) and open it!
        SteveIdeWindowController ideController = FindAnyObjectByType<SteveIdeWindowController>(FindObjectsInactive.Include);
        if (ideController != null)
        {
            ideController.OpenIde(challenge);
        }
        else
        {
            Debug.LogError("[SteveScreenShareManager] Could not find any SteveIdeWindowController in the scene! Make sure it is added to your IDE panel.");
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
