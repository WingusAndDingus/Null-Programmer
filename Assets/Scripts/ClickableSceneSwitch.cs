using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickableSceneSwitch : MonoBehaviour
{
    [SerializeField] private SceneSwitcher sceneSwitcher;
    [SerializeField] private string sceneName;

    private void OnMouseDown()
    {
        sceneSwitcher.LoadScene(sceneName);
    }
}