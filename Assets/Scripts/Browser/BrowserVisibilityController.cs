using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BrowserVisibilityController : MonoBehaviour
{
    [SerializeField]
    private string[] leftMonitorSceneNames;

    private void Start()
    {
        RefreshVisibility(SceneManager.GetActiveScene());
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshVisibility(newScene);
    }

    private void RefreshVisibility(Scene scene)
    {
        bool shouldShow = false;

        if (leftMonitorSceneNames != null)
        {
            shouldShow = Array.Exists(
                leftMonitorSceneNames,
                sceneName =>
                    string.Equals(
                        sceneName,
                        scene.name,
                        StringComparison.Ordinal
                    )
            );
        }

        BrowserManager.Instance?.SetVisible(shouldShow);
    }
}