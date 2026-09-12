using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Startbutton : MonoBehaviour
{
    public void Start()
    {
           // For Debugging. I'm not sure why it's not working
        if (EventSystem.current == null)
        {
            Debug.LogWarning("No active EventSystem found in the scene!");
        }
    }
    [Header("Alpha Settings")]
    [Range(0f, 1f)] public float defaultAlpha = 0.5f;
    [Range(0f, 1f)] public float hoverAlpha = 1.0f;

    [Header("Scene Settings")]
    public string sceneToLoad;

    private SpriteRenderer[] childRenderers;

    private void Awake()
    {
        childRenderers = GetComponentsInChildren<SpriteRenderer>();
        SetGroupAlpha(defaultAlpha);
    }

    public void SetHovered(bool isHovered)
    {
        SetGroupAlpha(isHovered ? hoverAlpha : defaultAlpha);
    }

    public void ChangeScene()
    {
        //why doesnt it change
        Debug.Log("Change Scene called. Attempting to load " +  sceneToLoad);
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void SetGroupAlpha(float alpha)
    {
        foreach (SpriteRenderer sr in childRenderers)
        {
            if (sr.name != "listener") {
                //for start game text TMP (might implement later)
                //if (sr.name == "Canvas") { sr.GetComponentInChildren<TextMeshPro>()}
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}
