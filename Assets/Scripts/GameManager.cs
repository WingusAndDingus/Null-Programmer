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
    public event Action<int> DayChanged; //Syed's new implementation
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
        bool result = SteveSuspicion < 50;
        Debug.Log($"[GameManager] CanSpendWeekendWithSteve evaluated. SteveSuspicion: {SteveSuspicion}, Result: {result}");
        return result;
    }

    public bool CanStudyThisWeekend()
    {
        bool result = GrandArchitectStat > 3;
        Debug.Log($"[GameManager] CanStudyThisWeekend evaluated. GrandArchitectStat: {GrandArchitectStat}, Result: {result}");
        return result;
    }

    public bool CanReconnectThisWeekend()
    {
        bool result = Connectedness > 50;
        Debug.Log($"[GameManager] CanReconnectThisWeekend evaluated. Connectedness: {Connectedness}, Result: {result}");
        return result;
    }

    public bool CanWallowThisWeekend()
    {
        Debug.Log("[GameManager] CanWallowThisWeekend evaluated. Result: true");
        return true;
    }

    public bool IsWeekendChoiceAvailable(WeekendChoice choice)
    {
        Debug.Log($"[GameManager] IsWeekendChoiceAvailable called for choice: {choice}");
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
        Debug.Log($"[GameManager] TryConfirmWeekendChoice attempted with choice: {choice}, HasSelectedBefore: {HasSelectedWeekendChoice}");
        // Weekend choice has already been permanently committed.
        if (HasSelectedWeekendChoice)
        {
            Debug.Log("[GameManager] TryConfirmWeekendChoice failed: Weekend choice already committed.");
            return false;
        }

        // Protect against trying to select a locked path.
        if (!IsWeekendChoiceAvailable(choice))
        {
            Debug.Log($"[GameManager] TryConfirmWeekendChoice failed: Choice {choice} is not available.");
            return false;
        }

        SelectedWeekendChoice = choice;
        Debug.Log($"[GameManager] TryConfirmWeekendChoice succeeded. Selected weekend choice set to: {choice}");
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
        Debug.Log("[GameManager] ResetStatics invoked.");
        Instance = null;
    }

    public static GameManager EnsureInstance()
    {
        Debug.Log("[GameManager] EnsureInstance called.");
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindAnyObjectByType<GameManager>();

        if (existing != null)
        {
            Debug.Log("[GameManager] Existing GameManager found in scene. Initializing.");
            existing.Initialize();
            return Instance;
        }

        Debug.Log("[GameManager] No GameManager found. Creating new GameObject.");
        return new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        Debug.Log("[GameManager] Awake called.");
        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("[GameManager] Initialize called. Resetting session parameters.");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Duplicate GameManager instance detected. Destroying duplicate.");
            Destroy(this);
            return;
        }

        Instance = this;
        // Ensure a fresh game session always starts at Day 1 with clean stats
        CurrentDay = 1;
        CurrentStep = "Start";
        SteveSuspicion = 0;
        UnspentSkillPoints = 0;
        SelectedWeekendChoice = null;
        rewardedMilestones.Clear();

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log("[GameManager] OnDestroy called.");
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
        Debug.Log($"[GameManager] SetZoomedIn called. New state: {value}");
        IsZoomedIn = value;
    }


    // =========================================================
    // Player / Game Stats
    // =========================================================
    public int UnspentSkillPoints { get; private set; } = 0;
    private readonly HashSet<string> rewardedMilestones = new HashSet<string>();
    public void AwardSkillPoint(string milestoneID)
    {
        Debug.Log($"[GameManager] AwardSkillPoint called with milestoneID: '{milestoneID}'");
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
        Debug.Log($"[GameManager] SpendSkillPoint called. Unspent before: {UnspentSkillPoints}");
        UnspentSkillPoints = Mathf.Max(0, UnspentSkillPoints - 1);
        Debug.Log($"[GameManager] Unspent skill points after spend: {UnspentSkillPoints}");
    }
    public void setCurrentDay(int day)
    {
        int previous = CurrentDay;
        CurrentDay = Mathf.Max(1, day);
        Debug.Log($"[GameManager] setCurrentDay called. Previous: {previous}, New CurrentDay: {CurrentDay}");
        if (previous != CurrentDay)
        {
            DayChanged?.Invoke(CurrentDay); //Syed's new implementation
        }
    }

    public void setStep(string stepName)
    {
        Debug.Log($"[GameManager] setStep called. Previous step: {CurrentStep}, New step: {stepName}");
        CurrentStep = stepName;
    }

    public void AdvanceToNextDay()
    {
        Debug.Log($"[GameManager] AdvanceToNextDay called. Incrementing from day {CurrentDay}");
        CurrentDay++;
        UnspentSkillPoints = 0;
        CurrentStep = "Start"; //reset for new day
        DayChanged?.Invoke(CurrentDay); //Syed's new implementation
        Debug.Log($"[GameManager] Advanced to Day {CurrentDay}. Step reset to 'Start'.");
    }

    public void ChangeGrandArchitectStat(int value)
    {
        GrandArchitectStat += value;
        Debug.Log($"[GameManager] ChangeGrandArchitectStat delta: {value}, Total GrandArchitectStat: {GrandArchitectStat}");
    }

    public void ChangePragmatistStat(int value)
    {
        PragmatistStat += value;
        Debug.Log($"[GameManager] ChangePragmatistStat delta: {value}, Total PragmatistStat: {PragmatistStat}");
    }

    public void ChangeSentinelStat(int value)
    {
        SentinelStat += value;
        Debug.Log($"[GameManager] ChangeSentinelStat delta: {value}, Total SentinelStat: {SentinelStat}");
    }

    public void ChangeGeniusStat(int value)
    {
        GeniusStat += value;
        Debug.Log($"[GameManager] ChangeGeniusStat delta: {value}, Total GeniusStat: {GeniusStat}");
    }

    public void ChangeDetectiveStat(int value)
    {
        DetectiveStat += value;
        Debug.Log($"[GameManager] ChangeDetectiveStat delta: {value}, Total DetectiveStat: {DetectiveStat}");
    }

    public void ChangeCodeQualityStat(int value)
    {
        CodeQuality += value;
        Debug.Log($"[GameManager] ChangeCodeQualityStat delta: {value}, Total CodeQuality: {CodeQuality}");
    }

    public void ChangeConnectednessStat(int value)
    {
        Connectedness += value;
        Debug.Log($"[GameManager] ChangeConnectednessStat delta: {value}, Total Connectedness: {Connectedness}");
    }

    public void ChangeSteveSuspicion(int value)
    {
        int previous = SteveSuspicion;

        SteveSuspicion = (int)Math.Max(
            0L,
            Math.Min(MaxSuspicion, (long)previous + value)
        );

        Debug.Log($"[GameManager] ChangeSteveSuspicion delta: {value}. Previous: {previous}, New SteveSuspicion: {SteveSuspicion} (Max: {MaxSuspicion})");

        if (previous == SteveSuspicion)
        {
            return;
        }

        SuspicionChanged?.Invoke(SteveSuspicion);

        if (previous < MaxSuspicion && SteveSuspicion == MaxSuspicion)
        {
            Debug.LogWarning("[GameManager] SteveSuspicion reached MAX! Triggering SteveCrisis.");
            SteveCrisis?.Invoke();
        }
    }

    // =========================================================
    // Email Reply Demo / Day 5 Mail State
    // =========================================================

    public void MarkEmailRead()
    {
        EmailRead = true;
        Debug.Log("[GameManager] MarkEmailRead called. EmailRead set to true.");
    }

    public void MarkDay5EmailRead()
    {
        Day5EmailRead = true;
        Debug.Log("[GameManager] MarkDay5EmailRead called. Day5EmailRead set to true.");
    }

    public void MarkReadEmailClosed()
    {
        Debug.Log($"[GameManager] MarkReadEmailClosed called. EmailRead: {EmailRead}, ReplyCompleted: {ReplyCompleted}");
        if (EmailRead && !ReplyCompleted)
        {
            ReplyAvailable = true;
            Debug.Log("[GameManager] ReplyAvailable set to true.");
        }
    }

    public bool TryApplyReply(
        int expectedStage,
        int suspicionDelta,
        string feedback
    )
    {
        Debug.Log($"[GameManager] TryApplyReply called. ExpectedStage: {expectedStage}, Current ReplyStage: {ReplyStage}, ReplyAvailable: {ReplyAvailable}, ReplyCompleted: {ReplyCompleted}");
        if (!ReplyAvailable ||
            ReplyCompleted ||
            ReplyStage != expectedStage)
        {
            Debug.Log("[GameManager] TryApplyReply rejected.");
            return false;
        }

        ReplyStage++;
        LastReplyFeedback = feedback;
        Debug.Log($"[GameManager] TryApplyReply applied successfully. New ReplyStage: {ReplyStage}, Feedback: '{feedback}'");

        if (ReplyCompleted)
        {
            ReplyAvailable = false;
            Debug.Log("[GameManager] ReplyCompleted reached. ReplyAvailable set to false.");
        }

        ChangeSteveSuspicion(suspicionDelta);
        return true;
    }

    public void RestartReplyTest()
    {
        Debug.Log("[GameManager] RestartReplyTest called. Resetting reply state and clearing suspicion.");
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
        Debug.Log($"[GameManager] GetWindowState queried for windowId: '{windowId}'");
        if (!windowStates.TryGetValue(windowId, out WindowState state))
        {
            state = new WindowState();
            windowStates[windowId] = state;
            Debug.Log($"[GameManager] Created new WindowState record for windowId: '{windowId}'");
        }

        return state;
    }


    // =========================================================
    // Maximized State
    // =========================================================

    public void SetWindowMaximized(string windowId, bool maximized)
    {
        Debug.Log($"[GameManager] SetWindowMaximized for '{windowId}' set to {maximized}");
        GetWindowState(windowId).IsMaximized = maximized;
    }

    public bool IsWindowMaximized(string windowId)
    {
        bool maximized = GetWindowState(windowId).IsMaximized;
        Debug.Log($"[GameManager] IsWindowMaximized for '{windowId}' -> {maximized}");
        return maximized;
    }


    // =========================================================
    // Open / Closed State
    // =========================================================

    public void SetWindowOpen(string windowId, bool isOpen)
    {
        Debug.Log($"[GameManager] SetWindowOpen for '{windowId}' set to {isOpen}");
        GetWindowState(windowId).IsOpen = isOpen;
    }

    public bool IsWindowOpen(string windowId)
    {
        bool isOpen = GetWindowState(windowId).IsOpen;
        Debug.Log($"[GameManager] IsWindowOpen for '{windowId}' -> {isOpen}");
        return isOpen;
    }


    // =========================================================
    // Window Position
    // =========================================================

    public void SetWindowPosition(string windowId, Vector2 normalizedPosition)
    {
        Debug.Log($"[GameManager] SetWindowPosition for '{windowId}' set to {normalizedPosition}");
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
            Debug.Log($"[GameManager] TryGetWindowPosition for '{windowId}' found position: {normalizedPosition}");
            return true;
        }

        normalizedPosition = default;
        Debug.Log($"[GameManager] TryGetWindowPosition for '{windowId}' found no saved position.");
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
            Debug.Log($"[GameManager] GetOrCreateWindowStackOrder assigned initial stack order {state.StackOrder} for windowId: '{windowId}'");
        }
        else
        {
            Debug.Log($"[GameManager] GetOrCreateWindowStackOrder retrieved existing stack order {state.StackOrder} for windowId: '{windowId}'");
        }

        return state.StackOrder;
    }

    public void BringWindowToFront(string windowId)
    {
        WindowState state = GetWindowState(windowId);

        state.StackOrder = nextWindowStackOrder;
        state.HasStackOrder = true;
        nextWindowStackOrder++;
        Debug.Log($"[GameManager] BringWindowToFront brought windowId '{windowId}' to front with stack order {state.StackOrder}");
    }
}
