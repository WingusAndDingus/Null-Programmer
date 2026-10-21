using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Runtime controller for the existing Day5MiniGameCanvas prefab.
/// Keep this component on the always-active Day5TraceTurnController GameObject,
/// not on the canvas that it hides.
/// </summary>
public class Day5TraceTurnMiniGame : MonoBehaviour
{
    public const string DefaultCompletionEventKey = Day5NarrativeEvents.TraceTurnComplete;

    private static readonly string[] CardLabels =
    {
        "Receive Player Input",
        "Validate and Resolve Attack",
        "Update Game State",
        "Check Terminal Condition",
        "Begin AI Turn"
    };

    [FormerlySerializedAs("panelRoot")]
    [SerializeField] private GameObject miniGameRoot;

    [SerializeField] private List<Day5TraceCard> cards = new List<Day5TraceCard>();
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button submitButton;

    [Header("Optional consequence")]
    [SerializeField] private int wrongAttemptsBeforeSuspicion = 2;
    [SerializeField] private int suspicionIncrease = 1;

    private readonly List<Day5TraceCard> selectedCards = new List<Day5TraceCard>();
    private bool stepActive;
    private bool suspicionApplied;
    private int wrongAttempts;
    private bool completing;
    private int lastSubmitFrame = -1;

    private void Awake()
    {
        if (miniGameRoot == gameObject)
        {
            Debug.LogError("Day5TraceTurnMiniGame must stay on the separate Day5TraceTurnController object, not on the canvas it hides.", this);
            enabled = false;
            return;
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetOrder);
            resetButton.onClick.AddListener(ResetOrder);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitOrder);
            submitButton.onClick.AddListener(SubmitOrder);
        }

        if (miniGameRoot != null) miniGameRoot.SetActive(false);
    }

    private void Start()
    {
        if (cards.Count != CardLabels.Length)
        {
            Debug.LogError($"Day 5 Trace the Turn needs exactly {CardLabels.Length} cards. Current list has {cards.Count}.", this);
        }
    }

    private void Update()
    {
        GameManager manager = GameManager.Instance;
        bool shouldBeActive = manager != null &&
                              manager.CurrentDay == 5 &&
                              manager.CurrentStep == "D5_TraceTurn";

        if (!shouldBeActive)
        {
            if (stepActive)
            {
                stepActive = false;
                completing = false;
                if (miniGameRoot != null) miniGameRoot.SetActive(false);
            }
            return;
        }

        if (!stepActive)
        {
            stepActive = true;
            OpenMiniGame();
        }
    }

    private void OpenMiniGame()
    {
        wrongAttempts = 0;
        suspicionApplied = false;
        completing = false;

        if (miniGameRoot != null) miniGameRoot.SetActive(true);

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            string text = i < CardLabels.Length ? CardLabels[i] : $"Step {i + 1}";
            cards[i].Configure(this, i, text);
            cards[i].SetInteractable(true);
        }

        ResetOrder();
    }

    public void SelectCard(Day5TraceCard card)
    {
        if (!stepActive || completing || card == null || card.IsSelected) return;

        selectedCards.Add(card);
        card.SetSelectionOrder(selectedCards.Count);
        UpdateSubmitButton();
    }

    // Public names are intentionally simple so an already-authored prefab can
    // keep or easily rebind its Button OnClick entries.
    public void ResetOrder()
    {
        if (completing) return;

        selectedCards.Clear();
        foreach (Day5TraceCard card in cards)
        {
            if (card != null)
            {
                card.SetSelectionOrder(0);
                card.SetInteractable(true);
            }
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Click the five steps in the order they should happen, then press Submit.";
        }

        UpdateSubmitButton();
    }

    public void SubmitOrder()
    {
        // A reused prefab may already have a persistent OnClick entry while this script
        // also binds at runtime. Debounce same-frame duplicate invocations.
        if (lastSubmitFrame == Time.frameCount) return;
        lastSubmitFrame = Time.frameCount;

        if (completing || selectedCards.Count != CardLabels.Length) return;

        if (IsCorrect())
        {
            completing = true;
            if (feedbackText != null)
            {
                feedbackText.text = "Correct. The terminal-state check happens before control can pass to the AI.";
            }

            foreach (Day5TraceCard card in cards)
            {
                if (card != null) card.SetInteractable(false);
            }

            if (resetButton != null) resetButton.interactable = false;
            if (submitButton != null) submitButton.interactable = false;
            StartCoroutine(CompleteAfterShortPause());
            return;
        }

        wrongAttempts++;

        if (feedbackText != null)
        {
            feedbackText.text = AIComesBeforeTerminalCheck()
                ? "That still allows posthumous retaliation. The AI receives control before the game establishes that the match is over."
                : "Not quite. Trace one complete player action: input, resolve it, update the state, check whether the game ended, then pass control.";
        }

        if (!suspicionApplied && wrongAttempts >= Mathf.Max(1, wrongAttemptsBeforeSuspicion))
        {
            suspicionApplied = true;
            if (GameManager.Instance != null && suspicionIncrease != 0)
            {
                GameManager.Instance.ChangeSteveSuspicion(suspicionIncrease);
            }
        }
    }

    private bool IsCorrect()
    {
        if (selectedCards.Count != CardLabels.Length) return false;

        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] == null || selectedCards[i].CorrectIndex != i)
            {
                return false;
            }
        }

        return true;
    }

    private bool AIComesBeforeTerminalCheck()
    {
        int aiPosition = selectedCards.FindIndex(card => card != null && card.CorrectIndex == 4);
        int terminalPosition = selectedCards.FindIndex(card => card != null && card.CorrectIndex == 3);
        return aiPosition >= 0 && terminalPosition >= 0 && aiPosition < terminalPosition;
    }

    private IEnumerator CompleteAfterShortPause()
    {
        yield return new WaitForSecondsRealtime(5.0f);

        if (miniGameRoot != null) miniGameRoot.SetActive(false);
        GameplayEvents.TriggerNarrativeEvent(DefaultCompletionEventKey);
    }

    private void UpdateSubmitButton()
    {
        if (submitButton != null)
        {
            submitButton.interactable = !completing && selectedCards.Count == CardLabels.Length;
        }

        if (resetButton != null)
        {
            resetButton.interactable = !completing && selectedCards.Count > 0;
        }
    }
}
