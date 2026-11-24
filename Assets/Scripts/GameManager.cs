using NullProgrammer.Showcase;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
/// <summary>
/// Shared session state: player stats, monitor zoom, story/day state, and fallback window state for true scene reloads.
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
    [Header("Current Day Set to: ")]
    [field: SerializeField]
    public int CurrentDay { get; private set; } = 1;
    public string CurrentStep { get; private set; } = "Start";
    public int GrandArchitectStat { get; private set; }
    public int PragmatistStat { get; private set; }
    public int DetectiveStat { get; private set; }
    public int SteveSuspicion { get; private set; }

    public int CodeQuality { get; private set; }
    public int Connectedness { get; private set; }

    public const int MaxSuspicion = 100;

    public event Action<int> SuspicionChanged;
    public event Action SteveCrisis;
    public event Action<int> DayChanged; //Syed's new implementation
    // messaging characters stats
    public int MomTrust { get; private set; }
    public int MomConcern { get; private set; }
    public int MarkTrust { get; private set; }
    public int AshleyTrust { get; private set; }
    public int AshleyGuarded { get; private set; }
    private readonly HashSet<string> storyFlags = new HashSet<string>(StringComparer.Ordinal);
    // =========================================================
    // Weekend Choice
    // =========================================================

    public WeekendChoice? SelectedWeekendChoice { get; private set; }
    public GameEndingType? SelectedEnding { get; private set; }

    public bool HasSelectedWeekendChoice => SelectedWeekendChoice.HasValue;

    // Personality skill progression is session-only. These levels persist across
    // scene changes because GameManager is DontDestroyOnLoad, but they are never
    // written to PlayerPrefs and therefore cannot leak into a different run.
    private int[] personalitySkillLevels = new int[5];

    // Showcase mode temporarily overrides the normal run's levels without changing them.
    private int[] showcaseSkillLevels;
    private GameEndingType? showcaseEndingOverride;
    internal bool ShowcaseWeekendUnlocked { get; private set; }
    internal static IEnumerable<string> ShowcaseClueIds => KnownClueIds;

    // =========================================================
    // Weekend Choice Availability
    // =========================================================
    [Header("Weekend & Ending Thresholds")]
    [Tooltip("Steve weekend is available only while suspicion is strictly below this value.")]
    [SerializeField, Min(0)] private int weekendSteveMaxSuspicionExclusive = 50;

    [Tooltip("Studying alone is available once CodeQuality reaches this value.")]
    [SerializeField, Min(0)] private int weekendStudyMinCodeQuality = 40;

    [Tooltip("Friends/family weekend is available only while Connectedness is strictly above this value.")]
    [SerializeField, Min(0)] private int weekendConnectionsMinConnectednessExclusive = 50;

    [Tooltip("CodeQuality at or above this value counts as high quality for endings.")]
    [SerializeField, Min(0)] private int highCodeQualityThreshold = 70;

    [Tooltip("CodeQuality at or above this value counts as moderate quality; below it is low quality.")]
    [SerializeField, Min(0)] private int moderateCodeQualityThreshold = 40;

    [Tooltip("SteveSuspicion at or above this value counts as high suspicion for endings.")]
    [SerializeField, Min(0)] private int highSuspicionEndingThreshold = 70;

    public int HighCodeQualityThreshold => highCodeQualityThreshold;
    public int ModerateCodeQualityThreshold => moderateCodeQualityThreshold;
    public int HighSuspicionEndingThreshold => highSuspicionEndingThreshold;

    public const int TotalClueCount = 10;
    private static readonly HashSet<string> KnownClueIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "C01_BjarneName",
        "C02_Birthday",
        "C03_AshleyGirlfriend",
        "C04_StoodUp",
        "C05_MarkBestFriend",
        "C06_Intoxicated",
        "C07_WorkStress",
        "C08_PreviousBreakup",
        "C09_EmotionalDistance",
        "C10_FewFriends"
    };

    private readonly HashSet<string> discoveredClues = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> detectiveClueReactionQueue = new Queue<string>();
    private AudioSource clueDiscoveryAudioSource;
    [Header("Clue Discovery Audio")]
    [SerializeField] private AudioClip clueDiscoveryClip;
    [SerializeField, Range(0f, 1f)] private float clueDiscoveryVolume = 0.85f;
    private bool playingDetectiveClueReaction;

    public int DiscoveredCluesCount => discoveredClues.Count;
    public IReadOnlyCollection<string> DiscoveredClues => discoveredClues;
    public event Action<string, int> ClueDiscovered;


    public bool CanSpendWeekendWithSteve()
    {
        bool result = SteveSuspicion < weekendSteveMaxSuspicionExclusive;
        Debug.Log($"[GameManager] CanSpendWeekendWithSteve evaluated. SteveSuspicion: {SteveSuspicion}, Result: {result}");
        return result;
    }

    public bool CanStudyThisWeekend()
    {
        int architectLevel = PersonalitySkills.GetSavedLevel(Personality.Architect);
        bool result = architectLevel > 3 && CodeQuality >= weekendStudyMinCodeQuality;
        Debug.Log($"[GameManager] CanStudyThisWeekend evaluated. ArchitectLevel: {architectLevel}, Result: {result}");
        return result;
    }

    public bool CanReconnectThisWeekend()
    {
        bool result = Connectedness > weekendConnectionsMinConnectednessExclusive;
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
        if (ShowcaseWeekendUnlocked && Enum.IsDefined(typeof(WeekendChoice), choice)) return true;
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


    // Screen-share session state. Stored here because this manager survives scene loads.
    [System.Serializable]
    public class ZoomShareState
    {
        public int day;
        public bool callVisible;
        public bool incomingCallVisible;
        public bool shareActive;
        public bool usesVideo;
        public double videoTime;
        public bool videoWasPlaying;
        public bool videoPausedForDialogue;
        [NonSerialized] public System.Threading.Tasks.Task pausedVideoDialogue;
        public int nextVideoCueIndex;
        public bool shareStarting;
        public SteveExpression expression;
        public bool hasExpression;
    }

    public ZoomShareState ZoomState { get; private set; } = new ZoomShareState();

    public void SaveZoomState(ZoomShareState state)
    {
        if (state != null) ZoomState = state;
    }

    public void ClearZoomState()
    {
        ZoomState = new ZoomShareState();
    }

    // =========================================================
    // Fallback Window State (true DesktopUI unload/reload only)
    // =========================================================
    // MonitorScenePersistenceManager V3 keeps the live DesktopUI scene loaded
    // while moving among Center/Left/Right, so monitor switches do NOT depend on
    // this cache anymore. Keep it for non-monitor transitions that actually unload
    // DesktopUI and later create fresh window instances. Public APIs are retained
    // unchanged to protect downstream/team code that may still call them.

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
        //CurrentDay = 1; for debugging
        CurrentStep = "Start";
        SteveSuspicion = 0;
        UnspentSkillPoints = 0;
        rewardedMilestones.Clear();
        personalitySkillLevels = new int[5];
        showcaseSkillLevels = null;
        SelectedWeekendChoice = null;
        MomTrust = 0;
        MomConcern = 0;
        MarkTrust = 0;
        AshleyTrust = 0;
        AshleyGuarded = 0;
        storyFlags.Clear();
        discoveredClues.Clear();
        detectiveClueReactionQueue.Clear();
        playingDetectiveClueReaction = false;
        SelectedEnding = null;
        EnsureClueDiscoveryAudioSource();

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

    /// <summary>
    /// Compatibility entry point retained for existing callers and UnityEvent bindings.
    /// New skill code can use TryAwardSkillPoint when it needs to know whether the
    /// milestone was newly rewarded.
    /// </summary>
    public void AwardSkillPoint(string milestoneID)
    {
        TryAwardSkillPoint(milestoneID);
    }

    /// <summary>
    /// Awards one generic personality skill point for a milestone in the current run.
    /// Duplicate milestone IDs are ignored for this run only. Nothing is persisted to disk.
    /// </summary>
    internal bool TryAwardSkillPoint(string milestoneID)
    {
        Debug.Log($"[GameManager] AwardSkillPoint called with milestoneID: '{milestoneID}'");
        if (string.IsNullOrWhiteSpace(milestoneID) || rewardedMilestones.Contains(milestoneID))
        {
            return false;
        }

        rewardedMilestones.Add(milestoneID);
        UnspentSkillPoints++;
        NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.Skill);
        Debug.Log($"[GameManager] Skill point awarded for '{milestoneID}'. Total unspent: {UnspentSkillPoints}");
        return true;
    }

    /// <summary>
    /// Returns the level used by the current run, or the temporary showcase level while
    /// showcase mode is active.
    /// </summary>
    internal int GetPersonalitySkillLevel(Personality personality)
    {
        int index = (int)personality;
        int[] levels = showcaseSkillLevels ?? personalitySkillLevels;

        if (levels == null || index < 0 || index >= levels.Length)
        {
            return 0;
        }

        return Mathf.Clamp(levels[index], 0, PersonalitySkills.MaxLevel);
    }

    /// <summary>
    /// Atomically spends one available point and raises one personality by one level.
    /// Keeping the spend and level increase together prevents points from being lost
    /// if an investment cannot be completed.
    /// </summary>
    internal bool TryInvestPersonalitySkill(Personality personality)
    {
        if (UnspentSkillPoints <= 0) return false;

        int index = (int)personality;
        int[] levels = showcaseSkillLevels ?? personalitySkillLevels;
        if (levels == null || index < 0 || index >= levels.Length) return false;
        if (levels[index] >= PersonalitySkills.MaxLevel) return false;

        levels[index]++;
        UnspentSkillPoints--;

        Debug.Log($"[GameManager] Invested one skill point in {personality}. " +
                  $"New level: {levels[index]}, Unspent remaining: {UnspentSkillPoints}");
        return true;
    }

    /// <summary>
    /// Compatibility API retained for older callers. New personality-skill code uses
    /// TryInvestPersonalitySkill so spending and leveling are atomic.
    /// </summary>
    public void SpendSkillPoint()
    {
        Debug.Log($"[GameManager] SpendSkillPoint called. Unspent before: {UnspentSkillPoints}");
        UnspentSkillPoints = Mathf.Max(0, UnspentSkillPoints - 1);
        Debug.Log($"[GameManager] Unspent skill points after spend: {UnspentSkillPoints}");
    }

    internal void ResetPersonalitySkillsForCurrentRun()
    {
        UnspentSkillPoints = 0;
        rewardedMilestones.Clear();
        personalitySkillLevels = new int[5];
        showcaseSkillLevels = null;
    }
    public void setCurrentDay(int day)
    {
        int previous = CurrentDay;
        CurrentDay = Mathf.Max(1, day);
        Debug.Log($"[GameManager] setCurrentDay called. Previous: {previous}, New CurrentDay: {CurrentDay}");
        if (previous != CurrentDay)
        {
            ClearZoomState();
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
        // Unspent personality skill points carry forward within the same run.
        // They are reset only when a fresh GameManager session starts.
        CurrentStep = "Start"; //reset for new day
        ClearZoomState();
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
    public bool HasStoryFlag(string key)
    {
        return !string.IsNullOrWhiteSpace(key) &&
               (storyFlags.Contains(key) || discoveredClues.Contains(key));

    }

    public void SetStoryFlag(string key, bool value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        // Clues are permanent discoveries. Existing JSON effects that set a clue
        // story flag will automatically register the clue, play its sound, and queue
        // the Detective's reaction without changing JsonMessageDialogueController.
        if (KnownClueIds.Contains(key))
        {
            if (value) DiscoverClue(key);
            else Debug.LogWarning("[GameManager] Clue discoveries are permanent and cannot be unset: " + key);
            return;
        }

        if (value) storyFlags.Add(key);
        else storyFlags.Remove(key);

        Debug.Log($"[GameManager] Story flag '{key}' set to {value}.");
    }

    public bool HasDiscoveredClue(string clueId)
    {
        return !string.IsNullOrWhiteSpace(clueId) && discoveredClues.Contains(clueId);
    }

    public bool DiscoverClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId) || !KnownClueIds.Contains(clueId))
        {
            Debug.LogWarning("[GameManager] Unknown clue ID: " + clueId);
            return false;
        }

        // Repeated effects must not award duplicate sounds/reactions/counts.
        if (!discoveredClues.Add(clueId))
            return false;

        storyFlags.Add(clueId);
        EnsureClueDiscoveryAudioSource();
        if (clueDiscoveryClip != null && clueDiscoveryAudioSource != null)
            clueDiscoveryAudioSource.PlayOneShot(clueDiscoveryClip, clueDiscoveryVolume);

        Debug.Log($"[GameManager] Clue discovered: {clueId} ({discoveredClues.Count}/{TotalClueCount}).");
        ClueDiscovered?.Invoke(clueId, discoveredClues.Count);

        detectiveClueReactionQueue.Enqueue(clueId);
        if (!playingDetectiveClueReaction)
            StartCoroutine(PlayDetectiveClueReactions());

        return true;
    }

    private void EnsureClueDiscoveryAudioSource()
    {
        if (clueDiscoveryAudioSource == null)
            clueDiscoveryAudioSource = GetComponent<AudioSource>();
        if (clueDiscoveryAudioSource == null)
            clueDiscoveryAudioSource = gameObject.AddComponent<AudioSource>();

        clueDiscoveryAudioSource.playOnAwake = false;
        clueDiscoveryAudioSource.loop = false;
        clueDiscoveryAudioSource.spatialBlend = 0f;
    }

    private IEnumerator PlayDetectiveClueReactions()
    {
        playingDetectiveClueReaction = true;

        while (detectiveClueReactionQueue.Count > 0)
        {
            string clueId = detectiveClueReactionQueue.Dequeue();

            // The dialogue UI is persistent, but it may not be present yet during
            // initial scene loading. Wait briefly for it rather than losing the clue.
            float waited = 0f;
            while (DialogueManager.Instance == null && waited < 8f)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[GameManager] Could not show Detective reaction; DialogueManager was unavailable for clue " + clueId);
                continue;
            }

            string reaction = GetDetectiveClueReaction(clueId);
            if (string.IsNullOrWhiteSpace(reaction))
                continue;

            DialogueLine line = new DialogueLine
            {
                speakerName = "The Detective",
                personality = HeadPersonality.Detective,
                text = reaction,
                displayDuration = 3.5f
            };

            Task playback = DialogueManager.Instance.DisplayLineAndWaitAsync(line);
            while (playback != null && !playback.IsCompleted)
                yield return null;

            if (playback != null && playback.IsFaulted)
                Debug.LogException(playback.Exception);
        }

        playingDetectiveClueReaction = false;
    }

    private static string GetDetectiveClueReaction(string clueId)
    {
        switch (clueId)
        {
            case "C01_BjarneName":
                return "Bjarne. That's our name. Not a username or a project label. A name. Start there.";
            case "C02_Birthday":
                return "Forty. A milestone, and we can't remember celebrating it. I wonder what we were trying to forget.";
            case "C03_AshleyGirlfriend":
                return "Ashley is our girlfriend. She expected something from us. This is more than an angry message.";
            case "C04_StoodUp":
                return "We stood Ashley up. The date mattered to her. Whatever happened that weekend wasn't just a harmless night out.";
            case "C05_MarkBestFriend":
                return "Mark. Our best friend. Someone kept a place for us even when we disappeared.";
            case "C06_Intoxicated":
                return "A voicemail left in that state. We were trying to reach someone. Why couldn't we say what we needed while sober?";
            case "C07_WorkStress":
                return "Work was weighing on us long before Saturday. The warning signs weren't subtle. We kept filing them under later.";
            case "C08_PreviousBreakup":
                return "They had already broken up once. This wasn't one bad evening; it was a pattern that nearly ended things before.";
            case "C09_EmotionalDistance":
                return "We stopped telling people how we felt. We weren't just busy; we were closing the door while people were still knocking.";
            case "C10_FewFriends":
                return "Mark was almost the only friend left. We reduced an entire life to one contact and a work inbox.";
            default:
                return "Another piece of the past. Keep going; the pattern is becoming clearer.";
        }
    }

    public void AddStoryStat(string key, int amount)
    {
        if (string.IsNullOrWhiteSpace(key) || amount == 0) return;

        switch (key)
        {
            case "MomTrust":
                MomTrust = Mathf.Max(0, MomTrust + amount);
                break;
            case "MarkTrust":
                MarkTrust = Mathf.Max(0, MarkTrust + amount);
                break;
            case "AshleyTrust":
                AshleyTrust = Mathf.Max(0, AshleyTrust + amount);
                break;
            case "Connectedness":
                ChangeConnectednessStat(amount);
                break;
            case "SteveSuspicion":
                ChangeSteveSuspicion(amount);
                break;
            case "CodeQuality":
                ChangeCodeQualityStat(amount);
                break;
            case "GrandArchitectStat":
                ChangeGrandArchitectStat(amount);
                break;
            case "PragmatistStat":
                ChangePragmatistStat(amount);
                break;
            case "DetectiveStat":
                ChangeDetectiveStat(amount);
                break;
            case "MomConcern":
                MomConcern = Mathf.Max(0, MomConcern + amount);
                break;
            case "AshleyGuarded":
                AshleyGuarded = Mathf.Max(0, AshleyGuarded + amount);
                break;
            default:
                Debug.LogWarning($"[GameManager] Unknown JSON dialogue stat key '{key}'.");
                break;
        }
    }


    // =========================================================
    // Email Reply Demo / Day 5 Mail State
    // =========================================================

    public void MarkEmailRead()
    {
        EmailRead = true;
        //Debug.Log("[GameManager] MarkEmailRead called. EmailRead set to true.");
    }

    public void MarkDay5EmailRead()
    {
        Day5EmailRead = true;
        //Debug.Log("[GameManager] MarkDay5EmailRead called. Day5EmailRead set to true.");
    }

    public void MarkReadEmailClosed()
    {
        //Debug.Log($"[GameManager] MarkReadEmailClosed called. EmailRead: {EmailRead}, ReplyCompleted: {ReplyCompleted}");
        if (EmailRead && !ReplyCompleted)
        {
            ReplyAvailable = true;
            //Debug.Log("[GameManager] ReplyAvailable set to true.");
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
    // Fallback Window State API
    // =========================================================
    // These methods are intentionally retained for source compatibility and for
    // genuine DesktopUI recreation. They are no longer the primary mechanism for
    // switching between monitors.

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

    public bool TryGetWindowOpen(string windowId, out bool isOpen)
    {
        if (windowStates.TryGetValue(windowId, out WindowState state))
        {
            isOpen = state.IsOpen;
            return true;
        }
        isOpen = false;
        return false;
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
    // =========================================================
    // Endings
    // =========================================================

    public GameEndingType DetermineEnding()
    {
        return StoreEnding(PreviewShowcaseEnding());
    }

    internal GameEndingType PreviewShowcaseEnding()
    {
        return showcaseEndingOverride ?? EvaluateEnding();
    }

    private GameEndingType EvaluateEnding()
    {
        int detectiveLevel = PersonalitySkills.GetSavedLevel(Personality.Detective);

        // Secret ending must take priority over every ordinary ending.
        if (detectiveLevel >= PersonalitySkills.MaxLevel &&
            DiscoveredCluesCount >= TotalClueCount)
        {
            return GameEndingType.SegmentationFaultOfSoul;
        }

        bool highQuality = CodeQuality >= highCodeQualityThreshold;
        bool moderateQuality = CodeQuality >= moderateCodeQualityThreshold && !highQuality;
        bool lowQuality = CodeQuality < moderateCodeQualityThreshold;
        bool suspicionHigh = SteveSuspicion >= highSuspicionEndingThreshold;

        // High-quality project outcomes depend on how the player spent the weekend.
        if (highQuality)
        {
            if (SelectedWeekendChoice == WeekendChoice.Steve)
                return GameEndingType.UltimateMentor;
            if (SelectedWeekendChoice == WeekendChoice.Study)
                return GameEndingType.PerfectionistTrap;
            if (SelectedWeekendChoice == WeekendChoice.PersonalConnections)
                return GameEndingType.ArchitectRedemption;
        }

        // A low-quality project rescued by Steve is the precarious-but-surviving outcome.
        if (lowQuality && SelectedWeekendChoice == WeekendChoice.Steve)
            return GameEndingType.ImposterProtocol;

        // Steve's suspicion and project quality determine the two termination outcomes.
        if (suspicionHigh && lowQuality)
            return GameEndingType.ObsoleteAndDeprecated;

        if (suspicionHigh && moderateQuality)
            return GameEndingType.ObsoleteSoftRelaunch;

        // The original design does not specify every possible combination (for example,
        // low quality + low suspicion + a friends weekend). Keep those unassigned runs
        // on the less-severe fallback until a dedicated neutral ending is authored.
        return GameEndingType.ObsoleteSoftRelaunch;
    }

    internal int? GetShowcaseSkillLevel(Personality personality)
    {
        return showcaseSkillLevels?[(int)personality];
    }

    internal bool TryInvestShowcaseSkill(Personality personality)
    {
        if (showcaseSkillLevels == null) return false;
        showcaseSkillLevels[(int)personality]++;
        return true;
    }

    internal void ApplyShowcaseState(ShowcaseState state)
    {
        CodeQuality = state.CodeQuality;
        Connectedness = state.Connectedness;
        GrandArchitectStat = state.Architect;
        PragmatistStat = state.Pragmatist;
        DetectiveStat = state.Detective;
        UnspentSkillPoints = Mathf.Max(0, state.SkillPoints);
        int previousSuspicion = SteveSuspicion;
        SteveSuspicion = Mathf.Clamp(state.Suspicion, 0, MaxSuspicion);
        SelectedWeekendChoice = state.Weekend;
        ShowcaseWeekendUnlocked = state.UnlockWeekend;
        SelectedEnding = null;
        showcaseEndingOverride = null;

        showcaseSkillLevels = new int[state.Levels.Length];
        foreach (Personality personality in ShowcaseState.Personalities)
        {
            int index = (int)personality;
            showcaseSkillLevels[index] = Mathf.Clamp(state.Levels[index], 0, PersonalitySkills.MaxLevel);
        }

        // Editing evidence should not replay discovery dialogue or sound.
        discoveredClues.Clear();
        foreach (string clue in state.Clues)
            if (KnownClueIds.Contains(clue)) discoveredClues.Add(clue);

        if (SteveSuspicion != previousSuspicion) SuspicionChanged?.Invoke(SteveSuspicion);
    }

    internal void ApplyShowcaseEnding(GameEndingType ending)
    {
        ShowcaseState state = ShowcaseState.Capture(this);
        state.Clues.Clear();
        state.Levels[(int)Personality.Detective] = 0;
        state.Weekend = WeekendChoice.Wallow;
        state.Suspicion = 0;

        // Use the current Inspector thresholds, so these are normal game outcomes
        // whenever the threshold configuration allows the requested route.
        int lowQuality = Math.Min(HighCodeQualityThreshold, ModerateCodeQualityThreshold) - 1;
        switch (ending)
        {
            case GameEndingType.ObsoleteAndDeprecated:
                state.CodeQuality = lowQuality;
                state.Suspicion = HighSuspicionEndingThreshold;
                break;
            case GameEndingType.ObsoleteSoftRelaunch:
                state.CodeQuality = ModerateCodeQualityThreshold;
                state.Suspicion = HighSuspicionEndingThreshold;
                break;
            case GameEndingType.ImposterProtocol:
                state.CodeQuality = lowQuality;
                state.Weekend = WeekendChoice.Steve;
                break;
            case GameEndingType.PerfectionistTrap:
                state.CodeQuality = HighCodeQualityThreshold;
                state.Weekend = WeekendChoice.Study;
                state.Levels[(int)Personality.Architect] = PersonalitySkills.MaxLevel;
                break;
            case GameEndingType.UltimateMentor:
                state.CodeQuality = HighCodeQualityThreshold;
                state.Weekend = WeekendChoice.Steve;
                break;
            case GameEndingType.ArchitectRedemption:
                state.CodeQuality = HighCodeQualityThreshold;
                state.Weekend = WeekendChoice.PersonalConnections;
                state.Connectedness = weekendConnectionsMinConnectednessExclusive < int.MaxValue
                    ? weekendConnectionsMinConnectednessExclusive + 1 : int.MaxValue;
                break;
            case GameEndingType.SegmentationFaultOfSoul:
                state.Levels[(int)Personality.Detective] = PersonalitySkills.MaxLevel;
                state.Clues.UnionWith(KnownClueIds);
                break;
        }

        ApplyShowcaseState(state);
        if (EvaluateEnding() != ending)
        {
            // Keep every showcase button usable with custom, unreachable thresholds.
            showcaseEndingOverride = ending;
            Debug.LogWarning($"[Showcase] Current thresholds cannot reach {ending} with its preset. Using the requested route for this preview.", this);
        }
    }

    internal void CancelShowcaseClueReactions()
    {
        StopAllCoroutines();
        detectiveClueReactionQueue.Clear();
        playingDetectiveClueReaction = false;
    }

    internal void PrepareShowcaseJump(int day)
    {
        CancelShowcaseClueReactions();
        ClearZoomState();
        // A showcase jump creates a fresh authored desktop, so clear the fallback
        // scene-reload cache as well.
        windowStates.Clear();
        nextWindowStackOrder = 0;
        EmailRead = false;
        Day5EmailRead = false;
        ReplyAvailable = false;
        ReplyStage = 0;
        LastReplyFeedback = "";
        SelectedEnding = null;
        SetZoomedIn(true);
        setStep("Start");
        setCurrentDay(day);
    }

    private GameEndingType StoreEnding(GameEndingType ending)
    {
        SelectedEnding = ending;
        Debug.Log("[GameManager] Selected ending: " + ending);
        return ending;
    }
}
