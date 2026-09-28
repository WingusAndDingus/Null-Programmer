using UnityEngine;
using UnityEngine.InputSystem;

public class GameEndTester : MonoBehaviour
{
    [Header("References")]
    public GameEndManager endManager;
    
    [Header("Test Assets")]
    public Sprite standardUMLPlaceholder;
    public Sprite bloatedUMLPlaceholder;

    void Update()
    {
        
        if (Keyboard.current == null) return;

        // Test Perfectionist Trap ending
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            string successTitle = "The Perfectionist Trap";
            string successText = "Steve: 'Your use of OOP kept the code incredibly clean. Because we seperated the project into classes the code is maintainable and reusable.'\n\nResult: You log off at 5:00PM, close your workstation, and enjoy a well earned rest.";
            
            endManager.SetupEndScreen(successTitle, successText, standardUMLPlaceholder, standardUMLPlaceholder);
        }

        // Test Coupled & Bricked ending
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            string failTitle = "Coupled & Bricked";
            string failText = "Management: 'Steve demonstrated real senior dev talent and upstaged your project. You have clearly lost your edge. You are terminated'\n\nResult: You let The Pragmatist cram everything into main.cpp, making it unmaintainable.";
            
            endManager.SetupEndScreen(failTitle, failText, bloatedUMLPlaceholder, standardUMLPlaceholder);
        }
    }
}