using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Visual layer for the monitor messaging app. Conversation state stays in MessagingSession.</summary>
public sealed class MessagingWindowView : MonoBehaviour
{
    private static readonly Color Screen = new Color(.045f, .061f, .092f);
    private static readonly Color Sidebar = new Color(.060f, .078f, .116f);
    private static readonly Color Surface = new Color(.090f, .115f, .166f);
    private static readonly Color SurfaceSelected = new Color(.14f, .19f, .34f);
    private static readonly Color Accent = new Color(.39f, .48f, 1f);
    private static readonly Color Muted = new Color(.62f, .69f, .81f);
    private static readonly Color Incoming = new Color(.115f, .145f, .205f);
    private static readonly Color Outgoing = new Color(.20f, .25f, .57f);

    public Canvas canvas;
    public SpriteRenderer body, titleBar;
    public TMP_Text heading;
    public Image selectedAvatar;
    public TMP_Text statusText;
    public Transform contacts, history, replies;
    public ScrollRect scroll;
    public DraggableWindow drag;
    public TMP_FontAsset messageFont;
    public float messageFontSize = 36;
    private float unitsPerPixel;

    public void FitContents()
    {
        if (canvas == null || body == null || titleBar == null) return;
        Bounds b = body.bounds;
        float padding = Mathf.Min(b.size.x, b.size.y) * .025f;
        float left = b.min.x + padding, right = b.max.x - padding, bottom = b.min.y + padding;
        float top = Mathf.Min(b.max.y, titleBar.bounds.min.y) - padding;
        if (top <= bottom || right <= left) return;
        if (unitsPerPixel <= 0) unitsPerPixel = (right - left) / 900f;
        var rect = (RectTransform)canvas.transform;
        Vector3 scale = rect.parent.lossyScale;
        rect.localScale = new Vector3(unitsPerPixel / scale.x, unitsPerPixel / scale.y, 1f / scale.z);
        rect.rotation = Quaternion.identity;
        rect.position = new Vector3((left + right) / 2, (bottom + top) / 2, b.center.z - .02f);
        rect.sizeDelta = new Vector2((right - left) / unitsPerPixel, (top - bottom) / unitsPerPixel);
        canvas.worldCamera = Camera.main;
        var group = GetComponent<SortingGroup>();
        if (group != null) { canvas.sortingLayerID = group.sortingLayerID; canvas.sortingOrder = group.sortingOrder + 1; }
    }

    private void LateUpdate() => FitContents();

    /// <summary>Rebuilds only the prefab's UI children, keeping its world-space Canvas and window identity.</summary>
    public void RebuildVisuals()
    {
        if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            var root = new GameObject("Messages Content", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
        }
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = canvas.transform.GetChild(i).gameObject;
            child.SetActive(false);
            DestroyImmediate(child);
        }

        var rootRect = (RectTransform)canvas.transform;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, .5f);
        rootRect.pivot = new Vector2(.5f, .5f);
        var back = Rect("Background", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        back.gameObject.AddComponent<Image>().color = Screen;

        // The left rail is intentionally narrow so the active conversation gets most of the monitor.
        var rail = Rect("Conversation Rail", rootRect, Vector2.zero, new Vector2(.29f, 1), Vector2.zero, Vector2.zero);
        rail.gameObject.AddComponent<Image>().color = Sidebar;
        var railTitle = Rect("Inbox Title", rail, new Vector2(0, .87f), Vector2.one, new Vector2(22, 0), new Vector2(-18, -18));
        Label("Inbox Label", railTitle, "MESSAGES", SmallTitleSize(), TextAlignmentOptions.MidlineLeft, Color.white);

        var contactScrollRoot = Rect("Contacts Scroll", rail, Vector2.zero, new Vector2(1, .86f), new Vector2(10, 10), new Vector2(-10, -6));
        var contactScroll = contactScrollRoot.gameObject.AddComponent<ScrollRect>();
        contactScroll.horizontal = false;
        var contactViewport = Rect("Contacts Viewport", contactScrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        contactViewport.gameObject.AddComponent<Image>().color = Sidebar;
        contactViewport.gameObject.AddComponent<RectMask2D>();
        var contactContent = Rect("Contacts", contactViewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        contactContent.pivot = new Vector2(.5f, 1);
        Layout(contactContent, 8, 6);
        contactContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contactScroll.viewport = contactViewport;
        contactScroll.content = contactContent;
        contacts = contactContent;

        var chat = Rect("Active Conversation", rootRect, new Vector2(.29f, 0), Vector2.one, Vector2.zero, Vector2.zero);
        var chatHeader = Rect("Contact Header", chat, new Vector2(0, .865f), Vector2.one, new Vector2(22, 0), new Vector2(-18, -12));
        var avatarRect = Rect("Selected Contact Portrait", chatHeader, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -29), new Vector2(58, 29));
        selectedAvatar = avatarRect.gameObject.AddComponent<Image>();
        selectedAvatar.preserveAspect = true;
        selectedAvatar.raycastTarget = false;
        var nameRect = Rect("Contact Name", chatHeader, new Vector2(0, .50f), new Vector2(1, 1), new Vector2(72, -1), new Vector2(-8, -2));
        heading = Label("Contact Name Label", nameRect, "Choose a conversation", MainTitleSize(), TextAlignmentOptions.BottomLeft, Color.white);
        var statusRect = Rect("Contact Status", chatHeader, new Vector2(0, 0), new Vector2(1, .5f), new Vector2(72, 1), new Vector2(-8, 0));
        statusText = Label("Contact Status Label", statusRect, "Select someone to view messages", BodySize() * .78f, TextAlignmentOptions.TopLeft, Muted);
        var headerRule = Rect("Header Divider", chat, new Vector2(0, .855f), new Vector2(1, .855f), Vector2.zero, new Vector2(0, 2));
        headerRule.gameObject.AddComponent<Image>().color = new Color(.20f, .25f, .36f);

        var replyArea = Rect("Reply Area", chat, Vector2.zero, new Vector2(1, .20f), new Vector2(18, 14), new Vector2(-18, -2));
        var replyBackground = replyArea.gameObject.AddComponent<Image>();
        replyBackground.color = new Color(.055f, .073f, .108f);
        var replyScroll = replyArea.gameObject.AddComponent<ScrollRect>();
        replyScroll.horizontal = false;
        var replyViewport = Rect("Reply Viewport", replyArea, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8));
        replyViewport.gameObject.AddComponent<RectMask2D>();
        var replyContent = Rect("Reply Choices", replyViewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        replyContent.pivot = new Vector2(.5f, 1);
        Layout(replyContent, 7, 2);
        replyContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        replyScroll.viewport = replyViewport;
        replyScroll.content = replyContent;
        replies = replyContent;

        var historyRoot = Rect("History Scroll", chat, new Vector2(0, .205f), new Vector2(1, .85f), new Vector2(12, 4), new Vector2(-12, -5));
        scroll = historyRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        var historyViewport = Rect("History Viewport", historyRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        historyViewport.gameObject.AddComponent<RectMask2D>();
        var historyContent = Rect("History", historyViewport, new Vector2(0, 1), Vector2.one, new Vector2(0, -6), Vector2.zero);
        historyContent.pivot = new Vector2(.5f, 1);
        Layout(historyContent, 12, 8);
        historyContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = historyViewport;
        scroll.content = historyContent;
        history = historyContent;
    }

    public static void Clear(Transform root)
    {
        if (root == null) return;
        foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }

    public void AddContact(MessageConversation conversation, string preview, int unread, bool isSelected, UnityEngine.Events.UnityAction action)
    {
        var row = New("Contact", contacts);
        var rowImage = row.AddComponent<Image>();
        rowImage.color = isSelected ? SurfaceSelected : Color.clear;
        var button = row.AddComponent<Button>();
        button.targetGraphic = rowImage;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = ButtonColors(rowImage.color);
        button.onClick.AddListener(action);
        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 82;
        var horizontal = row.AddComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(10, 10, 8, 8);
        horizontal.spacing = 12;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;
        AddAvatar(row.transform, conversation != null ? conversation.avatar : null, conversation != null ? conversation.contactName : "?", 54);

        var details = New("Contact Details", row.transform);
        var detailLayout = details.AddComponent<VerticalLayoutGroup>();
        detailLayout.spacing = 2;
        detailLayout.childControlWidth = detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;
        var detailsSize = details.AddComponent<LayoutElement>();
        detailsSize.flexibleWidth = 1;
        Label("Contact", details.transform, conversation.contactName, BodySize(), TextAlignmentOptions.MidlineLeft, Color.white);
        Label("Preview", details.transform, Trim(preview, 31), BodySize() * .68f, TextAlignmentOptions.MidlineLeft, Muted);
        if (unread > 0)
        {
            var dot = New("Unread", row.transform);
            var dotImage = dot.AddComponent<Image>();
            dotImage.color = new Color(.40f, .61f, 1f);
            var dotSize = dot.AddComponent<LayoutElement>();
            dotSize.preferredWidth = dotSize.preferredHeight = 10;
        }
    }

    public void SetActiveContact(MessageConversation conversation)
    {
        if (conversation == null)
        {
            if (heading != null) heading.text = "Messages";
            if (statusText != null) statusText.text = "Select a conversation";
            if (selectedAvatar != null)
            {
                selectedAvatar.sprite = null;
                selectedAvatar.color = new Color(.16f, .21f, .31f);
            }
            return;
        }
        if (heading != null) heading.text = conversation.contactName;
        if (statusText != null) statusText.text = "ACTIVE CONVERSATION";
        if (selectedAvatar != null)
        {
            selectedAvatar.sprite = conversation.avatar;
            selectedAvatar.color = conversation.avatar != null ? Color.white : new Color(.16f, .21f, .31f);
            selectedAvatar.preserveAspect = true;
        }
    }

    public void AddMessage(MessagingSession.Entry entry, string contactName, Sprite avatar)
    {
        var row = New("Message Row", history);
        var rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 88;
        var line = row.AddComponent<HorizontalLayoutGroup>();
        line.spacing = 10;
        line.childAlignment = entry.fromPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        line.childControlWidth = false;
        line.childControlHeight = false;
        line.childForceExpandWidth = false;
        line.childForceExpandHeight = false;

        if (entry.fromPlayer) AddSpacer(row.transform);
        else AddAvatar(row.transform, avatar, contactName, 38);

        var bubble = New("Message Bubble", row.transform);
        var bubbleImage = bubble.AddComponent<Image>();
        bubbleImage.color = entry.fromPlayer ? Outgoing : Incoming;
        var bubbleSize = bubble.AddComponent<LayoutElement>();
        bubbleSize.preferredWidth = 495;
        bubbleSize.minWidth = 140;
        bubbleSize.flexibleWidth = 0;
        var bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.padding = new RectOffset(16, 16, 9, 8);
        bubbleLayout.spacing = 3;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childControlHeight = false;
        bubbleLayout.childForceExpandWidth = true;
        bubbleLayout.childForceExpandHeight = false;
        string sender = entry.fromPlayer ? "YOU" : contactName.ToUpperInvariant();
        Label("Sender", bubble.transform, sender, BodySize() * .57f, TextAlignmentOptions.MidlineLeft,
            entry.fromPlayer ? new Color(.79f, .83f, 1f) : new Color(.70f, .78f, .94f));
        var message = Label("Message Text", bubble.transform, entry.text, BodySize() * .78f, TextAlignmentOptions.TopLeft, Color.white);
        message.textWrappingMode = TextWrappingModes.Normal;
        message.overflowMode = TextOverflowModes.Overflow;
        float messageWidth = 460;
        float textHeight = Mathf.Max(BodySize(), message.GetPreferredValues(entry.text, messageWidth, 0).y);
        message.gameObject.GetComponent<LayoutElement>().preferredHeight = textHeight;
        rowLayout.preferredHeight = Mathf.Max(88, textHeight + BodySize() * 2.3f + 28);
        Label("Time", bubble.transform, string.IsNullOrEmpty(entry.timeLabel) ? "" : entry.timeLabel,
            BodySize() * .55f, TextAlignmentOptions.MidlineRight, Muted);
        if (!entry.fromPlayer) AddSpacer(row.transform);
    }

    public Button ReplyChoice(string text, UnityEngine.Events.UnityAction action)
    {
        var go = New("Reply Choice", replies);
        var bg = go.AddComponent<Image>();
        bg.color = Surface;
        var button = go.AddComponent<Button>();
        button.targetGraphic = bg;
        button.colors = ButtonColors(Surface);
        button.onClick.AddListener(action);
        var sizing = go.AddComponent<LayoutElement>();
        sizing.preferredHeight = Mathf.Max(52, BodySize() * 1.8f);
        var label = Label("Reply Text", go.transform, text, BodySize() * .74f, TextAlignmentOptions.MidlineLeft, Color.white);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(14, 5);
        label.rectTransform.offsetMax = new Vector2(-14, -5);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
        return button;
    }

    public TMP_Text Label(Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        return Label("Text", parent, text, fontSize, alignment, color);
    }

    public TMP_Text Label(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = New(objectName, parent);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = messageFont != null ? messageFont : TMP_Settings.defaultFontAsset;
        if (label.font != null) label.fontSharedMaterial = label.font.material;
        label.text = text;
        label.fontSize = Mathf.Max(10, fontSize);
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        var fit = go.AddComponent<LayoutElement>();
        fit.minHeight = label.fontSize * 1.15f;
        fit.preferredHeight = label.fontSize * 1.4f;
        return label;
    }

    private void AddAvatar(Transform parent, Sprite sprite, string contactName, float size)
    {
        var root = New("Portrait", parent);
        var fit = root.AddComponent<LayoutElement>();
        fit.preferredWidth = size;
        fit.preferredHeight = size;
        fit.minWidth = size;
        var image = root.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : new Color(.16f, .21f, .31f);
        image.raycastTarget = false;
        if (sprite == null)
        {
            var initial = New("Initial", root.transform).AddComponent<TextMeshProUGUI>();
            initial.font = messageFont != null ? messageFont : TMP_Settings.defaultFontAsset;
            if (initial.font != null) initial.fontSharedMaterial = initial.font.material;
            initial.text = string.IsNullOrEmpty(contactName) ? "?" : contactName.Substring(0, 1).ToUpperInvariant();
            initial.fontSize = BodySize() * .8f;
            initial.alignment = TextAlignmentOptions.Center;
            initial.color = Color.white;
            initial.raycastTarget = false;
            initial.rectTransform.anchorMin = Vector2.zero;
            initial.rectTransform.anchorMax = Vector2.one;
            initial.rectTransform.offsetMin = initial.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void AddSpacer(Transform parent)
    {
        var spacer = New("Flexible Space", parent);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    private float BodySize() => Mathf.Clamp(messageFontSize * .68f, 18f, 29f);
    private float MainTitleSize() => Mathf.Clamp(messageFontSize * .82f, 23f, 34f);
    private float SmallTitleSize() => Mathf.Clamp(messageFontSize * .60f, 19f, 27f);
    private static string Trim(string value, int length) => string.IsNullOrEmpty(value) ? "" : value.Length <= length ? value : value.Substring(0, length - 1) + "…";

    private static ColorBlock ButtonColors(Color normal)
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = normal;
        colors.highlightedColor = new Color(Mathf.Min(1, normal.r + .08f), Mathf.Min(1, normal.g + .08f), Mathf.Min(1, normal.b + .12f), 1);
        colors.pressedColor = Accent;
        colors.selectedColor = SurfaceSelected;
        colors.fadeDuration = .08f;
        return colors;
    }

    private static void Layout(RectTransform rect, int spacing, int inset)
    {
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(inset, inset, inset, inset);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static GameObject New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
    {
        var go = New(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = low; rect.offsetMax = high;
        return rect;
    }
}
