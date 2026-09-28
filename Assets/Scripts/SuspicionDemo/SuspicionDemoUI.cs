using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Scene UI for shared suspicion state and a two-choice mock conversation.</summary>
public sealed class SuspicionDemoUI : MonoBehaviour
{
    [SerializeField] private Text meterLabel;
    [SerializeField] private TMP_Text meterLabelTMP;
    [SerializeField] private Image meterFill;
    [SerializeField] private Text warningLabel;
    [SerializeField] private TMP_Text warningLabelTMP;
    [SerializeField] private Button replyButton;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private TMP_Text dialogueTextTMP;
    [SerializeField] private Text feedbackText;
    [SerializeField] private TMP_Text feedbackTextTMP;
    [SerializeField] private Button choiceA;
    [SerializeField] private Button choiceB;
    [SerializeField] private Text choiceAText;
    [SerializeField] private TMP_Text choiceATextTMP;
    [SerializeField] private Text choiceBText;
    [SerializeField] private TMP_Text choiceBTextTMP;
    [SerializeField] private Button laterButton;
    [SerializeField] private Text laterText;
    [SerializeField] private TMP_Text laterTextTMP;
    [SerializeField] private GameObject testControls;
    [SerializeField] private Button addTest;
    [SerializeField] private Button subtractTest;
    [SerializeField] private Button resetTest;
    [SerializeField] private Text testFeedback;
    [SerializeField] private TMP_Text testFeedbackTMP;
    [SerializeField] private bool showTestControls = true;
    private GameManager state;
    private int presentedStage;
    private float nextChoiceTime;
    private readonly List<Collider2D> blocked2D = new List<Collider2D>();
    private readonly List<Collider> blocked3D = new List<Collider>();

    private static void SetLabel(Text legacy, TMP_Text tmp, string value)
    {
        if (tmp != null) tmp.text = value;
        else if (legacy != null) legacy.text = value;
    }

    private void Awake()
    {
        state = GameManager.EnsureInstance();
        if ((meterLabel == null && meterLabelTMP == null) || meterFill == null || (warningLabel == null && warningLabelTMP == null) || replyButton == null ||
            dialoguePanel == null || (dialogueText == null && dialogueTextTMP == null) || (feedbackText == null && feedbackTextTMP == null) || choiceA == null ||
            choiceB == null || (choiceAText == null && choiceATextTMP == null) || (choiceBText == null && choiceBTextTMP == null) || laterButton == null ||
            (laterText == null && laterTextTMP == null) || testControls == null || addTest == null || subtractTest == null ||
            resetTest == null || (testFeedback == null && testFeedbackTMP == null))
        {
            Debug.LogError("Suspicion UI references are missing. Undo/reinstall the demo.", this);
            enabled = false;
            return;
        }
        replyButton.onClick.AddListener(OpenReply);
        choiceA.onClick.AddListener(ChooseA);
        choiceB.onClick.AddListener(ChooseB);
        laterButton.onClick.AddListener(CloseReply);
        addTest.onClick.AddListener(AddTest);
        subtractTest.onClick.AddListener(SubtractTest);
        resetTest.onClick.AddListener(ResetTest);
        dialoguePanel.SetActive(false);
        RefreshHUD();
    }
    private void LateUpdate() { if (state != null) RefreshHUD(); }
    private void RefreshHUD()
    {
        int value = state.SteveSuspicion;
        SetLabel(meterLabel, meterLabelTMP, "Steve's suspicion: " + value + "/100");
        meterFill.fillAmount = value / 100f;
        meterFill.color = value >= 70 ? new Color32(201, 53, 63, 255) :
            value >= 40 ? new Color32(225, 159, 34, 255) : new Color32(43, 166, 119, 255);
        if (warningLabelTMP != null) warningLabelTMP.gameObject.SetActive(value >= 100);
        else if (warningLabel != null) warningLabel.gameObject.SetActive(value >= 100);
        SetLabel(warningLabel, warningLabelTMP, "Steve is suspicious! (Test crisis)");
        bool modal = dialoguePanel.activeSelf;
        replyButton.gameObject.SetActive(state.ReplyAvailable && !state.ReplyCompleted && !modal);
        // Hide test buttons in builds unless explicitly enabled in a development build.
        testControls.SetActive(showTestControls && (Application.isEditor || Debug.isDebugBuild) && !modal);
    }
    public void OpenReply()
    {
        if (!state.ReplyAvailable || state.ReplyCompleted) return;
        BlockWorldClicks();
        dialoguePanel.SetActive(true);
        DrawConversation();
        RefreshHUD();
        choiceA.Select();
    }
    private void DrawConversation()
    {
        presentedStage = state.ReplyStage;
        nextChoiceTime = Time.unscaledTime + .3f;
        SetLabel(feedbackText, feedbackTextTMP, state.LastReplyFeedback);
        bool complete = state.ReplyCompleted;
        choiceA.gameObject.SetActive(!complete);
        choiceB.gameObject.SetActive(!complete);
        SetLabel(laterText, laterTextTMP, complete ? "Close" : "Reply later");
        if (complete)
        {
            SetLabel(dialogueText, dialogueTextTMP, "Reply sent.\nSteve: All right. Let's get started.\nFinal suspicion: " + state.SteveSuspicion + "/100");
            laterButton.Select();
        }
        else if (presentedStage == 0)
        {
            SetLabel(dialogueText, dialogueTextTMP, "Steve: I saw the project email. What are you going to do first?");
            SetLabel(choiceAText, choiceATextTMP, "Uhh... what am I supposed to do?  (+10 suspicion)");
            SetLabel(choiceBText, choiceBTextTMP, "I'll check the SFML setup and open a test window.  (+0)");
        }
        else
        {
            SetLabel(dialogueText, dialogueTextTMP, "Steve: How will you know the setup actually works?");
            SetLabel(choiceAText, choiceATextTMP, "I'll build and run the test, then check the result.  (-5 suspicion)");
            SetLabel(choiceBText, choiceBTextTMP, "If it compiles, I'm sure everything else is fine.  (+15 suspicion)");
        }
    }
    private void ChooseA() { Choose(true); }
    private void ChooseB() { Choose(false); }
    private void Choose(bool first)
    {
        if (!dialoguePanel.activeSelf || Time.unscaledTime < nextChoiceTime) return;
        int expected = presentedStage;
        int delta = expected == 0 ? (first ? 10 : 0) : (first ? -5 : 15);
        string response = expected == 0 ? (first ? "Steve: You seem unsure. (+10)" : "Steve: Sounds like a sensible start. (+0)") :
            (first ? "Steve: Good. Testing it is reassuring. (-5, minimum 0)" : "Steve: That doesn't prove it works. (+15)");
        if (!state.TryApplyReply(expected, delta, response)) return;
        DrawConversation();
        RefreshHUD();
    }
    public void CloseReply()
    {
        dialoguePanel.SetActive(false);
        RestoreWorldClicks();
        RefreshHUD();
    }
    private void AddTest() { state.ChangeSteveSuspicion(10); }
    private void SubtractTest() { state.ChangeSteveSuspicion(-10); }
    private void ResetTest()
    {
        state.RestartReplyTest();
        SetLabel(testFeedback, testFeedbackTMP, "Reset. Open the email, read it, then close it to unlock Reply now again.");
        RefreshHUD();
    }
    private void BlockWorldClicks()
    {
        // OnMouseDown navigation ignores Canvas raycasts. Disable the scene's colliders
        // while this modal is open, then restore only the ones we disabled.
        foreach (Collider2D c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            if (c.enabled && c.gameObject.scene == gameObject.scene) { blocked2D.Add(c); c.enabled = false; }
        foreach (Collider c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (c.enabled && c.gameObject.scene == gameObject.scene) { blocked3D.Add(c); c.enabled = false; }
    }
    private void RestoreWorldClicks()
    {
        foreach (Collider2D c in blocked2D) if (c != null) c.enabled = true;
        foreach (Collider c in blocked3D) if (c != null) c.enabled = true;
        blocked2D.Clear(); blocked3D.Clear();
    }
    private void OnDisable() { RestoreWorldClicks(); }
    private void OnDestroy()
    {
        if (replyButton != null) replyButton.onClick.RemoveListener(OpenReply);
        if (choiceA != null) choiceA.onClick.RemoveListener(ChooseA);
        if (choiceB != null) choiceB.onClick.RemoveListener(ChooseB);
        if (laterButton != null) laterButton.onClick.RemoveListener(CloseReply);
        if (addTest != null) addTest.onClick.RemoveListener(AddTest);
        if (subtractTest != null) subtractTest.onClick.RemoveListener(SubtractTest);
        if (resetTest != null) resetTest.onClick.RemoveListener(ResetTest);
    }
}
