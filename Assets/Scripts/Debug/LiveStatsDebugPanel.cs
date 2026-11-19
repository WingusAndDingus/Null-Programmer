using UnityEngine;
using TMPro;

public class LiveStatsDebugPanel : MonoBehaviour
{
    [Header("Display Text")]
    [SerializeField] private TMP_Text statsText;

    [Header("Editable Stat Inputs (Optional)")]
    [SerializeField] private TMP_InputField dayInput;
    [SerializeField] private TMP_InputField codeQualityInput;
    [SerializeField] private TMP_InputField suspicionInput;
    [SerializeField] private TMP_InputField architectInput;
    [SerializeField] private TMP_InputField pragmatistInput;
    [SerializeField] private TMP_InputField detectiveInput;
    [SerializeField] private TMP_InputField connectednessInput;

    public static bool IsAnyInputFocused { get; private set; }

    private void Awake()
    {
        RegisterField(dayInput, ApplyDay);
        RegisterField(codeQualityInput, ApplyCodeQuality);
        RegisterField(suspicionInput, ApplySuspicion);
        RegisterField(architectInput, ApplyArchitect);
        RegisterField(pragmatistInput, ApplyPragmatist);
        RegisterField(detectiveInput, ApplyDetective);
        RegisterField(connectednessInput, ApplyConnectedness);
    }

    private void OnDestroy()
    {
        IsAnyInputFocused = false;
    }

    private void RegisterField(TMP_InputField field, System.Action<int> onCommit)
    {
        if (field == null) return;

        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.onEndEdit.AddListener(rawText =>
        {
            if (GameManager.Instance != null && int.TryParse(rawText, out int parsedValue))
            {
                onCommit?.Invoke(parsedValue);
            }
            SyncAllInputsFromGameManager();
        });
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        IsAnyInputFocused =
            (dayInput != null && dayInput.isFocused) ||
            (codeQualityInput != null && codeQualityInput.isFocused) ||
            (suspicionInput != null && suspicionInput.isFocused) ||
            (architectInput != null && architectInput.isFocused) ||
            (pragmatistInput != null && pragmatistInput.isFocused) ||
            (detectiveInput != null && detectiveInput.isFocused) ||
            (connectednessInput != null && connectednessInput.isFocused);

        if (statsText != null)
        {
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

        SyncAllInputsFromGameManager();
    }

    private void SyncAllInputsFromGameManager()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        SyncField(dayInput, gm.CurrentDay);
        SyncField(codeQualityInput, gm.CodeQuality);
        SyncField(suspicionInput, gm.SteveSuspicion);
        SyncField(architectInput, gm.GrandArchitectStat);
        SyncField(pragmatistInput, gm.PragmatistStat);
        SyncField(detectiveInput, gm.DetectiveStat);
        SyncField(connectednessInput, gm.Connectedness);
    }

    private void SyncField(TMP_InputField field, int currentValue)
    {
        if (field == null || field.isFocused) return;
        string targetText = currentValue.ToString();
        if (field.text != targetText)
        {
            field.SetTextWithoutNotify(targetText);
        }
    }

    // --- Commit Handlers (Convert absolute target value into GameManager delta) ---
    private void ApplyDay(int newDay)
    {
        int clamped = Mathf.Max(1, newDay);
        if (GameManager.Instance.CurrentDay == clamped) return;
        GameManager.Instance.setCurrentDay(clamped);
        ReloadNarrative();
    }

    private void ApplyCodeQuality(int target) =>
        GameManager.Instance.ChangeCodeQualityStat(target - GameManager.Instance.CodeQuality);

    private void ApplySuspicion(int target)
    {
        int clamped = Mathf.Clamp(target, 0, GameManager.MaxSuspicion);
        GameManager.Instance.ChangeSteveSuspicion(clamped - GameManager.Instance.SteveSuspicion);
    }

    private void ApplyArchitect(int target) =>
        GameManager.Instance.ChangeGrandArchitectStat(target - GameManager.Instance.GrandArchitectStat);

    private void ApplyPragmatist(int target) =>
        GameManager.Instance.ChangePragmatistStat(target - GameManager.Instance.PragmatistStat);

    private void ApplyDetective(int target) =>
        GameManager.Instance.ChangeDetectiveStat(target - GameManager.Instance.DetectiveStat);

    private void ApplyConnectedness(int target) =>
        GameManager.Instance.ChangeConnectednessStat(target - GameManager.Instance.Connectedness);

    // --- Existing Day Buttons ---
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