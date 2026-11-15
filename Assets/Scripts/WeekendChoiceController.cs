using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class WeekendChoiceController : MonoBehaviour
{
    [Header("Weekend Choice Toggles")]
    [SerializeField] private Toggle steveToggle;
    [SerializeField] private Toggle studyToggle;
    [SerializeField] private Toggle connectionsToggle;
    [SerializeField] private Toggle wallowToggle;

    [Header("Toggle Group")]
    [SerializeField] private ToggleGroup weekendToggleGroup;

    [Header("Confirmation")]
    [SerializeField] private Button confirmButton;

    [Header("Weekend Scenes")]
    [SerializeField] private string steveSceneName = "WeekendSteve";
    [SerializeField] private string studySceneName = "WeekendStudy";
    [SerializeField] private string connectionsSceneName = "WeekendConnections";
    [SerializeField] private string wallowSceneName = "WeekendWallow";

    [Header("Choice Labels")]
    [SerializeField] private TMP_Text steveLabel;
    [SerializeField] private TMP_Text studyLabel;
    [SerializeField] private TMP_Text connectionsLabel;
    [SerializeField] private TMP_Text wallowLabel;

    [Header("Choice Colors")]
    [SerializeField] private Color availableTextColor = Color.white;
    [SerializeField] private Color unavailableTextColor = Color.gray;

    private GameManager gameManager;

    // This is only the current UI selection. It is committed to GameManager on Confirm.
    private WeekendChoice? pendingChoice;

    // A choice gets its personality reaction only once during this visit to the weekend screen.
    private readonly HashSet<WeekendChoice> choicesWithCommentary = new HashSet<WeekendChoice>();

    private bool isConfirming;
    private bool isPlayingCommentary;

    private void Start()
    {
        gameManager = GameManager.EnsureInstance();

        SetupToggleGroup();
        SetupChoiceAvailability();
        SetupToggleListeners();
        SetupPointerClickRelays();

        ClearSelection();
        if (confirmButton != null)
            confirmButton.interactable = false;
    }

    // =========================================================
    // Setup
    // =========================================================

    private void SetupToggleGroup()
    {
        if (weekendToggleGroup == null)
        {
            Debug.LogError("[WeekendChoiceController] Weekend Toggle Group is not assigned.", this);
            return;
        }

        if (steveToggle != null) steveToggle.group = weekendToggleGroup;
        if (studyToggle != null) studyToggle.group = weekendToggleGroup;
        if (connectionsToggle != null) connectionsToggle.group = weekendToggleGroup;
        if (wallowToggle != null) wallowToggle.group = weekendToggleGroup;

        // The player starts with nothing selected and may switch between available choices.
        weekendToggleGroup.allowSwitchOff = true;
    }

    public void SetupChoiceAvailability()
    {
        if (gameManager == null)
            gameManager = GameManager.EnsureInstance();

        bool steveAvailable = gameManager.IsWeekendChoiceAvailable(WeekendChoice.Steve);
        bool studyAvailable = gameManager.IsWeekendChoiceAvailable(WeekendChoice.Study);
        bool connectionsAvailable = gameManager.IsWeekendChoiceAvailable(WeekendChoice.PersonalConnections);

        // Wallow is deliberately always accessible, even if another option is available.
        const bool wallowAvailable = true;

        SetChoiceAvailability(steveToggle, steveLabel, steveAvailable);
        SetChoiceAvailability(studyToggle, studyLabel, studyAvailable);
        SetChoiceAvailability(connectionsToggle, connectionsLabel, connectionsAvailable);
        SetChoiceAvailability(wallowToggle, wallowLabel, wallowAvailable);
    }

    private void SetChoiceAvailability(Toggle toggle, TMP_Text label, bool available)
    {
        if (toggle != null) toggle.interactable = available;
        if (label != null) label.color = available ? availableTextColor : unavailableTextColor;
    }

    private void SetupToggleListeners()
    {
        if (steveToggle != null) steveToggle.onValueChanged.AddListener(OnSteveToggleChanged);
        if (studyToggle != null) studyToggle.onValueChanged.AddListener(OnStudyToggleChanged);
        if (connectionsToggle != null) connectionsToggle.onValueChanged.AddListener(OnConnectionsToggleChanged);
        if (wallowToggle != null) wallowToggle.onValueChanged.AddListener(OnWallowToggleChanged);
    }

    private void SetupPointerClickRelays()
    {
        // A disabled Toggle does not raise onValueChanged. The relay still receives pointer
        // clicks, allowing locked options to explain why they are unavailable.
        AddPointerClickRelay(steveToggle, WeekendChoice.Steve);
        AddPointerClickRelay(studyToggle, WeekendChoice.Study);
        AddPointerClickRelay(connectionsToggle, WeekendChoice.PersonalConnections);
        AddPointerClickRelay(wallowToggle, WeekendChoice.Wallow);
    }

    private void AddPointerClickRelay(Toggle toggle, WeekendChoice choice)
    {
        if (toggle == null) return;

        WeekendChoiceClickRelay relay = toggle.GetComponent<WeekendChoiceClickRelay>();
        if (relay == null)
            relay = toggle.gameObject.AddComponent<WeekendChoiceClickRelay>();

        WeekendChoice capturedChoice = choice;
        relay.Initialize(() => OnWeekendOptionClicked(capturedChoice));
    }

    private void ClearSelection()
    {
        pendingChoice = null;

        if (steveToggle != null) steveToggle.SetIsOnWithoutNotify(false);
        if (studyToggle != null) studyToggle.SetIsOnWithoutNotify(false);
        if (connectionsToggle != null) connectionsToggle.SetIsOnWithoutNotify(false);
        if (wallowToggle != null) wallowToggle.SetIsOnWithoutNotify(false);
    }

    // =========================================================
    // Toggle Events
    // =========================================================

    private void OnSteveToggleChanged(bool isOn)
    {
        if (isOn) SelectChoice(WeekendChoice.Steve);
    }

    private void OnStudyToggleChanged(bool isOn)
    {
        if (isOn) SelectChoice(WeekendChoice.Study);
    }

    private void OnConnectionsToggleChanged(bool isOn)
    {
        if (isOn) SelectChoice(WeekendChoice.PersonalConnections);
    }

    private void OnWallowToggleChanged(bool isOn)
    {
        if (isOn) SelectChoice(WeekendChoice.Wallow);
    }

    // =========================================================
    // Selection and first-click commentary
    // =========================================================

    private void SelectChoice(WeekendChoice choice)
    {
        if (isConfirming || isPlayingCommentary || !gameManager.IsWeekendChoiceAvailable(choice))
            return;

        pendingChoice = choice;

        if (weekendToggleGroup != null)
            weekendToggleGroup.allowSwitchOff = false;

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnWeekendOptionClicked(WeekendChoice choice)
    {
        if (isConfirming || isPlayingCommentary || choicesWithCommentary.Contains(choice))
            return;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null)
        {
            Debug.LogWarning("[WeekendChoiceController] DialogueManager is unavailable; weekend-choice commentary was skipped.", this);
            return;
        }

        choicesWithCommentary.Add(choice);

        // Commit the visual selection here as well as in Toggle.onValueChanged.
        // This removes any ordering dependency between Unity's Toggle pointer handler
        // and the click relay that also needs to work for disabled (locked) Toggles.
        if (gameManager.IsWeekendChoiceAvailable(choice))
        {
            SetToggleSelectionWithoutNotify(choice);
            pendingChoice = choice;
            if (weekendToggleGroup != null)
                weekendToggleGroup.allowSwitchOff = false;
        }

        _ = PlayChoiceCommentaryAsync(choice, dialogue);
    }

    private void SetToggleSelectionWithoutNotify(WeekendChoice choice)
    {
        if (steveToggle != null) steveToggle.SetIsOnWithoutNotify(choice == WeekendChoice.Steve);
        if (studyToggle != null) studyToggle.SetIsOnWithoutNotify(choice == WeekendChoice.Study);
        if (connectionsToggle != null) connectionsToggle.SetIsOnWithoutNotify(choice == WeekendChoice.PersonalConnections);
        if (wallowToggle != null) wallowToggle.SetIsOnWithoutNotify(choice == WeekendChoice.Wallow);
    }

    private async Task PlayChoiceCommentaryAsync(WeekendChoice choice, DialogueManager dialogue)
    {
        isPlayingCommentary = true;
        SetChoiceControlsInteractable(false);

        try
        {
            List<DialogueLine> lines = BuildCommentary(choice);
            foreach (DialogueLine line in lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                    continue;

                await dialogue.DisplayLineAndWaitAsync(line);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            isPlayingCommentary = false;
            SetupChoiceAvailability();

            if (confirmButton != null)
            {
                confirmButton.interactable = pendingChoice.HasValue &&
                    gameManager.IsWeekendChoiceAvailable(pendingChoice.Value);
            }
        }
    }

    private void SetChoiceControlsInteractable(bool interactable)
    {
        if (steveToggle != null) steveToggle.interactable = interactable && gameManager.IsWeekendChoiceAvailable(WeekendChoice.Steve);
        if (studyToggle != null) studyToggle.interactable = interactable && gameManager.IsWeekendChoiceAvailable(WeekendChoice.Study);
        if (connectionsToggle != null) connectionsToggle.interactable = interactable && gameManager.IsWeekendChoiceAvailable(WeekendChoice.PersonalConnections);
        if (wallowToggle != null) wallowToggle.interactable = interactable; // Wallow always remains an available option.

        if (confirmButton != null)
            confirmButton.interactable = interactable && pendingChoice.HasValue &&
                gameManager.IsWeekendChoiceAvailable(pendingChoice.Value);
    }

    private List<DialogueLine> BuildCommentary(WeekendChoice choice)
    {
        switch (choice)
        {
            case WeekendChoice.Study:
                return gameManager.IsWeekendChoiceAvailable(WeekendChoice.Study)
                    ? new List<DialogueLine>
                    {
                        Voice(HeadPersonality.GrandArchitect,
                            "Excellent choice. A quiet weekend with the documentation, a clear plan, and time to rebuild the skills we once took for granted. Let's make something worthy of our old reputation.")
                    }
                    : new List<DialogueLine>
                    {
                        Voice(HeadPersonality.GrandArchitect,
                            "Ah. I would welcome a weekend of study, but our current programming skills are not quite where they need to be. We must first rebuild the foundations before attempting to restore the architecture.")
                    };

            case WeekendChoice.Steve:
                return gameManager.IsWeekendChoiceAvailable(WeekendChoice.Steve)
                    ? new List<DialogueLine>
                    {
                        Voice(HeadPersonality.Pragmatist,
                            "The best part of work isn't working. It's getting to know your coworkers. Coffee, conversation, and maybe letting Steve do the explaining for once.")
                    }
                    : new List<DialogueLine>
                    {
                        Voice(HeadPersonality.Pragmatist,
                            "A relaxing weekend with the colleague who currently suspects we're hiding something? That sounds less like team bonding and more like an interrogation with pastries.")
                    };

            case WeekendChoice.PersonalConnections:
                if (gameManager.IsWeekendChoiceAvailable(WeekendChoice.PersonalConnections))
                {
                    return new List<DialogueLine>
                    {
                        Voice(HeadPersonality.Detective,
                            "Perfect. More time with friends and family means more chances to piece together our past. Perhaps we'll finally learn what happened to us."),
                        Voice(HeadPersonality.Pragmatist,
                            "And we'll get to spend time with our loved ones."),
                        Voice(HeadPersonality.Detective,
                            "Yes, yes. That too.")
                    };
                }

                return new List<DialogueLine>
                {
                    Voice(HeadPersonality.Pragmatist,
                        "We could reach out, certainly. Though based on our reply rate, the group chat may have declared us legally deceased."),
                    Voice(HeadPersonality.Detective,
                        "Our connections are thinner than we thought. We need to rebuild trust before expecting everyone to clear their weekend for us.")
                };

            case WeekendChoice.Wallow:
                bool anotherOptionAvailable =
                    gameManager.IsWeekendChoiceAvailable(WeekendChoice.Steve) ||
                    gameManager.IsWeekendChoiceAvailable(WeekendChoice.Study) ||
                    gameManager.IsWeekendChoiceAvailable(WeekendChoice.PersonalConnections);

                if (anotherOptionAvailable)
                {
                    return new List<DialogueLine>
                    {
                        Voice(HeadPersonality.Pragmatist,
                            "We have perfectly good plans available, and you want to spend the weekend as a sentient duvet? Bold strategy."),
                        Voice(HeadPersonality.GrandArchitect,
                            "Rest is valuable. But choosing isolation when a path back to people or purpose is open is not the same as restoring ourselves."),
                        Voice(HeadPersonality.Detective,
                            "It may be avoidance. Perhaps we should ask why doing nothing feels safer than any of the alternatives.")
                    };
                }

                return new List<DialogueLine>
                {
                    Voice(HeadPersonality.Pragmatist,
                        "Well... at least nobody can accuse us of overcommitting. Our weekend calendar is beautifully, tragically empty."),
                    Voice(HeadPersonality.GrandArchitect,
                        "A quiet weekend might offer some rest. Still, I would rather it were a choice made freely than the only door left open."),
                    Voice(HeadPersonality.Detective,
                        "No invitations, no study plan, no Steve. Just silence. Perhaps that is the clue itself: we've let our world become very small.")
                };

            default:
                return new List<DialogueLine>();
        }
    }

    private static DialogueLine Voice(HeadPersonality personality, string text)
    {
        string speaker = personality switch
        {
            HeadPersonality.GrandArchitect => "The Grand Architect",
            HeadPersonality.Pragmatist => "The Pragmatist",
            HeadPersonality.Detective => "The Detective",
            _ => ""
        };

        return new DialogueLine
        {
            speakerName = speaker,
            personality = personality,
            text = text,
            displayDuration = 3.5f
        };
    }

    // =========================================================
    // Confirmation
    // =========================================================

    public void ConfirmChoice()
    {
        if (isConfirming || isPlayingCommentary || !pendingChoice.HasValue)
            return;

        WeekendChoice choice = pendingChoice.Value;
        if (!gameManager.TryConfirmWeekendChoice(choice))
            return;

        isConfirming = true;
        if (confirmButton != null) confirmButton.interactable = false;
        if (steveToggle != null) steveToggle.interactable = false;
        if (studyToggle != null) studyToggle.interactable = false;
        if (connectionsToggle != null) connectionsToggle.interactable = false;
        if (wallowToggle != null) wallowToggle.interactable = false;

        LoadWeekendScene(choice);
    }

    private void LoadWeekendScene(WeekendChoice choice)
    {
        switch (choice)
        {
            case WeekendChoice.Steve:
                SceneManager.LoadScene(steveSceneName);
                break;
            case WeekendChoice.Study:
                SceneManager.LoadScene(studySceneName);
                break;
            case WeekendChoice.PersonalConnections:
                SceneManager.LoadScene(connectionsSceneName);
                break;
            case WeekendChoice.Wallow:
                SceneManager.LoadScene(wallowSceneName);
                break;
        }
    }

    private void OnDestroy()
    {
        if (steveToggle != null) steveToggle.onValueChanged.RemoveListener(OnSteveToggleChanged);
        if (studyToggle != null) studyToggle.onValueChanged.RemoveListener(OnStudyToggleChanged);
        if (connectionsToggle != null) connectionsToggle.onValueChanged.RemoveListener(OnConnectionsToggleChanged);
        if (wallowToggle != null) wallowToggle.onValueChanged.RemoveListener(OnWallowToggleChanged);
    }
}
