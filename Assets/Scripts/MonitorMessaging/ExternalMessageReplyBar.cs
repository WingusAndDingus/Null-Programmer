using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Draws messaging reply choices in a horizontal strip just below the physical monitor
/// screen. It is a sibling of "Messages Screen", so the screen's RectMask2D cannot clip it.
/// </summary>
[DisallowMultipleComponent]
public sealed class ExternalMessageReplyBar : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField, Min(40f)] private float barHeight = 96f;
    [SerializeField, Min(0f)] private float gapBelowScreen = 18f;
    [SerializeField, Min(10f)] private float fontSize = 23f;
    [SerializeField] private Color panelColor = new Color(0.025f, 0.045f, 0.075f, 0.96f);
    [SerializeField] private Color buttonColor = new Color(0.075f, 0.11f, 0.17f, 0.98f);
    [SerializeField] private Color endedTextColor = new Color(0.68f, 0.75f, 0.86f, 1f);

    private RectTransform root;
    private TMP_FontAsset messageFont;
    private bool initialized;

    public void Initialize(RectTransform screen, TMP_FontAsset font, float preferredFontSize)
    {
        if (screen == null || screen.parent == null)
        {
            Debug.LogError("[ExternalMessageReplyBar] A screen RectTransform with a parent is required.", this);
            return;
        }

        messageFont = font != null ? font : TMP_Settings.defaultFontAsset;
        fontSize = Mathf.Max(10f, preferredFontSize);
        CreateOrPositionRoot(screen);
        initialized = true;
    }

    private void CreateOrPositionRoot(RectTransform screen)
    {
        if (root == null)
        {
            var go = new GameObject("External Message Replies",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            go.layer = screen.gameObject.layer;
            go.transform.SetParent(screen.parent, false);
            root = go.GetComponent<RectTransform>();

            var background = go.GetComponent<Image>();
            background.color = panelColor;
            background.raycastTarget = true;

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        root.SetParent(screen.parent, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);

        Vector3 centerBelowScreen = new Vector3(
            screen.rect.center.x,
            screen.rect.yMin - gapBelowScreen - barHeight * 0.5f,
            0f);

        root.position = screen.TransformPoint(centerBelowScreen);
        root.rotation = screen.rotation;
        root.localScale = screen.localScale;
        root.sizeDelta = new Vector2(screen.rect.width, barHeight);
        root.SetAsLastSibling();
        root.gameObject.SetActive(false);

        Canvas hostCanvas = GetComponent<Canvas>();
        if (hostCanvas != null && hostCanvas.GetComponent<GraphicRaycaster>() == null)
            hostCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    public void Show(MessageConversation.Reply[] choices, UnityAction<int> onSelected)
    {
        if (!initialized || root == null)
            return;

        MessagingWindowView.Clear(root);

        if (choices == null || choices.Length == 0)
        {
            root.gameObject.SetActive(false);
            return;
        }

        root.gameObject.SetActive(true);

        for (int i = 0; i < choices.Length; i++)
        {
            MessageConversation.Reply choice = choices[i];
            if (choice == null || string.IsNullOrWhiteSpace(choice.text))
                continue;

            int capturedIndex = i;
            CreateChoiceButton(choice.text, () => onSelected?.Invoke(capturedIndex));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    /// <summary>Shows a non-interactive status when the contact has no more replies today.</summary>
    public void ShowEnded(string message = "Conversation ended for today.")
    {
        if (!initialized || root == null)
            return;

        MessagingWindowView.Clear(root);
        root.gameObject.SetActive(true);

        var labelObject = new GameObject("Conversation Ended Status",
            typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(root, false);

        var size = labelObject.GetComponent<LayoutElement>();
        size.minWidth = 135f;
        size.minHeight = 64f;
        size.preferredHeight = 72f;
        size.flexibleWidth = 1f;
        size.flexibleHeight = 1f;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = messageFont != null ? messageFont : TMP_Settings.defaultFontAsset;
        if (label.font != null)
            label.fontSharedMaterial = label.font.material;
        label.text = message;
        label.fontSize = Mathf.Max(16f, fontSize - 2f);
        label.color = endedTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    public void Hide()
    {
        if (root == null)
            return;

        root.gameObject.SetActive(false);
        MessagingWindowView.Clear(root);
    }

    private void CreateChoiceButton(string text, UnityAction action)
    {
        var go = new GameObject("External Reply Choice",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(root, false);

        var image = go.GetComponent<Image>();
        image.color = buttonColor;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(action);

        var size = go.GetComponent<LayoutElement>();
        size.minWidth = 135f;
        size.preferredWidth = 200f;
        size.minHeight = 70f;
        size.preferredHeight = 78f;
        size.flexibleWidth = 1f;
        size.flexibleHeight = 1f;

        var labelObject = new GameObject("Reply Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(go.transform, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = messageFont != null ? messageFont : TMP_Settings.defaultFontAsset;
        if (label.font != null)
            label.fontSharedMaterial = label.font.material;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.045f, 0.06f);
        rect.anchorMax = new Vector2(0.955f, 0.94f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
