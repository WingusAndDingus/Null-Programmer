using UnityEngine;
using UnityEngine.UI;

/// <summary>One-email prototype. State lives on the desktop icon, not the window.</summary>
public class MailInbox : MonoBehaviour
{
    [SerializeField] private string sender = "Project Management";
    [SerializeField] private string subject = "Minesweeper Project - Getting Started";
    [SerializeField, TextArea(8, 20)] private string body =
        "Hi,\n\nYou're leading our new Minesweeper project, built in C++ using SFML 3.0. We'll review the finished application at the end of the next two weeks.\n\nFor today, get the development environment ready and confirm that you can open an SFML window. We'll send additional requirements as the project progresses.\n\nSteve will be working alongside you. He's eager to learn how you approach a project like this, so please keep him involved.\n\nWith your experience, I'm sure we're in good hands.\n\nBest,\nProject Management";

    public bool IsRead { get; private set; }
    [SerializeField, HideInInspector] private Canvas canvas;
    [SerializeField, HideInInspector] private GameObject window;
    [SerializeField, HideInInspector] private GameObject readingPane;
    [SerializeField, HideInInspector] private GameObject placeholder;
    [SerializeField, HideInInspector] private RectTransform badge;
    [SerializeField, HideInInspector] private Text inboxHeading;
    [SerializeField, HideInInspector] private Text messageLabel;
    [SerializeField, HideInInspector] private Button closeButton;
    [SerializeField, HideInInspector] private Button emailButton;
    [SerializeField, HideInInspector] private Text subjectLabel;
    [SerializeField, HideInInspector] private Text senderLabel;
    [SerializeField, HideInInspector] private Text bodyLabel;
    private bool createdAtRuntime;
    private Font font;

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (canvas == null)
        {
            BuildInterface();
            createdAtRuntime = true;
        }
        if (closeButton == null || emailButton == null || window == null ||
            readingPane == null || placeholder == null || badge == null ||
            inboxHeading == null || messageLabel == null || subjectLabel == null ||
            senderLabel == null || bodyLabel == null)
        {
            Debug.LogError("Mail UI references are incomplete. Undo deleted UI objects or restore the scene.", this);
            enabled = false;
            return;
        }
        // Runtime listeners are rebound after loading the saved scene.
        closeButton.onClick.AddListener(CloseInbox);
        emailButton.onClick.AddListener(ReadEmail);
        subjectLabel.text = subject;
        senderLabel.text = "From: " + sender;
        bodyLabel.text = body;
        RefreshReadState();
        window.SetActive(false);
    }

    public void OpenInbox()
    {
        // Opens the inbox, but won't mark it as read until the actual message is clicked.
        readingPane.SetActive(false);
        placeholder.SetActive(true);
        window.SetActive(true);
        RefreshReadState();
    }

    public void CloseInbox() { window.SetActive(false); }

    public void ReadEmail()
    {
        IsRead = true;
        placeholder.SetActive(false);
        readingPane.SetActive(true);
        RefreshReadState();
    }

    private void RefreshReadState()
    {
        badge.gameObject.SetActive(!IsRead);
        inboxHeading.text = IsRead ? "Inbox" : "Inbox (1 unread)";
        messageLabel.text = (IsRead ? "READ" : "NEW") + "\n\n" + sender + "\n\n" + subject;
        messageLabel.fontStyle = IsRead ? FontStyle.Normal : FontStyle.Bold;
    }

    private void LateUpdate()
    {
        if (IsRead || Camera.main == null) return;
        // There is the odverlay badge follows the existing sprite-based mail icon.
        var renderer = GetComponent<SpriteRenderer>();
        Vector3 position = renderer != null ? renderer.bounds.max : transform.position;
        Vector3 screen = Camera.main.WorldToScreenPoint(position);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, screen, null, out local);
        badge.anchoredPosition = local;
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseInbox);
        if (emailButton != null) emailButton.onClick.RemoveListener(ReadEmail);
        if (createdAtRuntime && canvas != null) Destroy(canvas.gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("Create Mail UI in Scene")]
    private void CreateMailUIInScene()
    {
        if (Application.isPlaying) return;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        if (canvas != null)
        {
            UnityEditor.Selection.activeGameObject = canvas.gameObject;
            Debug.Log("Mail UI already exists. Edit its children in the Hierarchy.", this);
            return;
        }
        UnityEditor.Undo.IncrementCurrentGroup();
        int group = UnityEditor.Undo.GetCurrentGroup();
        UnityEditor.Undo.SetCurrentGroupName("Create Mail UI");
        UnityEditor.Undo.RecordObject(this, "Save Mail UI references");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildInterface();
        RefreshReadState();
        // You can preview the message in Edit Mode. Awake hides the window at game start.
        window.SetActive(true);
        readingPane.SetActive(true);
        placeholder.SetActive(false);
        LateUpdate();
        UnityEditor.Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create Mail UI");
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.Undo.CollapseUndoOperations(group);
        UnityEditor.Selection.activeGameObject = canvas.gameObject;
    }
#endif

    private void BuildInterface()
    {
        var root = new GameObject("Mail UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        // Keep the UI in the same scene as its owner, including additive loads.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        badge = Panel("Unread badge", root.transform, new Color32(194, 49, 66, 255));
        badge.anchorMin = badge.anchorMax = new Vector2(.5f, .5f);
        badge.sizeDelta = new Vector2(26, 26);
        badge.GetComponent<Image>().raycastTarget = false;
        Label("Count", badge, "1", 17, Color.white, TextAnchor.MiddleCenter);

        // Modal backdrop prevents clicks passing into the desktop while mail is open.
        var backdrop = Panel("Mail window", root.transform, new Color(0, 0, 0, .45f));
        window = backdrop.gameObject;
        var frame = Panel("Inbox frame", backdrop, new Color32(236, 241, 245, 255));
        frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
        frame.sizeDelta = new Vector2(1000, 600);
        var title = Panel("Title bar", frame, new Color32(26, 54, 76, 255));
        Place(title, new Vector2(0, 1), Vector2.one, new Vector2(0, -54), Vector2.zero);
        var heading = Label("Title", title, "MAIL / Null Programmer", 22, Color.white);
        heading.rectTransform.offsetMin = new Vector2(20, 0);
        heading.rectTransform.offsetMax = new Vector2(-85, 0);
        var close = Button("Close", title, "X");
        closeButton = close.GetComponent<Button>();
        Place(close, new Vector2(1, 0), Vector2.one, new Vector2(-64, 7), new Vector2(-8, -7));

        var list = Panel("Message list", frame, new Color32(216, 226, 233, 255));
        Place(list, Vector2.zero, new Vector2(0, 1), new Vector2(16, 16), new Vector2(302, -70));
        inboxHeading = Label("Inbox heading", list, "", 22, new Color32(26, 54, 76, 255));
        Place(inboxHeading.rectTransform, new Vector2(0, 1), Vector2.one,
            new Vector2(14, -46), new Vector2(-10, -4));
        var row = Button("Sample email", list, "");
        emailButton = row.GetComponent<Button>();
        Place(row, new Vector2(0, 1), Vector2.one, new Vector2(10, -245), new Vector2(-10, -58));
        messageLabel = row.GetComponentInChildren<Text>();
        messageLabel.alignment = TextAnchor.UpperLeft;
        messageLabel.fontSize = 18;
        messageLabel.rectTransform.offsetMin = new Vector2(14, 14);
        messageLabel.rectTransform.offsetMax = new Vector2(-14, -14);

        var content = Panel("Message area", frame, Color.white);
        Place(content, Vector2.zero, Vector2.one, new Vector2(318, 16), new Vector2(-16, -70));
        placeholder = Label("Select message", content, "Select an email to read it.", 22,
            new Color32(80, 96, 110, 255), TextAnchor.MiddleCenter).gameObject;
        var pane = new GameObject("Reading pane", typeof(RectTransform)).GetComponent<RectTransform>();
        pane.SetParent(content, false);
        Place(pane, Vector2.zero, Vector2.one, new Vector2(22, 16), new Vector2(-22, -16));
        readingPane = pane.gameObject;
        var emailTitle = Label("Subject", pane, subject, 22, new Color32(26, 54, 76, 255));
        subjectLabel = emailTitle;
        emailTitle.fontStyle = FontStyle.Bold;
        Place(emailTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -58), Vector2.zero);
        var from = Label("Sender", pane, "From: " + sender, 17, new Color32(80, 96, 110, 255));
        senderLabel = from;
        Place(from.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -91), new Vector2(0, -58));

        var scroll = Panel("Body viewport", pane, Color.white);
        Place(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -106));
        scroll.gameObject.AddComponent<RectMask2D>();
        var bodyText = Label("Body", scroll, body, 18, new Color32(35, 48, 60, 255), TextAnchor.UpperLeft);
        bodyLabel = bodyText;
        bodyText.rectTransform.anchorMin = new Vector2(0, 1);
        bodyText.rectTransform.anchorMax = Vector2.one;
        bodyText.rectTransform.pivot = new Vector2(.5f, 1);
        bodyText.rectTransform.offsetMin = Vector2.zero;
        bodyText.rectTransform.offsetMax = new Vector2(-12, 0);
        bodyText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = scroll;
        scrollRect.content = bodyText.rectTransform;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25;
        readingPane.SetActive(false);
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = low; rect.offsetMax = high;
    }

    private RectTransform Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(string name, Transform parent, string value, int size, Color color,
        TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.font = font; text.fontSize = size; text.text = value;
        text.color = color; text.alignment = alignment; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private RectTransform Button(string name, Transform parent, string caption)
    {
        var rect = Panel(name, parent, Color.white);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color32(202, 227, 238, 255);
        colors.pressedColor = new Color32(164, 202, 219, 255);
        button.colors = colors;
        // Listeners are attached in the Awake so editor-created UI survives scene reloads.
        Label("Label", rect, caption, 20, new Color32(26, 54, 76, 255), TextAnchor.MiddleCenter);
        return rect;
    }
}