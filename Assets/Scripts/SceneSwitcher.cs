using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public const string CenterMonitorScene = "DesktopUI";

    public static bool IsMonitorScene(string sceneName)
    {
        return sceneName == CenterMonitorScene || sceneName == "Left Monitor" || sceneName == "Right Monitor";
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
