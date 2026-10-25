using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MonitorMessagingApp : MonoBehaviour
{
    public MessageConversation[] conversations;
    public RectTransform contacts, history, replies;
    public TMP_Text heading;
    public ScrollRect historyScroll;

    [Header("Physical Monitor Bounds")]
    [Tooltip("The SpriteRenderer on Square containing the right monitor artwork.")]
    public SpriteRenderer monitorArtwork;
    [Tooltip("Inner display rectangle in source-sprite pixels, measured from the bottom-left. Defaults match right monitor_0.")]
    public Rect displayPixels = new Rect(550, 378, 572, 306);

    [Header("Messaging Appearance")]
    public Sprite backgroundArtwork;
    [Range(700, 1200)] public float referenceWidth = 900;
    [Range(18, 36)] public float chatFontSize = 26;
    [Range(0.45f, 0.85f)] public float maximumBubbleWidth = 0.76f;
    public Color incomingBubbleColor = new Color(0.10f, 0.17f, 0.25f, 0.97f);
    public Color outgoingBubbleColor = new Color(0.16f, 0.30f, 0.50f, 0.98f);

    private MessagingWindowView view;
    private MessagingSession session;
    private MessageConversation selected;
    private bool refreshing;
    private int day = -1;
    private Sprite bubbleSprite;
    private Texture2D bubbleTexture;

    private void Start()
    {
        if (!BuildMonitorVisuals()) return;
        session = MessagingSession.Get();
        session.Changed += Refresh;
        Unlock();
    }

    private bool BuildMonitorVisuals()
    {
        var screen = transform.Find("Messages Screen") as RectTransform;
        if (screen == null)
        {
            Debug.LogError("MonitorMessagingApp could not find its Messages Screen child.", this);
            return false;
        }

        if (!FitMonitorCanvas()) return false;
        Canvas.ForceUpdateCanvases();
        if (screen.GetComponent<RectMask2D>() == null)
            screen.gameObject.AddComponent<RectMask2D>();

        // Only the presentation changes; the authored scene remains available outside Play mode.
        foreach (Transform child in screen) child.gameObject.SetActive(false);

        var visualRoot = new GameObject("Updated Messages UI", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(MessagingWindowView));
        visualRoot.transform.SetParent(screen, false);
        var canvas = visualRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = false;
        var monitorCanvas = GetComponentInParent<Canvas>();
        if (monitorCanvas != null) canvas.worldCamera = monitorCanvas.worldCamera;

        view = visualRoot.GetComponent<MessagingWindowView>();
        view.canvas = canvas;
        view.messageFont = TMP_Settings.defaultFontAsset;
        view.messageFontSize = 36;
        view.body = null;
        view.titleBar = null;
        view.drag = null;
        view.RebuildVisuals();

        // Use the full monitor, with a smaller logical canvas for readable text.
        Canvas.ForceUpdateCanvases();
        var rect = visualRoot.GetComponent<RectTransform>();
        float width = Mathf.Max(1, screen.rect.width);
        float height = Mathf.Max(1, screen.rect.height);
        float scale = width / Mathf.Max(700, referenceWidth);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width / scale, height / scale);
        rect.localScale = Vector3.one * scale;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;

        contacts = view.contacts as RectTransform;
        history = view.history as RectTransform;
        replies = view.replies as RectTransform;
        heading = view.heading;
        historyScroll = view.scroll;
        ConfigureConversationLayout();
        ApplyBackground(screen);
        return true;
    }

    private bool FitMonitorCanvas()
    {
        if (monitorArtwork == null)
        {
            monitorArtwork = gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .FirstOrDefault(renderer => renderer.sprite != null &&
                    renderer.sprite.name == "right monitor_0");
        }
        if (monitorArtwork == null || monitorArtwork.sprite == null)
        {
            Debug.LogError("Assign Square's SpriteRenderer to Monitor Artwork on Monitor Messaging.", this);
            return false;
        }
        var host = transform as RectTransform;
        var hostCanvas = GetComponent<Canvas>();
        if (host == null || hostCanvas == null || hostCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("Monitor Messaging needs its existing world-space Canvas and RectTransform.", this);
            return false;
        }
        var sprite = monitorArtwork.sprite;
        if (displayPixels.width <= 0 || displayPixels.height <= 0 ||
            displayPixels.xMin < 0 || displayPixels.yMin < 0 ||
            displayPixels.xMax > sprite.rect.width || displayPixels.yMax > sprite.rect.height)
        {
            Debug.LogError("Display Pixels must be a rectangle inside the monitor sprite.", this);
            return false;
        }

        // Sprite.pivot is authoritative, including Unity's sprite alignment/import settings.
        Vector2 centerPixels = displayPixels.center - sprite.pivot;
        Vector3 localCenter = new Vector3(centerPixels.x / sprite.pixelsPerUnit,
            centerPixels.y / sprite.pixelsPerUnit, 0);
        if (monitorArtwork.flipX) localCenter.x = -localCenter.x;
        if (monitorArtwork.flipY) localCenter.y = -localCenter.y;
        Vector3 centerWorld = monitorArtwork.transform.TransformPoint(localCenter);
        Vector3 artworkScale = monitorArtwork.transform.lossyScale;
        float worldWidth = displayPixels.width / sprite.pixelsPerUnit * Mathf.Abs(artworkScale.x);
        float worldHeight = displayPixels.height / sprite.pixelsPerUnit * Mathf.Abs(artworkScale.y);
        float logicalWidth = Mathf.Max(700, referenceWidth);
        float worldUnitsPerPixel = worldWidth / logicalWidth;
        Vector3 parentScale = host.parent != null ? host.parent.lossyScale : Vector3.one;

        host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
        host.pivot = new Vector2(0.5f, 0.5f);
        host.sizeDelta = new Vector2(logicalWidth, worldHeight / worldUnitsPerPixel);
        host.rotation = monitorArtwork.transform.rotation;
        host.localScale = new Vector3(worldUnitsPerPixel / Mathf.Abs(parentScale.x),
            worldUnitsPerPixel / Mathf.Abs(parentScale.y), 1f / Mathf.Abs(parentScale.z));
        // Keep the existing foreground depth and Canvas sorting settings.
        host.position = new Vector3(centerWorld.x, centerWorld.y, host.position.z);
        return true;
    }

    private void ConfigureConversationLayout()
    {
        var historyLayout = history.GetComponent<VerticalLayoutGroup>();
        historyLayout.childControlWidth = historyLayout.childControlHeight = true;
        historyLayout.childForceExpandWidth = true;
        historyLayout.childForceExpandHeight = false;
        historyLayout.spacing = 14;
        historyLayout.padding = new RectOffset(12, 12, 14, 14);

        var replyArea = view.canvas.transform.Find("Active Conversation/Reply Area") as RectTransform;
        if (replyArea != null)
        {
            replyArea.anchorMin = Vector2.zero;
            replyArea.anchorMax = new Vector2(1, 0.30f);
            var replyScroll = replyArea.GetComponent<ScrollRect>();
            if (replyScroll != null) replyScroll.enabled = false;
        }

        var historyArea = (RectTransform)historyScroll.transform;
        historyArea.anchorMin = new Vector2(0, 0.315f);
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.scrollSensitivity = 32;

        var viewport = (RectTransform)replies.parent;
        var mask = viewport.GetComponent<RectMask2D>();
        if (mask != null) mask.enabled = false;
        var fitter = replies.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        replies.anchorMin = Vector2.zero;
        replies.anchorMax = Vector2.one;
        replies.pivot = new Vector2(0.5f, 1);
        replies.offsetMin = replies.offsetMax = Vector2.zero;
        var replyLayout = replies.GetComponent<VerticalLayoutGroup>();
        replyLayout.childControlWidth = replyLayout.childControlHeight = true;
        replyLayout.childForceExpandWidth = true;
        replyLayout.childForceExpandHeight = false;
        replyLayout.childAlignment = TextAnchor.UpperLeft;
        replyLayout.spacing = 12;
    }

    private void ApplyBackground(RectTransform screen)
    {
        // Wallpaper fills the original monitor area.
        var wallpaper = screen.GetComponent<Image>();
        if (wallpaper == null) wallpaper = screen.gameObject.AddComponent<Image>();
        wallpaper.sprite = backgroundArtwork;
        wallpaper.color = backgroundArtwork != null
            ? Color.white : new Color(0.035f, 0.065f, 0.11f);
        wallpaper.preserveAspect = false;
        wallpaper.raycastTarget = false;

        // Render the wallpaper within the fitted physical display bounds.
        var background = view.canvas.transform.Find("Background");
        if (background != null)
        {
            var image = background.GetComponent<Image>();
            // Put the wallpaper directly on the generated UI's background.
            // This does not depend on the parent Canvas showing through.
            image.sprite = backgroundArtwork;
            image.color = backgroundArtwork != null
                ? new Color(0.85f, 0.85f, 0.85f, 1f)
                : new Color(0.025f, 0.045f, 0.075f, 1f);
            image.preserveAspect = false;
            image.raycastTarget = false;
        }
        var rail = view.canvas.transform.Find("Conversation Rail");
        if (rail != null)
            rail.GetComponent<Image>().color = new Color(0.055f, 0.09f, 0.14f, 0.82f);
        var replyArea = view.canvas.transform.Find("Active Conversation/Reply Area");
        if (replyArea != null)
            replyArea.GetComponent<Image>().color = new Color(0.035f, 0.06f, 0.10f, 0.72f);

        // Keep the sidebar compact and the conversation wide.
        var railRect = rail as RectTransform;
        if (railRect != null) railRect.anchorMax = new Vector2(0.26f, 1);
        var chatRect = view.canvas.transform.Find("Active Conversation") as RectTransform;
        if (chatRect != null) chatRect.anchorMin = new Vector2(0.26f, 0);
    }

    private void AddEdge(string label, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var edge = new GameObject(label, typeof(RectTransform), typeof(Image));
        edge.transform.SetParent(view.canvas.transform, false);
        var rect = edge.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var image = edge.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void Update()
    {
        if (session != null && GameManager.EnsureInstance().CurrentDay != day) Unlock();
    }

    private void Unlock()
    {
        day = GameManager.EnsureInstance().CurrentDay;
        foreach (var conversation in NarrativeMessageDelivery.GetConversations(conversations))
            session.Unlock(conversation, day);
        if (selected == null)
            selected = (NarrativeMessageDelivery.GetConversations(conversations)).FirstOrDefault(c =>
                c != null && c.id == session.SelectedId && session.GetThread(c.id) != null)
                ?? (NarrativeMessageDelivery.GetConversations(conversations)).FirstOrDefault(c =>
                    c != null && session.GetThread(c.id) != null);
        Refresh();
    }

    private void Select(MessageConversation conversation)
    {
        selected = conversation;
        Refresh();
    }

    private void Reply(int index)
    {
        if (selected != null && session.Reply(selected, index)) Refresh();
    }

    private void Refresh()
    {
        if (session == null || refreshing || view == null) return;
        refreshing = true;
        try
        {
            if (selected != null) session.Read(selected.id);
            MessagingWindowView.Clear(contacts);
            MessagingWindowView.Clear(history);
            MessagingWindowView.Clear(replies);
            foreach (var conversation in NarrativeMessageDelivery.GetConversations(conversations))
            {
                if (conversation == null) continue;
                var thread = session.GetThread(conversation.id);
                if (thread == null) continue;
                var captured = conversation;
                string preview = thread.messages.Count > 0 ? thread.messages[thread.messages.Count - 1].text : "";
                view.AddContact(conversation, preview, session.Unread(conversation.id),
                    conversation == selected, () => Select(captured));
            }

            view.SetActiveContact(selected);
            Canvas.ForceUpdateCanvases();
            if (selected == null)
            {
                view.Label(history, "No conversations are available yet. Return when a contact sends you a message.",
                    24, TextAlignmentOptions.TopLeft, new Color(0.62f, 0.69f, 0.81f));
            }
            else
            {
                var thread = session.GetThread(selected.id);
                if (thread != null)
                {
                    foreach (var entry in thread.messages) AddBubble(entry);
                    if (!thread.replied && selected.replies != null)
                    {
                        for (int i = 0; i < selected.replies.Length; i++)
                        {
                            if (selected.replies[i] == null) continue;
                            int index = i;
                            var button = CreateReplyButton(replies, selected.replies[i].text, () => Reply(index), 26);
                            var label = button.GetComponentInChildren<TMP_Text>();
                            label.font = view.messageFont;
                            if (label.font != null) label.fontSharedMaterial = label.font.material;
                            label.textWrappingMode = TextWrappingModes.Normal;
                            float textWidth = Mathf.Max(120, replies.rect.width - 44);
                            button.GetComponent<LayoutElement>().preferredHeight =
                                Mathf.Max(42, label.GetPreferredValues(label.text, textWidth, 0).y + 16);
                        }
                    }
                    else view.Label(replies, "Conversation complete. You can return and read it anytime.",
                        22, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.69f, 0.81f));
                }
            }
            FitReplyArea();
            Canvas.ForceUpdateCanvases();
            if (historyScroll != null) historyScroll.verticalNormalizedPosition = 0;
        }
        finally { refreshing = false; }
    }

    private Button CreateReplyButton(
        Transform parent,
        string text,
        UnityEngine.Events.UnityAction action,
        float fontSize)
    {
        var root = new GameObject("Reply Option", typeof(RectTransform),
            typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var background = root.GetComponent<Image>();
        background.color = new Color(0.065f, 0.09f, 0.125f);
        var button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        root.GetComponent<LayoutElement>().preferredHeight = 68;

        var label = view.Label(root.transform, text, fontSize,
            TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.94f, 0.98f));
        label.GetComponent<LayoutElement>().ignoreLayout = true;
        label.rectTransform.anchorMin = new Vector2(0.035f, 0.08f);
        label.rectTransform.anchorMax = new Vector2(0.965f, 0.92f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private void FitReplyArea()
    {
        var area = view.canvas.transform.Find("Active Conversation/Reply Area") as RectTransform;
        if (area == null) return;
        var layout = replies.GetComponent<VerticalLayoutGroup>();
        float needed = 36 + layout.padding.vertical;
        int count = 0;
        foreach (Transform child in replies)
        {
            if (!child.gameObject.activeSelf) continue;
            var size = child.GetComponent<LayoutElement>();
            needed += size != null ? Mathf.Max(size.minHeight, size.preferredHeight) : 42;
            count++;
        }
        needed += Mathf.Max(0, count - 1) * layout.spacing;
        float parentHeight = Mathf.Max(1, ((RectTransform)area.parent).rect.height);
        float fraction = Mathf.Clamp(needed / parentHeight, 0.16f, 0.50f);
        area.anchorMax = new Vector2(1, fraction);
        ((RectTransform)historyScroll.transform).anchorMin = new Vector2(0, fraction + 0.015f);
    }

    private void AddBubble(MessagingSession.Entry entry)
    {
        var row = new GameObject("Message Row", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(history, false);
        var bubble = new GameObject(entry.fromPlayer ? "Your Message" : "Incoming Message",
            typeof(RectTransform), typeof(Image));
        bubble.transform.SetParent(row.transform, false);
        var image = bubble.GetComponent<Image>();
        image.sprite = GetBubbleSprite();
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1;
        image.color = entry.fromPlayer ? outgoingBubbleColor : incomingBubbleColor;
        image.raycastTarget = false;

        var message = view.Label(bubble.transform, entry.text ?? "", chatFontSize,
            TextAlignmentOptions.TopLeft, Color.white);
        message.enableAutoSizing = false;
        message.textWrappingMode = TextWrappingModes.Normal;
        message.overflowMode = TextOverflowModes.Overflow;

        const float padding = 16;
        float usableWidth = Mathf.Max(120, history.rect.width - 24);
        float maxWidth = usableWidth * maximumBubbleWidth;
        float minWidth = Mathf.Min(90, maxWidth);
        float naturalWidth = message.GetPreferredValues(message.text, Mathf.Infinity, Mathf.Infinity).x;
        float width = Mathf.Clamp(naturalWidth + padding * 2, minWidth, maxWidth);
        float textHeight = message.GetPreferredValues(message.text, Mathf.Max(1, width - padding * 2), Mathf.Infinity).y;
        float height = Mathf.Max(chatFontSize + padding * 2, textHeight + padding * 2);
        var sizing = row.GetComponent<LayoutElement>();
        sizing.minHeight = sizing.preferredHeight = height;
        var rect = bubble.GetComponent<RectTransform>();
        float side = entry.fromPlayer ? 1 : 0;
        rect.anchorMin = rect.anchorMax = new Vector2(side, 1);
        rect.pivot = new Vector2(side, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        message.rectTransform.anchorMin = Vector2.zero;
        message.rectTransform.anchorMax = Vector2.one;
        message.rectTransform.offsetMin = new Vector2(padding, padding);
        message.rectTransform.offsetMax = new Vector2(-padding, -padding);
    }

    private Sprite GetBubbleSprite()
    {
        if (bubbleSprite != null) return bubbleSprite;
        const int size = 32;
        const float radius = 8;
        bubbleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        bubbleTexture.name = "Chat Bubble Corners";
        bubbleTexture.filterMode = FilterMode.Bilinear;
        bubbleTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - distance + 0.5f));
            }
        }
        bubbleTexture.SetPixels(pixels);
        bubbleTexture.Apply();
        // A 100-unit reference keeps the nine-sliced corners at 8 canvas units.
        bubbleSprite = Sprite.Create(bubbleTexture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        return bubbleSprite;
    }

    private void OnDestroy()
    {
        if (session != null) session.Changed -= Refresh;
        if (bubbleSprite != null) Destroy(bubbleSprite);
        if (bubbleTexture != null) Destroy(bubbleTexture);
    }
}
