using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    public Rect displayPixels = new Rect(548, 367, 578, 323);

    [Header("Messaging Appearance")]
    [Tooltip("Brighter incoming bubbles for the latest message group awaiting a player reply.")]
    public Color pendingIncomingBubbleColor = new Color(0.19f, 0.32f, 0.50f, 0.98f);
    [Tooltip("Muted incoming bubbles after the player has responded to that message group.")]
    public Color archivedIncomingBubbleColor = new Color(0.075f, 0.11f, 0.17f, 0.98f);
    public Sprite backgroundArtwork;
    [Range(700, 1200)] public float referenceWidth = 900;
    [Range(18, 36)] public float chatFontSize = 26;
    [Range(0.45f, 0.85f)] public float maximumBubbleWidth = 0.76f;
    public Color incomingBubbleColor = new Color(0.10f, 0.17f, 0.25f, 0.97f);
    public Color outgoingBubbleColor = new Color(0.16f, 0.30f, 0.50f, 0.98f);

    private MessagingWindowView view;
    private JsonMessageDialogueController jsonDialogueController;
    private bool hasRenderedHistory;
    private ExternalMessageReplyBar externalReplyBar;
    private string lastRenderedHistoryId;
    private Coroutine historyScrollRoutine;
    private int lastRenderedMessageCount = -1;
    public Color systemBubbleColor = new Color(0.025f, 0.075f, 0.15f, 0.98f);
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
        jsonDialogueController = GetComponent<JsonMessageDialogueController>();
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
        Canvas.ForceUpdateCanvases();
        externalReplyBar = GetComponent<ExternalMessageReplyBar>();
        if (externalReplyBar == null)
            externalReplyBar = gameObject.AddComponent<ExternalMessageReplyBar>();
        externalReplyBar.Initialize(screen, view.messageFont, 23f);
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

        // Existing scenes retain their serialized Inspector values when a script changes.
        // Upgrade only the old stock bounds; keep any deliberately customized rectangle.
        if (displayPixels == new Rect(550, 378, 572, 306))
            displayPixels = new Rect(548, 367, 578, 323);

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
        if (history == null || historyScroll == null || view == null)
        {
            Debug.LogError("[MonitorMessagingApp] Cannot configure message history: UI references are missing.", this);
            return;
        }

        // Message rows set their preferred height in AddBubble(). The layout group
        // MUST control child height or those preferred heights are ignored.
        var historyLayout = history.GetComponent<VerticalLayoutGroup>();
        if (historyLayout == null)
            historyLayout = history.gameObject.AddComponent<VerticalLayoutGroup>();

        historyLayout.childControlWidth = true;
        historyLayout.childControlHeight = true;
        historyLayout.childForceExpandWidth = true;
        historyLayout.childForceExpandHeight = false;
        historyLayout.childAlignment = TextAnchor.UpperLeft;
        historyLayout.spacing = 14f;
        historyLayout.padding = new RectOffset(12, 12, 14, 14);

        // Replies are drawn by ExternalMessageReplyBar outside the physical screen.
        // Hide the legacy in-screen panel completely; it is the dark rectangle
        // that used to sit between the message history and the external buttons.
        Transform replyArea = view.canvas.transform.Find("Active Conversation/Reply Area");
        if (replyArea != null)
            replyArea.gameObject.SetActive(false);

        // Expand the history to use the available conversation area below the header.
        RectTransform historyArea = historyScroll.transform as RectTransform;
        historyArea.anchorMin = new Vector2(0f, 0f);
        historyArea.anchorMax = new Vector2(1f, 0.85f);
        historyArea.offsetMin = new Vector2(12f, 4f);
        historyArea.offsetMax = new Vector2(-12f, -5f);

        RectTransform viewport = historyScroll.transform.Find("History Viewport") as RectTransform;
        if (viewport == null)
        {
            Debug.LogError("[MonitorMessagingApp] History Viewport child is missing.", this);
            return;
        }

        // Add a transparent Graphic to the viewport. RectMask2D alone does not
        // receive pointer/wheel events, so scrolling over blank parts of the chat
        // can otherwise fail even though the contacts list scrolls correctly.
        Image viewportGraphic = viewport.GetComponent<Image>();
        if (viewportGraphic == null)
            viewportGraphic = viewport.gameObject.AddComponent<Image>();
        viewportGraphic.color = new Color(0f, 0f, 0f, 0f);
        viewportGraphic.raycastTarget = true;

        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        historyScroll.enabled = true;
        historyScroll.horizontal = false;
        historyScroll.vertical = true;
        historyScroll.inertia = true;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.scrollSensitivity = 40f;
        historyScroll.viewport = viewport;
        historyScroll.content = history;

        // ContentSizeFitter on History determines its height from the VerticalLayoutGroup.
        // Keep it stretched horizontally but top-anchored vertically so it grows down.
        history.anchorMin = new Vector2(0f, 1f);
        history.anchorMax = new Vector2(1f, 1f);
        history.pivot = new Vector2(0.5f, 1f);

        ContentSizeFitter fitter = history.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = history.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutRebuilder.ForceRebuildLayoutImmediate(history);
        Canvas.ForceUpdateCanvases();
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
        if (jsonDialogueController != null)
            jsonDialogueController.TryStartForDay(day);

        foreach (var conversation in NarrativeMessageDelivery.GetConversations(conversations))
            session.Unlock(conversation, day);
        if (selected == null)
            selected = (NarrativeMessageDelivery.GetConversations(conversations)).FirstOrDefault(c =>
                c != null && c.id == session.SelectedId && session.GetThread(c.id) != null)
                ?? (NarrativeMessageDelivery.GetConversations(conversations)).FirstOrDefault(c =>
                    c != null && session.GetThread(c.id) != null);
        if (jsonDialogueController != null)
            jsonDialogueController.SetSelectedConversation(selected != null ? selected.id : null);
        Refresh();
    }

    private void Select(MessageConversation conversation)
    {
        selected = conversation;
        if (jsonDialogueController != null)
            jsonDialogueController.SetSelectedConversation(selected != null ? selected.id : null);
        Refresh();
    }


    private void Reply(int index)
    {
        if (selected == null) return;

        if (jsonDialogueController != null &&
            jsonDialogueController.HandlesConversation(selected.id))
        {
            if (jsonDialogueController.TrySelectChoice(selected.id, index))
                Refresh();
            return;
        }

        if (session.Reply(selected, index)) Refresh();

    }


    private void Refresh()
    {
        if (session == null || refreshing || view == null) return;

        string selectedId = selected != null ? selected.id : null;
        float previousScrollPosition = historyScroll != null
            ? historyScroll.verticalNormalizedPosition
            : 0f;

        var selectedThreadBeforeRefresh = selected != null
            ? session.GetThread(selected.id)
            : null;
        int currentMessageCount = selectedThreadBeforeRefresh != null
            ? selectedThreadBeforeRefresh.messages.Count
            : 0;

        bool changedConversation = !string.Equals(
            selectedId, lastRenderedHistoryId, System.StringComparison.Ordinal);

        // Any newly appended message in the currently open thread should be brought
        // into view, even if the player was previously reading older history.
        // A simple read/unread refresh does not increase the count and preserves scroll.
        bool newMessageArrived = hasRenderedHistory && !changedConversation &&
            currentMessageCount > lastRenderedMessageCount;

        bool shouldStickToBottom = changedConversation || !hasRenderedHistory || newMessageArrived;
        refreshing = true;
        try
        {
            if (selected != null) session.Read(selected.id);

            MessagingWindowView.Clear(contacts);
            MessagingWindowView.Clear(history);

            foreach (var conversation in NarrativeMessageDelivery.GetConversations(conversations))
            {
                if (conversation == null) continue;

                var thread = session.GetThread(conversation.id);
                if (thread == null) continue;

                var captured = conversation;
                bool hasReplyOptions = !thread.replied && conversation.replies != null &&
                    System.Array.Exists(conversation.replies, reply => reply != null);

                string status = hasReplyOptions ? "Unread" : "";

                view.AddContact(conversation, status, session.Unread(conversation.id),
                    conversation == selected, () => Select(captured));
            }

            view.SetActiveContact(selected);
            Canvas.ForceUpdateCanvases();

            if (externalReplyBar != null)
                externalReplyBar.Hide();

            if (selected == null)
            {
                view.Label(history,
                    "No conversations are available yet. Return when a contact sends you a message.",
                    24, TextAlignmentOptions.TopLeft, new Color(0.62f, 0.69f, 0.81f));
            }
            else
            {
                var thread = session.GetThread(selected.id);
                if (thread != null)
                {
                    foreach (var entry in thread.messages)
                        AddBubble(entry);

                    bool hasReplyOptions = !thread.replied && selected.replies != null &&
                        System.Array.Exists(selected.replies, reply => reply != null);

                    if (hasReplyOptions && externalReplyBar != null)
                    {
                        externalReplyBar.Show(selected.replies, Reply);
                    }
                    else if (externalReplyBar != null && jsonDialogueController != null &&
                        jsonDialogueController.IsConversationFinished(selected.id))
                    {
                        externalReplyBar.ShowEnded("Conversation ended for today.");
                    }
                }
            }

            Canvas.ForceUpdateCanvases();
            if (history != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(history);
            Canvas.ForceUpdateCanvases();

            if (historyScrollRoutine != null)
                StopCoroutine(historyScrollRoutine);

            historyScrollRoutine = StartCoroutine(
                RestoreHistoryScroll(shouldStickToBottom, previousScrollPosition));

            hasRenderedHistory = true;
            lastRenderedHistoryId = selectedId;
            lastRenderedMessageCount = currentMessageCount;
        }
        finally
        {
            refreshing = false;
        }
    }
    private IEnumerator RestoreHistoryScroll(bool stickToBottom, float previousPosition)
    {
        // Wait for deferred Destroy calls and ContentSizeFitter/layout calculations.
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (history != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(history);

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        if (historyScroll != null && historyScroll.content != null && historyScroll.viewport != null)
        {
            historyScroll.StopMovement();

            float contentHeight = historyScroll.content.rect.height;
            float viewportHeight = historyScroll.viewport.rect.height;
            bool canScroll = contentHeight > viewportHeight + 2f;

            historyScroll.verticalNormalizedPosition = !canScroll || stickToBottom
                ? 0f
                : Mathf.Clamp01(previousPosition);
        }

        historyScrollRoutine = null;
    }

    private void AddBubble(MessagingSession.Entry entry)
    {
        var row = new GameObject("Message Row", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(history, false);

        string bubbleName = entry.isSystem ? "System Message" :
            (entry.fromPlayer ? "Your Message" : "Incoming Message");
        var bubble = new GameObject(bubbleName, typeof(RectTransform), typeof(Image));
        bubble.transform.SetParent(row.transform, false);

        var image = bubble.GetComponent<Image>();
        image.sprite = GetBubbleSprite();
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1;
        image.color = entry.isSystem ? systemBubbleColor :
            (entry.fromPlayer ? outgoingBubbleColor :
                (entry.isPending ? pendingIncomingBubbleColor : archivedIncomingBubbleColor));
        image.raycastTarget = false;

        var alignment = entry.isSystem ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
        var message = view.Label(bubble.transform, entry.text ?? "", chatFontSize,
            alignment, Color.white);
        message.enableAutoSizing = false;
        message.textWrappingMode = TextWrappingModes.Normal;
        message.overflowMode = TextOverflowModes.Overflow;

        const float padding = 16;
        float usableWidth = Mathf.Max(120, history.rect.width - 24);
        float maxWidth = usableWidth * maximumBubbleWidth;
        float minWidth = Mathf.Min(90, maxWidth);
        float naturalWidth = message.GetPreferredValues(message.text, Mathf.Infinity, Mathf.Infinity).x;
        float width = Mathf.Clamp(naturalWidth + padding * 2, minWidth, maxWidth);
        if (entry.isSystem)
            width = Mathf.Min(maxWidth * 0.82f, Mathf.Max(width, 220f));

        float textHeight = message.GetPreferredValues(
            message.text, Mathf.Max(1, width - padding * 2), Mathf.Infinity).y;
        float height = Mathf.Max(chatFontSize + padding * 2, textHeight + padding * 2);
        row.GetComponent<LayoutElement>().minHeight = height;
        row.GetComponent<LayoutElement>().preferredHeight = height;

        float horizontalAnchor = entry.isSystem ? 0.5f : (entry.fromPlayer ? 1f : 0f);
        var rect = bubble.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(horizontalAnchor, 1f);
        rect.pivot = new Vector2(horizontalAnchor, 1f);
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
        if (historyScrollRoutine != null)
            StopCoroutine(historyScrollRoutine);

        if (externalReplyBar != null)
            externalReplyBar.Hide();

        if (session != null) session.Changed -= Refresh;
        if (bubbleSprite != null) Destroy(bubbleSprite);
        if (bubbleTexture != null) Destroy(bubbleTexture);
    }
}
