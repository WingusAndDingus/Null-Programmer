using System;
using UnityEngine;

/// <summary>Shared session state: player stats, monitor zoom, and the email reply demo.</summary>
[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsZoomedIn { get; private set; }
    public int GrandArchitectStat { get; private set; }
    public int PragmatistStat { get; private set; }
    public int SentinelStat { get; private set; }
    public int GeniusStat { get; private set; }
    public int DetectiveStat { get; private set; }
    public int SteveSuspicion { get; private set; }
    public int CodeQuality { get; private set; }
    public int Connectedness { get; private set; }
    public bool EmailRead { get; private set; }
    public bool ReplyAvailable { get; private set; }
    public int ReplyStage { get; private set; }
    public bool ReplyCompleted => ReplyStage >= 2;
    public string LastReplyFeedback { get; private set; } = "";
    public event Action<int> SuspicionChanged;
    public event Action SteveCrisis;
    public const int MaxSuspicion = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    public static GameManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            existing.Initialize();
            return Instance;
        }
        return new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake() { Initialize(); }
    private void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            // Keep unrelated components on an authored scene object intact.
            Destroy(this);
            return;
        }
        Instance = this;
        // A persistent manager must be a root object.
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void SetZoomedIn(bool value) { IsZoomedIn = value; }
    public void ChangeGrandArchitectStat(int value) { GrandArchitectStat += value; }
    public void ChangePragmatistStat(int value) { PragmatistStat += value; }
    public void ChangeSentinelStat(int value) { SentinelStat += value; }
    public void ChangeGeniusStat(int value) { GeniusStat += value; }
    public void ChangeDetectiveStat(int value) { DetectiveStat += value; }
    public void ChangeCodeQualityStat(int value) { CodeQuality += value; }
    public void ChangeConnectednessStat(int value) { Connectedness += value; }

    public void ChangeSteveSuspicion(int value)
    {
        int previous = SteveSuspicion;
        SteveSuspicion = (int)Math.Max(0L, Math.Min(MaxSuspicion, (long)previous + value));
        if (previous == SteveSuspicion) return;
        SuspicionChanged?.Invoke(SteveSuspicion);
        if (previous < MaxSuspicion && SteveSuspicion == MaxSuspicion) SteveCrisis?.Invoke();
    }
    public void MarkEmailRead() { EmailRead = true; }
    public void MarkReadEmailClosed() { if (EmailRead && !ReplyCompleted) ReplyAvailable = true; }

    // Expected stage prevents repeated clicks/reopening from applying the same choice twice.
    public bool TryApplyReply(int expectedStage, int suspicionDelta, string feedback)
    {
        if (!ReplyAvailable || ReplyCompleted || ReplyStage != expectedStage) return false;
        ReplyStage++;
        LastReplyFeedback = feedback;
        if (ReplyCompleted) ReplyAvailable = false;
        ChangeSteveSuspicion(suspicionDelta);
        return true;
    }
    // Developer-only UI calls this explicitly; leaves mail read and zoom state alone.
    public void RestartReplyTest()
    {
        ChangeSteveSuspicion(-SteveSuspicion);
        ReplyStage = 0;
        ReplyAvailable = false;
        LastReplyFeedback = "";
    }
}
