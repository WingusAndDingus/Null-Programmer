using UnityEngine;
using UnityEngine.InputSystem; // Using the New Input System

public class BossBattleTester : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current == null) return;

        // Press 'Spacebar' to trigger the boss battle
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Triggering Boss Battle Demo...");
            
            // We use discard (_) because we don't need to wait for the async result in the tester
            _ = RefactoFightManager.Instance.StartBossBattleAsync();
        }
    }
}