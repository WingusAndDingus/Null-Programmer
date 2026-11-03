using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

public class GameEndManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI feedbackText;

    [Header("Full-Screen Viewer")]
    public GameObject fullScreenPanel;
    public Image fullScreenDisplayImage;

    [Header("Settings")]
    public float fadeDuration = 1.5f;

    private Sprite playerUmlSprite;
    private Sprite standardUmlSprite;

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (fullScreenPanel != null) fullScreenPanel.SetActive(false);
    }

    
    public void SetupEndScreen(string title, string feedback, Sprite playerUML, Sprite standardUML, Color titleColor)
    {
        endingTitleText.text = title;
        endingTitleText.color = titleColor; 
        feedbackText.text = feedback;
        
        playerUmlSprite = playerUML;
        standardUmlSprite = standardUML;

        if (canvasGroup != null) StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    // --- Button Methods ---

    public void OpenPlayerUML()
    {
        fullScreenDisplayImage.sprite = playerUmlSprite;
        fullScreenPanel.SetActive(true);
    }

    public void OpenSteveUML()
    {
        fullScreenDisplayImage.sprite = standardUmlSprite;
        fullScreenPanel.SetActive(true);
    }

    public void CloseUMLViewer()
    {
        fullScreenPanel.SetActive(false);
    }

    public void RecompileGame()
    {
        // 1. Destroy the current GameManager
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        // 2. Wipe PlayerPrefs to completely reset all saved Personality Skill points
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 3. Restart the game
        SceneManager.LoadScene("StartScreen", LoadSceneMode.Single);
    }
}