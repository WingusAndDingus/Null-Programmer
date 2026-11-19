using UnityEngine;

public class ClickableSceneSwitch : MonoBehaviour
{
    [SerializeField] private SceneSwitcher sceneSwitcher;
    [SerializeField] private string sceneName;

    private void OnMouseDown()
    {
        if (DialogueManager.BlocksMonitorClick() || RefactoFightApp.IsFightActive
            || GameEndManager.IsEndScreenActive || GameEndEvaluator.IsPresentationActive) return;
        sceneSwitcher.LoadScene(sceneName);
    }
}

