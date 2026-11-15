using UnityEngine;
using TMPro;

public class LiveStatsDebugPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statsText;

    private void Update()
    {
        if (GameManager.Instance == null || statsText == null) return;

        statsText.text = 
            $"<b>--- LIVE GAME MANAGER STATS ---</b>\n" +
            $"Day: {GameManager.Instance.CurrentDay} | Step: {GameManager.Instance.CurrentStep}\n\n" +
            $"<color=#4EC9B0>Code Quality: {GameManager.Instance.CodeQuality}</color>\n" +
            $"<color=#FF5555>Steve Suspicion: {GameManager.Instance.SteveSuspicion} / {GameManager.MaxSuspicion}</color>\n\n" +
            $"<b>Personality Stats:</b>\n" +
            $"Architect: {GameManager.Instance.GrandArchitectStat}\n" +
            $"Pragmatist: {GameManager.Instance.PragmatistStat}\n" +
            $"Detective: {GameManager.Instance.DetectiveStat}\n" +
            $"Connectedness: {GameManager.Instance.Connectedness}";
    }

    public void NextDay()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.AdvanceToNextDay();
        ReloadNarrative();
    }

    public void PreviousDay()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentDay <= 1) return;

        GameManager.Instance.setCurrentDay(GameManager.Instance.CurrentDay - 1);
        ReloadNarrative();
    }

    private void ReloadNarrative()
    {
        DayNarrativeExecutor executor = FindAnyObjectByType<DayNarrativeExecutor>();
        if (executor != null)
        {
            executor.LoadAndExecuteDay(GameManager.Instance.CurrentDay);
        }
    }
}