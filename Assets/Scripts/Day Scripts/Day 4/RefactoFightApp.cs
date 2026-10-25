using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;

public class RefactoFightApp : MonoBehaviour
{
    private static TaskCompletionSource<bool> narrativeCompletionSource;

    // Called by DayNarrativeExecutor to halt the story until the app is played.
    public static Task<bool> WaitForBattleCompletionAsync(CancellationToken token)
    {
        narrativeCompletionSource = new TaskCompletionSource<bool>();
        token.Register(() => narrativeCompletionSource?.TrySetCanceled());
        return narrativeCompletionSource.Task;
    }

    // Called by the Desktop Icon's OnClick() Event
    public async void LaunchRefactoFight()
    {
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

        // If the story is waiting for this result, alert the executor to continue!
        narrativeCompletionSource?.TrySetResult(playerWon);
        narrativeCompletionSource = null;
    }
}