using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Day 5's small "Trace the Turn" lesson. The player clicks five cards in the
/// order the game should process a turn. Completion publishes a named narrative
/// event so DayNarrativeExecutor can continue without knowing Day 5 details.
///
/// Put this component on an always-active GameObject. Assign miniGameRoot to a
/// child panel; do not put this component on miniGameRoot itself because the
/// controller needs to stay active while the panel is hidden.
/// </summary>
public class Day5TraceTurnMiniGame : MonoBehaviour
{
    public const string DefaultTriggerStepID = "D5_TraceTurn";
    public const string DefaultCompletionEventKey = "Day5.TraceTurn.Complete";

    private static readonly string[] CardLabels =
    {
        "Receive Player Input",
        "Validate and Resolve Attack",
        "Update Game State",
        "Check Terminal Condition",
        "Begin AI Turn"
    };

    [Header("Narrative Connection")]
    [SerializeField] private int dayNumber = 5;
    [SerializeField] private string triggerStepID = DefaultTriggerStepID;
    [SerializeField] private string completionEventKey = DefaultCompletionEventKey;

    [Header("UI")]
    [SerializeField] private GameObject miniGameRoot;
    [SerializeField] private List<Day5TraceCard> cards = new List<Day5TraceCard>();
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button submitButton;

    [Header("Failure Consequence")]
    [Tooltip("After this many incorrect submissions, apply the suspicion penalty once.")]
    [Min(1)] [SerializeField] private int attemptsBeforeSuspicion = 2;
    [SerializeField] private int repeatedFailureSuspicion = 1;

    private readonly List<Day5TraceCard> selectedCards = new List<Day5TraceCard>();
    private bool miniGameActive;
    private bool completedForCurrentVisit;
    private bool suspicionApplied;
    private int wrongAttempts;
    private string lastObservedStep;

    private void Awake()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetSelection);
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitSequence);
            submitButton.interactable = false;
        }

        ConfigureCards();

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetSelection);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitSequence);
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        string currentStep = gm.CurrentStep;
        if (currentStep != lastObservedStep)
        {
            // Allow the activity to be replayed if testing returns to this step later.
            if (lastObservedStep == triggerStepID && currentStep != triggerStepID)
            {
                completedForCurrentVisit = false;
                HideMiniGame();
            }

            lastObservedStep = currentStep;
        }

        if (gm.CurrentDay == dayNumber &&
            currentStep == triggerStepID &&
            !miniGameActive &&
            !completedForCurrentVisit)
        {
            BeginMiniGame();
        }
    }

    private void ConfigureCards()
    {
        if (cards.Count != CardLabels.Length)
        {
            Debug.LogWarning($"[Day5TraceTurnMiniGame] Expected {CardLabels.Length} cards but found {cards.Count}. Assign exactly five cards in the Inspector.", this);
        }

        int count = Mathf.Min(cards.Count, CardLabels.Length);
        for (int i = 0; i < count; i++)
        {
            if (cards[i] != null)
            {
                cards[i].Configure(this, i, CardLabels[i]);
            }
        }
    }

    private void BeginMiniGame()
    {
        miniGameActive = true;
        wrongAttempts = 0;
        suspicionApplied = false;
        ResetSelection();

        if (feedbackText != null)
        {
            feedbackText.text = "Click the cards in the order a safe player turn should be processed.";
        }

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(true);
        }
    }

    public void SelectCard(Day5TraceCard card)
    {
        if (!miniGameActive || card == null || selectedCards.Contains(card)) return;

        selectedCards.Add(card);
        card.MarkSelected(selectedCards.Count - 1);

        if (submitButton != null)
        {
            submitButton.interactable = selectedCards.Count == CardLabels.Length;
        }
    }

    public void ResetSelection()
    {
        selectedCards.Clear();

        foreach (Day5TraceCard card in cards)
        {
            card?.ResetCard();
        }

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }
    }

    public void SubmitSequence()
    {
        if (!miniGameActive || selectedCards.Count != CardLabels.Length) return;

        bool correct = true;
        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] == null || selectedCards[i].CorrectOrderIndex != i)
            {
                correct = false;
                break;
            }
        }

        if (correct)
        {
            CompleteMiniGame();
            return;
        }

        wrongAttempts++;

        if (feedbackText != null)
        {
            int terminalIndex = selectedCards.FindIndex(c => c != null && c.CorrectOrderIndex == 3);
            int aiIndex = selectedCards.FindIndex(c => c != null && c.CorrectOrderIndex == 4);

            feedbackText.text = aiIndex >= 0 && terminalIndex >= 0 && aiIndex < terminalIndex
                ? "Posthumous retaliation: the AI got a turn before the terminal-state check. Try again."
                : "That order leaves part of the previous action unresolved. Try tracing it again.";
        }

        if (!suspicionApplied &&
            wrongAttempts >= attemptsBeforeSuspicion &&
            repeatedFailureSuspicion != 0 &&
            GameManager.Instance != null)
        {
            GameManager.Instance.ChangeSteveSuspicion(repeatedFailureSuspicion);
            suspicionApplied = true;
        }

        ResetSelection();
    }

    private void CompleteMiniGame()
    {
        completedForCurrentVisit = true;
        miniGameActive = false;

        if (feedbackText != null)
        {
            feedbackText.text = "Correct: resolve the action and check for a terminal state before handing over the turn.";
        }

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }

        GameplayEvents.TriggerNarrativeEvent(completionEventKey);
    }

    private void HideMiniGame()
    {
        miniGameActive = false;
        ResetSelection();

        if (miniGameRoot != null)
        {
            miniGameRoot.SetActive(false);
        }
    }
}
