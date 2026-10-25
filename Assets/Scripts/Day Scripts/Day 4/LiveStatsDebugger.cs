using UnityEngine;
using TMPro;

public class LiveStatsDebugger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro component that will display the stats here")]
    public TextMeshProUGUI liveStatsText;

    private void Update()
    {
        // Continuously read the live state of the game every frame
        if (GameManager.Instance != null && liveStatsText != null)
        {
            liveStatsText.text = 
                $"<color=yellow>--- LIVE DEBUG STATS ---</color>\n" +
                $"Day: {GameManager.Instance.CurrentDay}\n" +
                $"Current Step: {GameManager.Instance.CurrentStep}\n\n" +
                $"<color=#4EC9B0>Code Quality: {GameManager.Instance.CodeQuality}</color>\n" +
                $"<color=#ff5555>Suspicion: {GameManager.Instance.SteveSuspicion} / 100</color>\n\n" +
                $"Architect: {GameManager.Instance.GrandArchitectStat}\n" +
                $"Pragmatist: {GameManager.Instance.PragmatistStat}\n" +
                $"Sentinel: {GameManager.Instance.SentinelStat}\n" +
                $"Genius: {GameManager.Instance.GeniusStat}\n" +
                $"Detective: {GameManager.Instance.DetectiveStat}\n" +
                $"Connectedness: {GameManager.Instance.Connectedness}";
        }
    }

    public void NextDay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceToNextDay(); 
            ReloadNarrative();
        }
    }

    public void PreviousDay()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentDay > 1)
        {
            GameManager.Instance.setCurrentDay(GameManager.Instance.CurrentDay - 1); 
            ReloadNarrative();
        }
    }

    private void ReloadNarrative()
    {
        // Find the executor dynamically and force it to load the newly set day
        DayNarrativeExecutor executor = FindAnyObjectByType<DayNarrativeExecutor>();
        if (executor != null)
        {
            executor.LoadAndExecuteDay(GameManager.Instance.CurrentDay); //
        }
        else
        {
            Debug.LogWarning("[LiveStatsDebugger] DayNarrativeExecutor not found in the scene.");
        }
    }
}