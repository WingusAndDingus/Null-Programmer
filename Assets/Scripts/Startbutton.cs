using UnityEngine;
using UnityEngine.SceneManagement;

public class Startbutton : MonoBehaviour
{
    [Header("Alpha Settings")]
    [Range(0f, 1f)] public float defaultAlpha = 0.5f;
    [Range(0f, 1f)] public float hoverAlpha = 1.0f;

    [Header("Scene Settings")]
    public string sceneToLoad;

    [HideInInspector] public bool canInteract = true; // Controlled by WakeUpSequence

    private SpriteRenderer[] childRenderers;

    private void Awake()
    {
        childRenderers = GetComponentsInChildren<SpriteRenderer>();
        SetGroupAlpha(defaultAlpha);
    }

    public void SetHovered(bool isHovered)
    {
        if (!canInteract) return;
        SetGroupAlpha(isHovered ? hoverAlpha : defaultAlpha);
    }

    public void ChangeScene()
    {
        if (!canInteract) return;

        Debug.Log("Change Scene called. Attempting to load " + sceneToLoad);
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void SetGroupAlpha(float alpha)
    {
        foreach (SpriteRenderer sr in childRenderers)
        {
            if (sr.name != "listener")
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}
