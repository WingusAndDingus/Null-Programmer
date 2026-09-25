using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>Email state stays on the icon while the existing window is hidden/shown.</summary>
[DisallowMultipleComponent]
public sealed class IntegratedMailInbox : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private SpriteRenderer windowBody;
    [SerializeField] private SpriteRenderer titleBar;
    [SerializeField] private DraggableWindow drag;
    [SerializeField] private Canvas contentCanvas;
    [SerializeField] private Canvas notificationCanvas;
    [SerializeField] private RectTransform unreadBadge;
    [SerializeField] private Text inboxLabel;
    [SerializeField] private Text rowLabel;
    [SerializeField] private Text subjectLabel;
    [SerializeField] private Text senderLabel;
    [SerializeField] private Text bodyLabel;
    [SerializeField] private GameObject readingPane;
    [SerializeField] private GameObject prompt;
    [SerializeField] private Button emailButton;
    [SerializeField] private ScrollRect bodyScroll;
    [SerializeField] private float worldUnitsPerPixel;

    [Header("Sample email")]
    [SerializeField] private string sender = "Project Management";
    [SerializeField] private string subject = "Minesweeper Project - Getting Started";
    [SerializeField, TextArea(8, 20)] private string body =
        "Hi,\n\nYou're leading our new Minesweeper project, built in C++ using SFML 3.0. We'll review the finished application at the end of the next two weeks.\n\nFor today, get the development environment ready and confirm that you can open an SFML window. We'll send additional requirements as the project progresses.\n\nSteve will be working alongside you. He's eager to learn how you approach a project like this, so please keep him involved.\n\nWith your experience, I'm sure we're in good hands.\n\nBest,\nProject Management";

    public bool IsRead { get; private set; }
    public DraggableWindow Drag => drag;
    public Canvas ContentCanvas => contentCanvas;
    public SpriteRenderer WindowBody => windowBody;

    private void Awake()
    {
        if (window == null || windowBody == null || titleBar == null || drag == null ||
            contentCanvas == null || notificationCanvas == null || unreadBadge == null ||
            inboxLabel == null || rowLabel == null || subjectLabel == null ||
            senderLabel == null || bodyLabel == null || readingPane == null ||
            prompt == null || emailButton == null || bodyScroll == null)
        {
            Debug.LogError("Integrated Mail references are incomplete. Restore the missing objects or undo the installation.", this);
            enabled = false;
            return;
        }
        IsRead = false;
        emailButton.onClick.AddListener(ReadEmail);
        Refresh();
    }

    private void Start()
    {
        if (enabled) window.SetActive(false);
    }

    public void OpenInbox()
    {
        if (!enabled || window == null) return;
        readingPane.SetActive(false);
        prompt.SetActive(true);
        window.SetActive(true);
        drag.BringToFront();
        Refresh();
        FitContents();
    }

    public void ReadEmail()
    {
        if (!enabled) return;
        IsRead = true;
        prompt.SetActive(false);
        readingPane.SetActive(true);
        drag.BringToFront();
        Refresh();
        Canvas.ForceUpdateCanvases();
        bodyScroll.verticalNormalizedPosition = 1f;
    }

    private void Refresh()
    {
        unreadBadge.gameObject.SetActive(!IsRead);
        inboxLabel.text = IsRead ? "Inbox" : "Inbox (1 unread)";
        rowLabel.text = (IsRead ? "READ" : "NEW") + "\n\n" + sender + "\n\n" + subject;
        rowLabel.fontStyle = IsRead ? FontStyle.Normal : FontStyle.Bold;
        subjectLabel.text = subject;
        senderLabel.text = "From: " + sender;
        bodyLabel.text = body;
    }

    private void LateUpdate()
    {
        if (window != null && window.activeInHierarchy) FitContents();
        PositionBadge();
    }

    // Called by the installer too, so the UI is visible and editable before Play.
    public void FitContents()
    {
        if (contentCanvas == null || windowBody == null || titleBar == null) return;
        Bounds b = windowBody.bounds;
        float padding = Mathf.Min(b.size.x, b.size.y) * .025f;
        float left = b.min.x + padding;
        float right = b.max.x - padding;
        float bottom = b.min.y + padding;
        float top = Mathf.Min(b.max.y, titleBar.bounds.min.y) - padding;
        if (top <= bottom || right <= left) return;
        if (worldUnitsPerPixel <= 0f) worldUnitsPerPixel = (right - left) / 900f;
        RectTransform rect = (RectTransform)contentCanvas.transform;
        Vector3 parentScale = rect.parent.lossyScale;
        rect.localScale = new Vector3(worldUnitsPerPixel / parentScale.x,
            worldUnitsPerPixel / parentScale.y, 1f / parentScale.z);
        rect.rotation = Quaternion.identity;
        rect.position = new Vector3((left + right) * .5f, (bottom + top) * .5f, b.center.z - .02f);
        rect.sizeDelta = new Vector2((right - left) / worldUnitsPerPixel, (top - bottom) / worldUnitsPerPixel);
        contentCanvas.worldCamera = Camera.main;
        SortingGroup group = window.GetComponent<SortingGroup>();
        if (group != null)
        {
            contentCanvas.sortingLayerID = group.sortingLayerID;
            contentCanvas.sortingOrder = group.sortingOrder + 1;
        }
    }

    public void PositionBadge()
    {
        if (unreadBadge == null || notificationCanvas == null || Camera.main == null) return;
        var sprite = GetComponent<SpriteRenderer>();
        Vector3 point = sprite != null ? sprite.bounds.max : transform.position;
        Vector3 screen = Camera.main.WorldToScreenPoint(point);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)notificationCanvas.transform, screen, null, out Vector2 local))
            unreadBadge.anchoredPosition = local;
    }

    private void OnDestroy()
    {
        if (emailButton != null) emailButton.onClick.RemoveListener(ReadEmail);
    }

#if UNITY_EDITOR
    // Single setup entry point used by the editor installer, not during gameplay.
    public void Configure(GameObject windowObject, SpriteRenderer windowSprite, SpriteRenderer bar,
        DraggableWindow draggable, Canvas canvas, Canvas badgeCanvas, RectTransform badge,
        Text heading, Text row, Text subjectText, Text fromText, Text messageText,
        GameObject pane, GameObject emptyPrompt, Button button, ScrollRect scroll)
    {
        window = windowObject; windowBody = windowSprite; titleBar = bar; drag = draggable;
        contentCanvas = canvas; notificationCanvas = badgeCanvas; unreadBadge = badge;
        inboxLabel = heading; rowLabel = row; subjectLabel = subjectText; senderLabel = fromText;
        bodyLabel = messageText; readingPane = pane; prompt = emptyPrompt;
        emailButton = button; bodyScroll = scroll;
        Refresh(); FitContents(); PositionBadge();
    }
#endif
}
