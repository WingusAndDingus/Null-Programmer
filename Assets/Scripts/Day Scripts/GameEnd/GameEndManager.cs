using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

public class GameEndManager : MonoBehaviour
{
    public static bool IsEndScreenActive { get; private set; } = false;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI feedbackText;

    [Header("Full-Screen Viewer")]
    public GameObject fullScreenPanel;
    public Image fullScreenDisplayImage;

    [Header("Settings")]
    public float fadeDuration = 1.5f;

    private Sprite standardUmlSprite;
    private bool showUml = true;

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (fullScreenPanel != null) fullScreenPanel.SetActive(false);
    }

    
    public void SetupEndScreen(string title, string feedback, Sprite standardUML, Color titleColor,
        bool showUml = true)
    {
        IsEndScreenActive = true;
        
        endingTitleText.text = title;
        endingTitleText.color = titleColor; 
        feedbackText.text = feedback;
        
        standardUmlSprite = standardUML;
        this.showUml = showUml;
        SetUmlButtonVisible(showUml);

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

    private void SetUmlButtonVisible(bool visible)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == this &&
                    button.onClick.GetPersistentMethodName(i) == nameof(OpenSteveUML))
                {
                    button.gameObject.SetActive(visible);
                    break;
                }
            }
        }
    }

    // --- Button Methods ---

    public void OpenSteveUML()
    {
        if (!showUml) return;

        fullScreenDisplayImage.sprite = standardUmlSprite;
        fullScreenPanel.SetActive(true);
    }

    public void CloseUMLViewer()
    {
        fullScreenPanel.SetActive(false);
    }

    public void RecompileGame()
    {
        IsEndScreenActive = false;
        RefactoFightApp.ResetAppLock();
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

    private void OnDestroy()
    {
        IsEndScreenActive = false;
    }
}
