using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps presentation separate from dialogue timing and narrative progression.
public class DialoguePanelPresentation : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image speakerPortrait;

    [Header("Speaker Pictures")]
    [SerializeField] private Sprite stevePortrait;
    [SerializeField] private Sprite architectPortrait;
    [SerializeField] private Sprite pragmatistPortrait;
    [SerializeField] private Sprite detectivePortrait;

    [Header("Compact Panel (Canvas Units)")]
    [SerializeField] private float minimumHeight = 190f;
    [SerializeField] private float maximumHeight = 270f;
    [SerializeField, Range(0.15f, 0.35f)] private float maximumScreenHeight = 0.25f;
    [SerializeField] private float portraitSize = 150f;
    [SerializeField] private float bodyFontSize = 30f;

    private RectTransform viewport;
    private ScrollRect scroll;
    private Canvas canvas;
    private DialogueLine currentLine;
    private Vector2 lastCanvasSize;
    private float lastCanvasScale;
    private float contentHeight;

    public void Show(DialogueLine line)
    {
        if (panel == null || speakerText == null || dialogueText == null || line == null) return;
        currentLine = line;
        EnsureUI();
        ApplyLayout();
    }

    private void EnsureUI()
    {
        if (canvas == null) canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        var panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = panel.gameObject.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 1400;
        if (panel.GetComponent<GraphicRaycaster>() == null) panel.gameObject.AddComponent<GraphicRaycaster>();
        if (speakerPortrait == null)
        {
            var obj = new GameObject("Speaker Portrait", typeof(RectTransform), typeof(Image));
            obj.layer = panel.gameObject.layer;
            obj.transform.SetParent(panel, false);
            speakerPortrait = obj.GetComponent<Image>();
        }
        speakerPortrait.preserveAspect = true;
        speakerPortrait.raycastTarget = false;
        if (viewport != null) return;
        var body = new GameObject("Dialogue Text Viewport", typeof(RectTransform),
            typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        body.layer = panel.gameObject.layer;
        viewport = body.GetComponent<RectTransform>();
        viewport.SetParent(panel, false);
        body.GetComponent<Image>().color = Color.clear;
        scroll = body.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = dialogueText.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        dialogueText.rectTransform.SetParent(viewport, false);
    }

    private Sprite GetPortrait(DialogueLine line)
    {
        switch (line.personality)
        {
            case HeadPersonality.GrandArchitect:
            case HeadPersonality.Sentinel: return architectPortrait;
            case HeadPersonality.Pragmatist: return pragmatistPortrait;
            case HeadPersonality.Genius:
            case HeadPersonality.Detective: return detectivePortrait;
            case HeadPersonality.None:
                return string.Equals(line.speakerName?.Trim(), "Steve", StringComparison.OrdinalIgnoreCase)
                    ? stevePortrait : null;
            default: return null;
        }
    }

    private void ApplyLayout()
    {
        Vector2 size = ((RectTransform)canvas.transform).rect.size;
        float scale = Mathf.Max(0.01f, canvas.scaleFactor);

        panel.localScale = Vector3.one;
        panel.localRotation = Quaternion.identity;
        panel.anchorMin = new Vector2(0.02f, 0f);
        panel.anchorMax = new Vector2(0.98f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = Vector2.zero;

        Sprite portrait = GetPortrait(currentLine);
        speakerPortrait.sprite = portrait;
        speakerPortrait.gameObject.SetActive(portrait != null);
        float limit = size.y * maximumScreenHeight;
        float pictureSize = Mathf.Min(portraitSize, limit - 32f);
        float left = portrait != null ? pictureSize + 48f : 24f;
        float width = Mathf.Max(1f, size.x * 0.96f - left - 24f);
        float fontSize = Mathf.Max(bodyFontSize, 14f / scale);

        dialogueText.text = currentLine.text ?? "";
        dialogueText.enableAutoSizing = false;
        dialogueText.fontSize = fontSize;
        dialogueText.richText = false;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.overflowMode = TextOverflowModes.Overflow;
        dialogueText.margin = Vector4.zero;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.raycastTarget = false;
        float textHeight = Mathf.Ceil(dialogueText.GetPreferredValues(dialogueText.text, width, Mathf.Infinity).y) + 6f;
        float heading = fontSize * 0.9f + 10f;
        float top = 18f + heading;
        float height = limit;
        panel.sizeDelta = new Vector2(0f, height);

        RectTransform picture = speakerPortrait.rectTransform;
        picture.localScale = Vector3.one;
        picture.localRotation = Quaternion.identity;
        picture.anchorMin = picture.anchorMax = picture.pivot = new Vector2(0f, 0.5f);
        picture.anchoredPosition = new Vector2(24f, 0f);
        float fittedPicture = Mathf.Min(pictureSize, height - 32f);
        picture.sizeDelta = new Vector2(fittedPicture, fittedPicture);

        Image background = panel.GetComponent<Image>();
        Color ink = background != null && background.color.grayscale > 0.48f
            ? new Color(0.06f, 0.07f, 0.1f) : Color.white;
        dialogueText.color = ink;
        speakerText.color = ink;
        speakerText.enableAutoSizing = false;
        speakerText.fontSize = fontSize * 0.8f;
        speakerText.margin = Vector4.zero;
        speakerText.alignment = TextAlignmentOptions.TopLeft;
        speakerText.raycastTarget = false;
        RectTransform name = speakerText.rectTransform;
        name.localScale = Vector3.one;
        name.localRotation = Quaternion.identity;
        name.anchorMin = new Vector2(0f, 1f);
        name.anchorMax = new Vector2(1f, 1f);
        name.pivot = new Vector2(0.5f, 1f);
        name.offsetMin = new Vector2(left, -18f - heading);
        name.offsetMax = new Vector2(-24f, -18f);

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(left, 18f);
        viewport.offsetMax = new Vector2(-24f, -top);
        var content = dialogueText.rectTransform;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        contentHeight = Mathf.Max(textHeight, height - top - 18f);
        content.sizeDelta = new Vector2(0f, contentHeight);
        content.anchoredPosition = Vector2.zero;
        scroll.StopMovement();
        scroll.vertical = textHeight > height - top - 18f + 0.5f;
        lastCanvasSize = size;
        lastCanvasScale = scale;
    }

    // If a future line exceeds the compact panel, follow the text as it is typed.
    // The player can scroll back during the existing hold at the end of the line.
    public void FollowTyping(int visibleCharacters)
    {
        if (scroll == null || !scroll.vertical || visibleCharacters <= 0) return;
        dialogueText.ForceMeshUpdate();
        int count = dialogueText.textInfo.characterCount;
        if (count == 0) return;
        float bottom = -dialogueText.textInfo.characterInfo[Mathf.Min(visibleCharacters - 1, count - 1)].descender;
        float viewHeight = viewport.rect.height;
        float offset = Mathf.Clamp(bottom - viewHeight + 8f, 0f, Mathf.Max(0f, contentHeight - viewHeight));
        dialogueText.rectTransform.anchoredPosition = new Vector2(0f, offset);
    }

    private void LateUpdate()
    {
        if (currentLine == null || panel == null || !panel.gameObject.activeInHierarchy || canvas == null) return;
        if ((((RectTransform)canvas.transform).rect.size - lastCanvasSize).sqrMagnitude > 0.01f ||
            !Mathf.Approximately(canvas.scaleFactor, lastCanvasScale))
        {
            ApplyLayout();
            FollowTyping(dialogueText.maxVisibleCharacters);
        }
    }
}
