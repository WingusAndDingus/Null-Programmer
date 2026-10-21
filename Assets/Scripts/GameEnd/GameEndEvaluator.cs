using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndEvaluator : MonoBehaviour
{
    [Header("UML Assets")]
    public Sprite standardUmlSprite;
    public Sprite bloatedUmlSprite;

    [Header("Ending Text: The Perfectionist Trap")]
    public string successTitle = "Ending: The Perfectionist Trap";
    [TextArea(3, 10)] 
    public string successText = "Steve: 'Your use of OOP kept the code incredibly clean. Because we seperated the project into classes the code is maintainable and reusable.'\n\nResult: You log off at 5:00PM, close your workstation, and enjoy a well earned rest.";

    [Header("Ending Text: Coupled and Deprecated")]
    public string failTitle = "Ending: Coupled and Deprecated";
    [TextArea(3, 10)] 
    public string failText = "Management: 'Steve demonstrated real senior dev talent and upstaged your project. You have clearly lost your edge. You are terminated.'\n\nResult: You let The Pragmatist cram everything into main.cpp, making it unmaintainable.";

    public void TriggerFinalEvaluation()
    {
        // Subscribe to the scene loaded event to inject data once the UI is ready
        SceneManager.sceneLoaded += OnEndScreenLoaded;
        
        // Load the UI scene additively over the desktop
        SceneManager.LoadScene("GameEndScreen", LoadSceneMode.Additive);
    }

    private void OnEndScreenLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameEndScreen")
        {
            SceneManager.sceneLoaded -= OnEndScreenLoaded;

            GameEndManager uiManager = FindAnyObjectByType<GameEndManager>();
            if (uiManager != null)
            {
                InjectEndingData(uiManager);
            }
            else
            {
                Debug.LogError("GameEndManager not found in the loaded scene");
            }
        }
    }

    private void InjectEndingData(GameEndManager uiManager)
    {
        // 1. Fetch current stats from GameManager
        int codeQuality = GameManager.Instance.CodeQuality;
        int suspicion = GameManager.Instance.SteveSuspicion;
        
        // 2. Determine ending from metrics
        bool isPerfectionistTrap = (codeQuality >= 50) && (suspicion < 50);

        // 3. Pass the data to GameEndManager
        if (isPerfectionistTrap)
        {
            // Success: Player UML matches the standard UML template
            uiManager.SetupEndScreen(successTitle, successText, standardUmlSprite, standardUmlSprite);
        }
        else
        {
            uiManager.SetupEndScreen(failTitle, failText, bloatedUmlSprite, standardUmlSprite);
        }
    }
}