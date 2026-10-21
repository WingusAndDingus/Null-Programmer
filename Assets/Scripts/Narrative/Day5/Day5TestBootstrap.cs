using UnityEngine;

/// <summary>
/// Optional development-only helper. Put it in the Day 5 test scene and remove/disable it
/// once normal game progression reaches Day 5.
/// </summary>
[DefaultExecutionOrder(-900)]
public class Day5TestBootstrap : MonoBehaviour
{
    [SerializeField] private bool forceDay5OnPlay = true;

    private void Awake()
    {
        if (!forceDay5OnPlay) return;

        GameManager manager = GameManager.EnsureInstance();
        manager.setCurrentDay(5);
        manager.setStep("Start");
    }
}
