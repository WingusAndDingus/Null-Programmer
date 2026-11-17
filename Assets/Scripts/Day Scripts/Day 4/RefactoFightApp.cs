using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;

public class RefactoFightApp : MonoBehaviour
{
    private static TaskCompletionSource<bool> narrativeCompletionSource;

    // Tracks whether the "Wait Until Refacto Fight Completed" step has been reached
    private static bool isUnlockedByNarrative = false;

    // Tracks whether the player has already opened RefactoFight in this playthrough
    private static bool hasBeenPlayedThisPlaythrough = false;

    // True while the RefactoFight scene is open
    public static bool IsFightActive { get; private set; } = false;

    // Called by DayNarrativeExecutor to halt the story until the app is played.
    public static Task<bool> WaitForBattleCompletionAsync(CancellationToken token)
    {
        isUnlockedByNarrative = true; // Unlock the app permanently once this step begins
        narrativeCompletionSource = new TaskCompletionSource<bool>();
        token.Register(() => narrativeCompletionSource?.TrySetCanceled());
        return narrativeCompletionSource.Task;
    }

    // Called by the Desktop Icon's OnClick() Event
    public async void LaunchRefactoFight()
    {
        // Block launching if the narrative hasn't reached the wait step yet
        if (!isUnlockedByNarrative)
        {
            Debug.Log("[RefactoFightApp] Cannot open yet: Waiting for Steve to introduce the app.");
            return;
        }

        // Block launching if it has already been opened once during this playthrough
        if (hasBeenPlayedThisPlaythrough)
        {
            Debug.Log("[RefactoFightApp] RefactoFight can only be played once per playthrough.");
            return;
        }

        // Prevent double-clicking and loading the scene twice at the same time
        if (SceneManager.GetSceneByName("RefactoFight").isLoaded) return;

        hasBeenPlayedThisPlaythrough = true;
        IsFightActive = true;

        var loadOp = SceneManager.LoadSceneAsync("RefactoFight", LoadSceneMode.Additive);
        while (!loadOp.isDone) await Task.Yield();

        RefactoFightManager bossManager = FindAnyObjectByType<RefactoFightManager>();
        bool playerWon = false;
        
        if (bossManager != null)
        {
            playerWon = await bossManager.StartBossBattleAsync();
        }

        var unloadOp = SceneManager.UnloadSceneAsync("RefactoFight");
        while (!unloadOp.isDone) await Task.Yield();
        IsFightActive = false;
        
        // If the story is waiting for this result, alert the executor to continue!
        narrativeCompletionSource?.TrySetResult(playerWon);
        narrativeCompletionSource = null;
    }

    public static void ResetAppLock()
    {
        isUnlockedByNarrative = false;
        hasBeenPlayedThisPlaythrough = false;
        narrativeCompletionSource = null;
    }
}