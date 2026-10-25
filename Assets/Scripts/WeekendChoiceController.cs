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

    // This is only the current UI selection.
    // It is not stored permanently until Confirm is pressed.
    private WeekendChoice? pendingChoice;

    // Prevents the confirmation process from running twice.
    private bool isConfirming;


    private void Start()
    {
        gameManager = GameManager.EnsureInstance();

        SetupToggleGroup();
        SetupChoiceAvailability();
        SetupToggleListeners();

        // Start with no weekend choice selected.
        ClearSelection();

        // Confirmation is impossible until something is selected.
        confirmButton.interactable = false;
    }


    // =========================================================
    // Setup
    // =========================================================

    private void SetupToggleGroup()
    {
        steveToggle.group = weekendToggleGroup;
        studyToggle.group = weekendToggleGroup;
        connectionsToggle.group = weekendToggleGroup;
        wallowToggle.group = weekendToggleGroup;

        // Initially allow every toggle to be off,
        // because the player has not selected anything yet.
        weekendToggleGroup.allowSwitchOff = true;
    }

    public void SetupChoiceAvailability()
    {
        bool steveAvailable =
            gameManager.IsWeekendChoiceAvailable(
                WeekendChoice.Steve
            );

        bool studyAvailable =
            gameManager.IsWeekendChoiceAvailable(
                WeekendChoice.Study
            );

        bool connectionsAvailable =
            gameManager.IsWeekendChoiceAvailable(
                WeekendChoice.PersonalConnections
            );

        // Wallow is always available.
        bool wallowAvailable = true;


        steveToggle.interactable = steveAvailable;
        studyToggle.interactable = studyAvailable;
        connectionsToggle.interactable = connectionsAvailable;
        wallowToggle.interactable = wallowAvailable;


        steveLabel.color =
            steveAvailable
                ? availableTextColor
                : unavailableTextColor;

        studyLabel.color =
            studyAvailable
                ? availableTextColor
                : unavailableTextColor;

        connectionsLabel.color =
            connectionsAvailable
                ? availableTextColor
                : unavailableTextColor;

        wallowLabel.color =
            wallowAvailable
                ? availableTextColor
                : unavailableTextColor;
    }

    private void SetupToggleListeners()
    {
        steveToggle.onValueChanged.AddListener(
            OnSteveToggleChanged
        );

        studyToggle.onValueChanged.AddListener(
            OnStudyToggleChanged
        );

        connectionsToggle.onValueChanged.AddListener(
            OnConnectionsToggleChanged
        );

        wallowToggle.onValueChanged.AddListener(
            OnWallowToggleChanged
        );
    }

    private void ClearSelection()
    {
        pendingChoice = null;

        steveToggle.SetIsOnWithoutNotify(false);
        studyToggle.SetIsOnWithoutNotify(false);
        connectionsToggle.SetIsOnWithoutNotify(false);
        wallowToggle.SetIsOnWithoutNotify(false);
    }


    // =========================================================
    // Toggle Events
    // =========================================================

    private void OnSteveToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SelectChoice(WeekendChoice.Steve);
        }
    }

    private void OnStudyToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SelectChoice(WeekendChoice.Study);
        }
    }

    private void OnConnectionsToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SelectChoice(WeekendChoice.PersonalConnections);
        }
    }

    private void OnWallowToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SelectChoice(WeekendChoice.Wallow);
        }
    }


    // =========================================================
    // Selection
    // =========================================================

    private void SelectChoice(WeekendChoice choice)
    {
        if (!gameManager.IsWeekendChoiceAvailable(choice))
        {
            return;
        }

        pendingChoice = choice;

        // Once the player has selected something, don't let them
        // return to a state where nothing is selected.
        //
        // They can still freely switch to another available choice.
        weekendToggleGroup.allowSwitchOff = false;

        confirmButton.interactable = true;
    }


    // =========================================================
    // Confirmation
    // =========================================================

    public void ConfirmChoice()
    {
        // Protect against repeated confirmation clicks.
        if (isConfirming)
        {
            return;
        }

        // No selection has been made.
        if (!pendingChoice.HasValue)
        {
            return;
        }

        WeekendChoice choice = pendingChoice.Value;

        // GameManager performs the final validation and
        // permanently records the choice.
        if (!gameManager.TryConfirmWeekendChoice(choice))
        {
            return;
        }

        isConfirming = true;

        // Immediately disable the UI before loading the next scene.
        confirmButton.interactable = false;

        steveToggle.interactable = false;
        studyToggle.interactable = false;
        connectionsToggle.interactable = false;
        wallowToggle.interactable = false;

        LoadWeekendScene(choice);
    }


    // =========================================================
    // Scene Loading
    // =========================================================

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


    // =========================================================
    // Cleanup
    // =========================================================

    private void OnDestroy()
    {
        steveToggle.onValueChanged.RemoveListener(
            OnSteveToggleChanged
        );

        studyToggle.onValueChanged.RemoveListener(
            OnStudyToggleChanged
        );

        connectionsToggle.onValueChanged.RemoveListener(
            OnConnectionsToggleChanged
        );

        wallowToggle.onValueChanged.RemoveListener(
            OnWallowToggleChanged
        );
    }
}