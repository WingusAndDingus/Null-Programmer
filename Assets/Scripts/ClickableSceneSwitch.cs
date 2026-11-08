using UnityEngine;

public class ClickableSceneSwitch : MonoBehaviour
{
    [SerializeField] private SceneSwitcher sceneSwitcher;
    [SerializeField] private string sceneName;

    private void OnMouseDown()
    {
        if (DialogueManager.BlocksMonitorClick()) return;
        sceneSwitcher.LoadScene(sceneName);
    }
}
