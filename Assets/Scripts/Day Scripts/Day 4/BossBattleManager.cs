using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Threading.Tasks;
using UnityEditor.Analytics;

public class BossBattleManager : MonoBehaviour
{
    public static BossBattleManager Instance;

    public enum BattleState { PreStart, Playing, Paused, GameOver }
    public BattleState currentState = BattleState.PreStart;

    [Header("Audio Engine")]
    [SerializeField] private AudioClip globalBattleMusic;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float noteTravelTime = 1.5f;

    [Header("UI & Lane Anchors")]
    [SerializeField] private RectTransform[] laneSpawners; 
    [SerializeField] private RectTransform[] hitLines;     
    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private Slider fleetHealthBar;
    [SerializeField] private TextMeshProUGUI hitFeedbackText; 
    [SerializeField] private Image[] laneBackgrounds; 

    [Header("Damage & Feedback Visuals")]
    [SerializeField] private Image bossHeaderBackground; 
    [SerializeField] private Image[] fleetImages; 
    [SerializeField] private RectTransform battleContainer; 
    [SerializeField] private GameObject uiTextParticlePrefab; 
    [SerializeField] private GameObject uiSmokeParticlePrefab; 

    [Header("MCQ Integrated UI")]
    [SerializeField] private GameObject mcqPanel;
    [SerializeField] private TextMeshProUGUI floatingQuestionText; 
    [SerializeField] private TextMeshProUGUI[] answerChoiceTexts; 

    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI menuTitleText;
    [SerializeField] private GameObject statsPanel;

    [SerializeField] private TextMeshProUGUI menuStatsText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject continueButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioSource sfxSource; // Dedicated AudioSource for sound effects
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip missSound;

    [Header("Background Pulse")]
    [SerializeField] private Image mainBackgroundImage; 
    [SerializeField] private Color hitPulseColor = new Color(0f, 0.8f, 1f, 1f); // Cyan
    [SerializeField] private Color missPulseColor = new Color(1f, 0f, 0f, 1f); // Red

    private Color originalBackgroundColor;
    private Coroutine activeBackgroundPulse;
    private Coroutine activeCountdown;
    [Header("Battle Data Assets")]
    [SerializeField] private List<BossBattlePhaseData> battlePhases;

    // Timing & State
    private double phaseStartDspTime; 
    private double pauseStartDspTime; // Used to offset time when unpausing
    private int currentPhaseIndex = 0;
    private float bossHP = 100f;
    private float fleetHP = 100f;
    private bool isMCQActive = false;
    private bool battleWon = false;

    // Stat Tracking & Buffered Rewards
    private int perfectHits, goodHits, misses, earlyMisses;
    private int pendingArchitectStat = 0;
    private int pendingSuspicionStat = 0;
    private int pendingCodeQuality = 0;

    // Visual State
    private Vector3 originalFeedbackPos;
    private Color trueBossHeaderColor;
    private Coroutine activeTextCoroutine;
    private Coroutine activeBossFlashCoroutine;
    private Coroutine[] activeLaneCoroutines = new Coroutine[4];

    private struct ActiveNote
    {
        public GameObject noteObject;
        public RectTransform rectTransform;
        public NoteData data;
        public bool hit;
        public bool isAnswer; 
        public bool isCorrectAnswer;
    }
    private List<ActiveNote> activeNotes = new List<ActiveNote>();
    private int nextNoteIndex = 0;

    private const float PERFECT_WINDOW = 0.08f;
    private const float GOOD_WINDOW = 0.16f;
    private const float EARLY_WINDOW = 0.40f;

    private TaskCompletionSource<bool> battleCompletionSource;

    private void Awake()
    {
        Instance = this;
        if (statsPanel != null) statsPanel.SetActive(false);
        if (mcqPanel != null) mcqPanel.SetActive(false);
        if (floatingQuestionText != null) floatingQuestionText.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (hitFeedbackText != null) 
            originalFeedbackPos = hitFeedbackText.rectTransform.anchoredPosition;
            
        if (bossHeaderBackground != null)
            trueBossHeaderColor = bossHeaderBackground.color;

        if (mainBackgroundImage != null) originalBackgroundColor = mainBackgroundImage.color;
    }

    public Task<bool> StartBossBattleAsync()
    {
        battleCompletionSource = new TaskCompletionSource<bool>();
        gameObject.SetActive(true);
        
        // Show Start Menu instead of starting immediately
        ShowMenu("MAIN.CPP BOSS BATTLE", "", true, false, false, false);
        currentState = BattleState.PreStart;

        return battleCompletionSource.Task;
    }

    // --- Menu Button Handlers ---

    public void OnStartClicked()
    {
        menuPanel.SetActive(false);
        InitializeBattleData();
        if (activeCountdown != null) StopCoroutine(activeCountdown);
        activeCountdown = StartCoroutine(CountdownRoutine(false));
    }

    public void OnResumeClicked()
    {
        menuPanel.SetActive(false);
        if (activeCountdown != null) StopCoroutine(activeCountdown);
        activeCountdown = StartCoroutine(CountdownRoutine(true));
    }

    public void OnRestartClicked()
    {
        menuPanel.SetActive(false);
        musicSource.Stop();
        ClearAllNotes();
        InitializeBattleData();
        StartPhase(0);
    }

    public void OnContinueClicked()
    {
        // Apply all buffered stats to the real GameManager now
        if (GameManager.Instance != null)
        {
            if (pendingArchitectStat > 0) GameManager.Instance.ChangeGrandArchitectStat(pendingArchitectStat);
            if (pendingSuspicionStat > 0) GameManager.Instance.ChangeSteveSuspicion(pendingSuspicionStat);
            if (pendingCodeQuality != 0) GameManager.Instance.ChangeCodeQualityStat(pendingCodeQuality);
        }

        gameObject.SetActive(false); 
        battleCompletionSource?.TrySetResult(battleWon);
    }

    private void InitializeBattleData()
    {
        currentPhaseIndex = 0;
        bossHP = 100f;
        fleetHP = 100f;
        UpdateHPUI();
        
        perfectHits = 0; goodHits = 0; misses = 0; earlyMisses = 0;
        pendingArchitectStat = 0; pendingSuspicionStat = 0; pendingCodeQuality = 0;
        
        musicSource.clip = globalBattleMusic;
        musicSource.loop = true;
    }

    private void StartPhase(int phaseIndex)
    {
        if (phaseIndex >= battlePhases.Count)
        {
            bossHP = 0f;
            UpdateHPUI();
            EndBattle(true); 
            return;
        }

        activeNotes.Clear();
        nextNoteIndex = 0;
        isMCQActive = false;
        
        if (mcqPanel != null) mcqPanel.SetActive(false);
        else if (floatingQuestionText != null) floatingQuestionText.gameObject.SetActive(false);

        phaseStartDspTime = AudioSettings.dspTime;
        if (!musicSource.isPlaying) musicSource.Play();
        
        currentState = BattleState.Playing;
    }

    private void Update()
    {
        // Handle Pause Input
        if (currentState == BattleState.Playing && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseGame();
            return;
        }

        if (currentState != BattleState.Playing) return;

        double currentPhaseTime = AudioSettings.dspTime - phaseStartDspTime;
        BossBattlePhaseData currentPhase = battlePhases[currentPhaseIndex];

        // 1. Spawn Regular Notes
        if (!isMCQActive)
        {
            while (nextNoteIndex < currentPhase.notes.Count &&
                   currentPhase.notes[nextNoteIndex].hitTime - noteTravelTime <= currentPhaseTime)
            {
                SpawnNote(currentPhase.notes[nextNoteIndex]);
                nextNoteIndex++;
            }

            if (nextNoteIndex >= currentPhase.notes.Count && activeNotes.Count == 0)
            {
                StartMCQ(currentPhase.intermissionQuestion, currentPhaseTime);
            }
        }

        // 2. Update Position of All Active Notes
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            ActiveNote an = activeNotes[i];
            float progress = (float)((currentPhaseTime - (an.data.hitTime - noteTravelTime)) / noteTravelTime);

            int laneIdx = (int)an.data.lane;
            Vector3 startPos = laneSpawners[laneIdx].position;
            Vector3 targetPos = hitLines[laneIdx].position;
            an.rectTransform.position = Vector3.Lerp(startPos, targetPos, progress);

            // Missed Note Logic
            if (currentPhaseTime > an.data.hitTime + GOOD_WINDOW && !an.hit)
            {
                if (an.isAnswer)
                {
                    ClearAllNotes();
                    EvaluateAnswer(false, an.data.lane);
                    break; 
                }
                else
                {
                    RhythmLane missedLane = an.data.lane;
                    NotePooler.Instance.ReturnNote(an.noteObject);
                    activeNotes.RemoveAt(i);
                    TriggerMiss(missedLane);
                }
            }
        }

        // 3. Process Input
        CheckLaneInput(Key.D, RhythmLane.LaneD, currentPhaseTime);
        CheckLaneInput(Key.F, RhythmLane.LaneF, currentPhaseTime);
        CheckLaneInput(Key.J, RhythmLane.LaneJ, currentPhaseTime);
        CheckLaneInput(Key.K, RhythmLane.LaneK, currentPhaseTime);
    }

    private void PauseGame()
    {
        currentState = BattleState.Paused;
        pauseStartDspTime = AudioSettings.dspTime;
        musicSource.Pause();
        ShowMenu("PAUSED", "", false, true, true, false);
    }

    private void ShowMenu(string title, string stats, bool start, bool resume, bool restart, bool cont)
    {
        menuPanel.SetActive(true);
        menuTitleText.text = title;
        menuStatsText.text = stats;
        if (stats.Length > 0) 
        {statsPanel.SetActive(true);}
        else {statsPanel.SetActive(false);}
        startButton.SetActive(start);
        resumeButton.SetActive(resume);
        restartButton.SetActive(restart);
        continueButton.SetActive(cont);
    }

    // --- Spawning Logic ---
    private void SpawnNote(NoteData noteData)
    {
        int laneIdx = (int)noteData.lane;
        GameObject noteObj = NotePooler.Instance.GetNote(laneSpawners[laneIdx]);
        
        ActiveNote an = new ActiveNote {
            noteObject = noteObj,
            rectTransform = noteObj.GetComponent<RectTransform>(),
            data = noteData,
            hit = false,
            isAnswer = false
        };
        activeNotes.Add(an);
    }

    private void StartMCQ(OOPQuestion question, double currentTime)
    {
        isMCQActive = true;
        
        // 1. Show the master panel container
        if (mcqPanel != null) mcqPanel.SetActive(true);

        if (floatingQuestionText != null)
        {
            floatingQuestionText.text = question.questionText;
            floatingQuestionText.gameObject.SetActive(true);
        }

        double answerHitTime = currentTime + 3.0f;
        for (int i = 0; i < 4; i++)
        {
            // 2. Populate the static answer text
            if (answerChoiceTexts != null && i < answerChoiceTexts.Length && answerChoiceTexts[i] != null)
            {
                if (i < question.options.Length)
                {
                    // Prepend the key (D, F, J, K) to make it obvious which lane to hit!
                    string[] keys = { "[D]", "[F]", "[J]", "[K]" };
                    answerChoiceTexts[i].text = $"{keys[i]} {question.options[i]}";
                    answerChoiceTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    answerChoiceTexts[i].gameObject.SetActive(false);
                }
            }

            // 3. Spawn the standard falling note for the player to strike
            if (i < question.options.Length)
            {
                SpawnAnswerNote((RhythmLane)i, answerHitTime, i == question.correctAnswerIndex);
            }
        }
    }
    private void SpawnAnswerNote(RhythmLane lane, double hitTime, bool isCorrect)
    {
        int laneIdx = (int)lane;
        
        // Pull a standard note from the pool instead of instantiating a prefab
        GameObject noteObj = NotePooler.Instance.GetNote(laneSpawners[laneIdx]);

        ActiveNote an = new ActiveNote {
            noteObject = noteObj,
            rectTransform = noteObj.GetComponent<RectTransform>(),
            data = new NoteData { hitTime = (float)hitTime, lane = lane },
            hit = false,
            isAnswer = true,
            isCorrectAnswer = isCorrect
        };
        activeNotes.Add(an);
    }

    // --- Input & Evaluation ---
    private void CheckLaneInput(Key key, RhythmLane lane, double currentPhaseTime)
    {
        if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
        {
            int laneIdx = (int)lane;
            if (activeLaneCoroutines[laneIdx] != null) StopCoroutine(activeLaneCoroutines[laneIdx]);
            activeLaneCoroutines[laneIdx] = StartCoroutine(FlashLaneRoutine(laneIdx));

            int targetNoteIndex = -1;
            float smallestDiff = float.MaxValue;
            double timeToNextNote = double.MaxValue;

            for (int i = 0; i < activeNotes.Count; i++)
            {
                if (activeNotes[i].data.lane == lane && !activeNotes[i].hit)
                {
                    float diff = Mathf.Abs((float)(currentPhaseTime - activeNotes[i].data.hitTime));
                    if (diff < smallestDiff)
                    {
                        smallestDiff = diff;
                        targetNoteIndex = i;
                        timeToNextNote = activeNotes[i].data.hitTime - currentPhaseTime; 
                    }
                }
            }

            if (targetNoteIndex != -1)
            {
                ActiveNote targetNote = activeNotes[targetNoteIndex];

                if (smallestDiff <= GOOD_WINDOW)
                {
                    targetNote.hit = true;
                    if (targetNote.isAnswer)
                    {
                        ClearAllNotes();
                        EvaluateAnswer(targetNote.isCorrectAnswer, lane);
                    }
                    else
                    {
                        NotePooler.Instance.ReturnNote(targetNote.noteObject);
                        activeNotes.RemoveAt(targetNoteIndex);

                        if (smallestDiff <= PERFECT_WINDOW)
                            RegisterHit("PERFECT!", 0.5f, 0f, Color.cyan, 1.5f, lane);
                        else
                            RegisterHit("GOOD", 0.25f, 0f, Color.darkCyan, 1.2f, lane);
                    }
                }
                else if (timeToNextNote > GOOD_WINDOW && timeToNextNote <= EARLY_WINDOW && !targetNote.isAnswer)
                {
                    targetNote.hit = true;
                    NotePooler.Instance.ReturnNote(targetNote.noteObject);
                    activeNotes.RemoveAt(targetNoteIndex);
                    TriggerEarlyMiss(lane);
                }
            }
        }
    }

    private void EvaluateAnswer(bool isCorrect, RhythmLane lane)
    {
        // Instantly hide the entire MCQ side panel
        if (mcqPanel != null) mcqPanel.SetActive(false);
        else if (floatingQuestionText != null) floatingQuestionText.gameObject.SetActive(false);

        if (isCorrect)
        {
            RegisterHit("CORRECT!", 0f, -25f, Color.green, 1.5f, lane); 
            pendingArchitectStat += 10; 
        }
        else
        {
            TriggerMiss(lane);
            pendingSuspicionStat += 5; 
        }

        currentPhaseIndex++;
        StartPhase(currentPhaseIndex);
    }

    private void ClearAllNotes()
    {
        foreach (var an in activeNotes)
        {
            if (an.isAnswer) Destroy(an.noteObject); 
            else NotePooler.Instance.ReturnNote(an.noteObject); 
        }
        activeNotes.Clear();
    }

    // --- Hit/Miss/UI Visuals ---

    private void RegisterHit(string feedback, float bossDamage, float fleetDamage, Color color, float scale, RhythmLane lane)
    {
        if (feedback == "PERFECT!") perfectHits++;
        else if (feedback == "GOOD") goodHits++;

        if (activeTextCoroutine != null) StopCoroutine(activeTextCoroutine);
        activeTextCoroutine = StartCoroutine(AnimateFeedbackText(feedback, color, scale, false));

        bossHP = Mathf.Max(1f, bossHP - bossDamage); 
        fleetHP = Mathf.Min(100f, Mathf.Max(0f, fleetHP - fleetDamage)); 
        UpdateHPUI();

        
        if (activeBossFlashCoroutine != null) StopCoroutine(activeBossFlashCoroutine);
        activeBossFlashCoroutine = StartCoroutine(FlashBossRoutine());
        
        if (feedback == "PERFECT!" || feedback == "CORRECT!") SpawnBinaryExplosion(hitLines[(int)lane].position);
        
        if (sfxSource != null && hitSound != null) sfxSource.PlayOneShot(hitSound);
        if (activeBackgroundPulse != null) StopCoroutine(activeBackgroundPulse);
        activeBackgroundPulse = StartCoroutine(PulseBackgroundRoutine(hitPulseColor)); 
    }

    private void TriggerMiss(RhythmLane lane)
    {
        misses++;
        if (activeTextCoroutine != null) StopCoroutine(activeTextCoroutine);
        activeTextCoroutine = StartCoroutine(AnimateFeedbackText("MISS!", Color.red, 1.2f, true));

        fleetHP = Mathf.Max(0f, fleetHP - 10f);
        UpdateHPUI();

        StartCoroutine(FlashFleetAndShakeRoutine());
        if (fleetImages[(int)lane] != null) SpawnSmokePuff(fleetImages[(int)lane].transform.position);

        if (fleetHP <= 0) EndBattle(false);

        if (sfxSource != null && missSound != null) sfxSource.PlayOneShot(missSound);
        if (activeBackgroundPulse != null) StopCoroutine(activeBackgroundPulse);
        activeBackgroundPulse = StartCoroutine(PulseBackgroundRoutine(missPulseColor));
    }

    private void TriggerEarlyMiss(RhythmLane lane)
    {
        earlyMisses++;
        if (activeTextCoroutine != null) StopCoroutine(activeTextCoroutine);
        activeTextCoroutine = StartCoroutine(AnimateFeedbackText("TOO EARLY!", new Color(1f, 0.5f, 0f), 1.2f, true));

        fleetHP = Mathf.Max(0f, fleetHP - 10f);
        UpdateHPUI();

        StartCoroutine(FlashFleetAndShakeRoutine());
        if (fleetImages[(int)lane] != null) SpawnSmokePuff(fleetImages[(int)lane].transform.position);

        if (fleetHP <= 0) EndBattle(false);

        if (sfxSource != null && missSound != null) sfxSource.PlayOneShot(missSound);
        if (activeBackgroundPulse != null) StopCoroutine(activeBackgroundPulse);
        activeBackgroundPulse = StartCoroutine(PulseBackgroundRoutine(missPulseColor));
    }

    private void UpdateHPUI()
    {
        if (bossHealthBar != null) bossHealthBar.value = bossHP / 100f;
        if (fleetHealthBar != null) fleetHealthBar.value = fleetHP / 100f;
    }

    private void EndBattle(bool won)
    {
        currentState = BattleState.GameOver;
        battleWon = won;
        if (musicSource != null) musicSource.Stop();
        ClearAllNotes();
        
        string title = won ? "<color=green>MAIN.CPP REFACTORED!</color>" : "<color=red>FLEET DESTROYED</color>";
        
        // Calculate Code Quality Bonus based on stats
        if (won)
        {
            pendingCodeQuality = 20 + (perfectHits / 2) - (misses * 2);
            pendingCodeQuality = Mathf.Max(5, pendingCodeQuality); // Minimum 5 for winning
        }
        else
        {
            pendingCodeQuality = -15; // Penalty for defeat if they continue
        }

        string statsStr = $"Perfect: {perfectHits}\nGood: {goodHits}\nMissed: {misses}\nToo Early: {earlyMisses}\n\n";
        statsStr += won ? $"<color=green>Code Quality Earned: +{pendingCodeQuality}</color>" : $"<color=red>Code Quality Penalty: {pendingCodeQuality}</color>";

        // Show the Result Menu
        ShowMenu(title, statsStr, false, false, true, true);
    }

    // --- Coroutines (Flashes, Text, Particles) ---
    private IEnumerator AnimateFeedbackText(string feedback, Color textColor, float startScale, bool shake)
    {
        hitFeedbackText.text = feedback;
        hitFeedbackText.color = textColor;
        RectTransform rt = hitFeedbackText.rectTransform;
        rt.localScale = Vector3.one * startScale;
        rt.anchoredPosition = originalFeedbackPos;

        float duration = 0.5f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            Color c = hitFeedbackText.color;
            c.a = Mathf.Lerp(1f, 0f, progress);
            hitFeedbackText.color = c;
            rt.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, progress);

            if (shake) rt.anchoredPosition = originalFeedbackPos + new Vector3(Mathf.Sin(elapsed * 50f) * 10f, -20f * progress, 0);
            else rt.anchoredPosition = originalFeedbackPos + new Vector3(0, 30f * progress, 0); 
            
            yield return null;
        }
        rt.anchoredPosition = originalFeedbackPos; 
        hitFeedbackText.text = "";
    }

    private IEnumerator FlashLaneRoutine(int laneIndex)
    {
        if (laneBackgrounds == null || laneBackgrounds.Length <= laneIndex || laneBackgrounds[laneIndex] == null) yield break;
        Image laneImg = laneBackgrounds[laneIndex];
        laneImg.color = new Color(0.2f, 0.8f, 1f, 0.6f); 
        float duration = 0.2f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = laneImg.color;
            c.a = Mathf.Lerp(0.6f, 0f, elapsed / duration);
            laneImg.color = c;
            yield return null;
        }
        laneImg.color = new Color(1f, 1f, 1f, 0f); 
    }

    private IEnumerator FlashBossRoutine()
    {
        if (bossHeaderBackground == null) yield break;
        
        bossHeaderBackground.color = Color.white;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            bossHeaderBackground.color = Color.Lerp(Color.white, trueBossHeaderColor, elapsed / 0.15f);
            yield return null;
        }
        bossHeaderBackground.color = trueBossHeaderColor;
    }

    private IEnumerator FlashFleetAndShakeRoutine()
    {
        foreach (var ship in fleetImages) if (ship != null) ship.color = Color.red;
        Vector3 originalContainerPos = battleContainer != null ? battleContainer.anchoredPosition : Vector3.zero;
        float elapsed = 0f, duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (battleContainer != null)
            {
                float offsetX = UnityEngine.Random.Range(-15f, 15f) * (1f - (elapsed / duration));
                float offsetY = UnityEngine.Random.Range(-15f, 15f) * (1f - (elapsed / duration));
                battleContainer.anchoredPosition = originalContainerPos + new Vector3(offsetX, offsetY, 0);
            }
            foreach (var ship in fleetImages) if (ship != null) ship.color = Color.Lerp(Color.red, Color.white, elapsed / duration);
            yield return null;
        }
        if (battleContainer != null) battleContainer.anchoredPosition = originalContainerPos;
    }

    public void SpawnBinaryExplosion(Vector3 screenPos)
    {
        if (uiTextParticlePrefab == null) return;
        for (int i = 0; i < 6; i++) 
        {
            GameObject p = Instantiate(uiTextParticlePrefab, battleContainer);
            p.transform.position = screenPos;
            p.GetComponent<TextMeshProUGUI>().text = UnityEngine.Random.value > 0.5f ? "1" : "0";
            p.GetComponent<TextMeshProUGUI>().color = Color.cyan;
            Vector3 direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1.5f), 0).normalized;
            StartCoroutine(AnimateUIParticle(p.GetComponent<RectTransform>(), direction, 150f, 0.6f, true));
        }
    }

    public void SpawnSmokePuff(Vector3 screenPos)
    {
        if (uiSmokeParticlePrefab == null) return;
        for (int i = 0; i < 4; i++) 
        {
            GameObject p = Instantiate(uiSmokeParticlePrefab, battleContainer);
            p.transform.position = screenPos;
            Vector3 direction = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(0.5f, 1f), 0).normalized;
            StartCoroutine(AnimateUIParticle(p.GetComponent<RectTransform>(), direction, 100f, 0.8f, false));
        }
    }

    private IEnumerator AnimateUIParticle(RectTransform rt, Vector3 direction, float speed, float duration, bool spin)
    {
        float elapsed = 0f;
        Graphic graphic = rt.GetComponent<Graphic>();
        Vector3 randomRotation = new Vector3(0, 0, UnityEngine.Random.Range(-200f, 200f));
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            rt.position += direction * speed * Time.deltaTime;
            if (spin) rt.Rotate(randomRotation * Time.deltaTime);
            if (graphic != null)
            {
                Color c = graphic.color;
                c.a = Mathf.Lerp(1f, 0f, progress);
                graphic.color = c;
            }
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    private IEnumerator CountdownRoutine(bool isResume)
    {
        string[] steps = { "3", "2", "1", "<color=green>START!</color>" };
        
        foreach (string step in steps)
        {
            hitFeedbackText.text = step;
            hitFeedbackText.color = Color.white;
            hitFeedbackText.rectTransform.localScale = Vector3.one * 1.5f;
            hitFeedbackText.rectTransform.anchoredPosition = originalFeedbackPos;
            yield return new WaitForSecondsRealtime(0.75f);
        }
        hitFeedbackText.text = "";

        musicSource.volume = 0f;

        if (isResume)
        {
            phaseStartDspTime += (AudioSettings.dspTime - pauseStartDspTime);
            musicSource.Play();
            currentState = BattleState.Playing;
        }
        else
        {
            StartPhase(0); 
        }

        // Fade music in
        float fadeDuration = 1.0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        musicSource.volume = 1f;
    }

    private IEnumerator PulseBackgroundRoutine(Color pulseColor)
    {
        if (mainBackgroundImage == null) yield break;
        
        float duration = 0.2f;
        float elapsed = 0f;
        mainBackgroundImage.color = pulseColor;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mainBackgroundImage.color = Color.Lerp(pulseColor, originalBackgroundColor, elapsed / duration);
            yield return null;
        }
        mainBackgroundImage.color = originalBackgroundColor;
    }
}