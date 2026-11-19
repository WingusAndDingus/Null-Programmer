using UnityEngine;

public class ClickableSceneSwitch : MonoBehaviour
{
    [SerializeField] private SceneSwitcher sceneSwitcher;
    [SerializeField] private string sceneName;

    private void OnMouseDown()
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.CurrentDay == 7 && SceneSwitcher.IsMonitorScene(sceneName)) return;

        if (DialogueManager.BlocksMonitorClick() || RefactoFightApp.IsFightActive
            || GameEndManager.IsEndScreenActive || GameEndEvaluator.IsPresentationActive) return;
        sceneSwitcher.LoadScene(sceneName);
    }
}
