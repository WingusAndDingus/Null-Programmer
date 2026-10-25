using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared session state: player stats, monitor zoom, window state, and the email reply demo.
/// </summary>

public enum WeekendChoice
{
    Steve,
    Study,
    PersonalConnections,
    Wallow
}

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // =========================================================
    // Camera Zoom
    // =========================================================

    public bool IsZoomedIn { get; private set; }


    // =========================================================
    // Player / Game Stats
    // =========================================================
    public int CurrentDay { get; private set; } = 1;
    public string CurrentStep { get; private set; } = "Start";
    public int GrandArchitectStat { get; private set; }
    public int PragmatistStat { get; private set; }
    public int SentinelStat { get; private set; }
    public int GeniusStat { get; private set; }
    public int DetectiveStat { get; private set; }
    public int SteveSuspicion { get; private set; }

    public int CodeQuality { get; private set; }
    public int Connectedness { get; private set; }

    public const int MaxSuspicion = 100;

    public event Action<int> SuspicionChanged;
    public event Action SteveCrisis;

    // =========================================================
    // Weekend Choice
    // =========================================================

    public WeekendChoice? SelectedWeekendChoice { get; private set; }

    public bool HasSelectedWeekendChoice => SelectedWeekendChoice.HasValue;

    // =========================================================
    // Weekend Choice Availability
    // =========================================================

    public bool CanSpendWeekendWithSteve()
    {
        return SteveSuspicion < 50;
    }

    public bool CanStudyThisWeekend()
    {
        return GrandArchitectStat > 3;
    }

    public bool CanReconnectThisWeekend()
    {
        return Connectedness > 50;
    }

    public bool CanWallowThisWeekend()
    {
        return true;
    }

    public bool IsWeekendChoiceAvailable(WeekendChoice choice)
    {
        switch (choice)
        {
            case WeekendChoice.Steve:
                return CanSpendWeekendWithSteve();

            case WeekendChoice.Study:
                return CanStudyThisWeekend();

            case WeekendChoice.PersonalConnections:
                return CanReconnectThisWeekend();

            case WeekendChoice.Wallow:
                return CanWallowThisWeekend();

            default:
                return false;
        }
    }

    public bool TryConfirmWeekendChoice(WeekendChoice choice)
    {
        // Weekend choice has already been permanently committed.
        if (HasSelectedWeekendChoice)
        {
            return false;
        }

        // Protect against trying to select a locked path.
        if (!IsWeekendChoiceAvailable(choice))
        {
            return false;
        }

        SelectedWeekendChoice = choice;
        return true;
    }


    // =========================================================
    // Email Reply Demo / Day 5 Mail State
    // =========================================================

    public bool EmailRead { get; private set; }
    public bool Day5EmailRead { get; private set; }
    public bool ReplyAvailable { get; private set; }
    public int ReplyStage { get; private set; }
    public bool ReplyCompleted => ReplyStage >= 2;
    public string LastReplyFeedback { get; private set; } = "";


    // =========================================================
    // Window State
    // =========================================================

    private class WindowState
    {
        public bool IsOpen = true;
        public bool IsMaximized = false;

        public bool HasPosition = false;
        public Vector2 Position;

        public bool HasStackOrder = false;
        public int StackOrder;
    }

    private readonly Dictionary<string, WindowState> windowStates =
        new Dictionary<string, WindowState>();

    private int nextWindowStackOrder = 0;


    // =========================================================
    // Singleton / Lifetime
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    public static GameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindAnyObjectByType<GameManager>();

        if (existing != null)
        {
            existing.Initialize();
            return Instance;
        }

        return new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =========================================================
    // Camera Zoom
    // =========================================================

    public void SetZoomedIn(bool value)
    {
        IsZoomedIn = value;
    }


    // =========================================================
    // Player / Game Stats
    // =========================================================
    public int UnspentSkillPoints { get; private set; } = 0;
    private readonly HashSet<string> rewardedMilestones = new HashSet<string>();
    public void AwardSkillPoint(string milestoneID)
    {
        if (string.IsNullOrWhiteSpace(milestoneID) || rewardedMilestones.Contains(milestoneID))
        {
            return;
        }

        rewardedMilestones.Add(milestoneID);
        UnspentSkillPoints++;
        Debug.Log($"[GameManager] Skill point awarded for '{milestoneID}'. Total unspent: {UnspentSkillPoints}");
    }

    public void SpendSkillPoint()
    {
        UnspentSkillPoints = Mathf.Max(0, UnspentSkillPoints - 1);
    }
    public void setCurrentDay(int day)
    {
        CurrentDay = Mathf.Max(1, day);
    }

    public void setStep(string stepName)
    {
        CurrentStep = stepName;
    }

    public void AdvanceToNextDay()
    {
        CurrentDay++;
        UnspentSkillPoints = 0;
        CurrentStep = "Start"; //reset for new day
    }

    public void ChangeGrandArchitectStat(int value)
    {
        GrandArchitectStat += value;
    }

    public void ChangePragmatistStat(int value)
    {
        PragmatistStat += value;
    }

    public void ChangeSentinelStat(int value)
    {
        SentinelStat += value;
    }

    public void ChangeGeniusStat(int value)
    {
        GeniusStat += value;
    }

    public void ChangeDetectiveStat(int value)
    {
        DetectiveStat += value;
    }

    public void ChangeCodeQualityStat(int value)
    {
        CodeQuality += value;
    }

    public void ChangeConnectednessStat(int value)
    {
        Connectedness += value;
    }

    public void ChangeSteveSuspicion(int value)
    {
        int previous = SteveSuspicion;

        SteveSuspicion = (int)Math.Max(
            0L,
            Math.Min(MaxSuspicion, (long)previous + value)
        );

        if (previous == SteveSuspicion)
        {
            return;
        }

        SuspicionChanged?.Invoke(SteveSuspicion);

        if (previous < MaxSuspicion && SteveSuspicion == MaxSuspicion)
        {
            SteveCrisis?.Invoke();
        }
    }

    // =========================================================
    // Email Reply Demo / Day 5 Mail State
    // =========================================================

    public void MarkEmailRead()
    {
        EmailRead = true;
    }

    public void MarkDay5EmailRead()
    {
        Day5EmailRead = true;
    }

    public void MarkReadEmailClosed()
    {
        if (EmailRead && !ReplyCompleted)
        {
            ReplyAvailable = true;
        }
    }

    public bool TryApplyReply(
        int expectedStage,
        int suspicionDelta,
        string feedback
    )
    {
        if (!ReplyAvailable ||
            ReplyCompleted ||
            ReplyStage != expectedStage)
        {
            return false;
        }

        ReplyStage++;
        LastReplyFeedback = feedback;

        if (ReplyCompleted)
        {
            ReplyAvailable = false;
        }

        ChangeSteveSuspicion(suspicionDelta);
        return true;
    }

    public void RestartReplyTest()
    {
        ChangeSteveSuspicion(-SteveSuspicion);
        ReplyStage = 0;
        ReplyAvailable = false;
        LastReplyFeedback = "";
    }


    // =========================================================
    // Window State Helpers
    // =========================================================

    private WindowState GetWindowState(string windowId)
    {
        if (!windowStates.TryGetValue(windowId, out WindowState state))
        {
            state = new WindowState();
            windowStates[windowId] = state;
        }

        return state;
    }


    // =========================================================
    // Maximized State
    // =========================================================

    public void SetWindowMaximized(string windowId, bool maximized)
    {
        GetWindowState(windowId).IsMaximized = maximized;
    }

    public bool IsWindowMaximized(string windowId)
    {
        return GetWindowState(windowId).IsMaximized;
    }


    // =========================================================
    // Open / Closed State
    // =========================================================

    public void SetWindowOpen(string windowId, bool isOpen)
    {
        GetWindowState(windowId).IsOpen = isOpen;
    }

    public bool IsWindowOpen(string windowId)
    {
        return GetWindowState(windowId).IsOpen;
    }


    // =========================================================
    // Window Position
    // =========================================================

    public void SetWindowPosition(string windowId, Vector2 normalizedPosition)
    {
        WindowState state = GetWindowState(windowId);

        state.Position = normalizedPosition;
        state.HasPosition = true;
    }

    public bool TryGetWindowPosition(
        string windowId,
        out Vector2 normalizedPosition
    )
    {
        WindowState state = GetWindowState(windowId);

        if (state.HasPosition)
        {
            normalizedPosition = state.Position;
            return true;
        }

        normalizedPosition = default;
        return false;
    }


    // =========================================================
    // Window Stack Order
    // =========================================================

    public int GetOrCreateWindowStackOrder(string windowId)
    {
        WindowState state = GetWindowState(windowId);

        if (!state.HasStackOrder)
        {
            state.StackOrder = nextWindowStackOrder;
            state.HasStackOrder = true;
            nextWindowStackOrder++;
        }

        return state.StackOrder;
    }

    public void BringWindowToFront(string windowId)
    {
        WindowState state = GetWindowState(windowId);

        state.StackOrder = nextWindowStackOrder;
        state.HasStackOrder = true;
        nextWindowStackOrder++;
    }
}
