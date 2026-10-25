using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DialogueBubble : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private TMP_Text optionText;

    [Header("Readable Choice Cards")]
    [SerializeField] private bool useRoundedCards = true;
    [SerializeField] private Vector2 textPadding = new Vector2(30f, 26f);
    [SerializeField] private float minimumHeight = 160f;

    [Header("Floating Animation")]
    [SerializeField] private float floatSpeed = 0.8f;
    [SerializeField] private float floatDistance = 4f;
    [Header("Entrance Animation")]
    [SerializeField] private float entranceDuration = 0.25f;
    [SerializeField] private float startScale = 0.98f;
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, -18f);

    private static Sprite roundedSprite;
    private Sprite authoredSprite;
    private RectTransform bubbleRect;
    private TMP_Text personalityText;
    private CanvasGroup canvasGroup;
    private Coroutine entrance;
    private Action<int> onSelectCallback;
    private int optionIndex;
    private Vector2 initialPosition;
    private float timeOffset;
    private bool listenerBound;
    private bool isInitialized;
    private bool selected;
    private string message = "";
    private string personalityName = "";

    public TMP_Text MessageText => optionText;
    public Vector2 LayoutSize => bubbleRect != null ? bubbleRect.sizeDelta : Vector2.zero;

    private void Awake()
    {
        timeOffset = UnityEngine.Random.Range(0f, 100f);
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (bubbleRect == null) bubbleRect = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();
        if (bubbleImage == null) bubbleImage = GetComponent<Image>();
        if (optionText == null) optionText = GetComponentInChildren<TMP_Text>(true);
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (bubbleImage != null && authoredSprite == null) authoredSprite = bubbleImage.sprite;
        if (button != null && !listenerBound)
        {
            button.onClick.AddListener(OnClicked);
            listenerBound = true;
        }
    }

    public void Setup(DialogueChoiceOption option, int index, Color color, Action<int> callback)
    {
        EnsureReferences();
        if (bubbleRect == null || optionText == null || button == null || bubbleImage == null)
            throw new InvalidOperationException("A dialogue choice needs a RectTransform, Button, Image and TMP text.");

        optionIndex = index;
        message = option.text ?? "";
        personalityName = PersonalityName(option.personality);
        onSelectCallback = callback;
        selected = false;
        isInitialized = false;
        bubbleRect.localScale = Vector3.one;
        bubbleRect.localRotation = Quaternion.identity;
        optionText.rectTransform.localScale = Vector3.one;
        optionText.rectTransform.localRotation = Quaternion.identity;
        optionText.text = message;
        optionText.richText = false; // C++ comparisons and template syntax are literal text.
        optionText.raycastTarget = false;
        optionText.enableAutoSizing = false;
        optionText.textWrappingMode = TextWrappingModes.Normal;
        optionText.overflowMode = TextOverflowModes.Truncate;
        optionText.margin = Vector4.zero;
        optionText.alignment = TextAlignmentOptions.TopLeft;
        optionText.maxVisibleCharacters = int.MaxValue;

        color.a = 1f;
        bubbleImage.color = color;
        bubbleImage.sprite = useRoundedCards ? GetRoundedSprite() : authoredSprite;
        bubbleImage.type = useRoundedCards ? Image.Type.Sliced : Image.Type.Simple;
        bubbleImage.preserveAspect = false;
        Color ink = color.grayscale > 0.48f ? new Color(0.07f, 0.09f, 0.12f) : Color.white;
        optionText.color = ink;

        if (!string.IsNullOrEmpty(personalityName) && personalityText == null)
        {
            var heading = new GameObject("Personality", typeof(RectTransform), typeof(TextMeshProUGUI));
            heading.layer = gameObject.layer;
            heading.transform.SetParent(transform, false);
            personalityText = heading.GetComponent<TMP_Text>();
        }
        if (personalityText != null)
        {
            personalityText.gameObject.SetActive(!string.IsNullOrEmpty(personalityName));
            personalityText.font = optionText.font;
            personalityText.text = personalityName;
            personalityText.color = ink;
            personalityText.fontStyle = FontStyles.Bold;
            personalityText.enableAutoSizing = false;
            personalityText.richText = false;
            personalityText.raycastTarget = true; //we need this to make sure it can capture a mouse click
            personalityText.alignment = TextAlignmentOptions.TopLeft;
            personalityText.margin = Vector4.zero;
        }
        var shadow = GetComponent<Shadow>();
        if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(0f, -5f);
        shadow.useGraphicAlpha = true;
    }

    private float HeadingHeight(float fontSize) => string.IsNullOrEmpty(personalityName) ? 0f : fontSize * 0.85f + 12f;
    private float HorizontalPadding(float width) => useRoundedCards ? textPadding.x : width * 0.2f;

    // Measure at the final width and font size, before placing any cards.
    public float MeasureHeight(float width, float fontSize)
    {
        optionText.fontSize = fontSize;
        float bodyHeight = Mathf.Ceil(optionText.GetPreferredValues(message,
            Mathf.Max(1f, width - 2f * HorizontalPadding(width)), Mathf.Infinity).y) + 10f;
        float content = bodyHeight + HeadingHeight(fontSize);
        return Mathf.Max(minimumHeight, useRoundedCards ? content + 2f * textPadding.y : content / 0.55f);
    }

    public void FitToText(float width, float fontSize, float rowHeight = 0f)
    {
        float height = Mathf.Max(MeasureHeight(width, fontSize), rowHeight + 50f);
        bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 1f);
        bubbleRect.pivot = new Vector2(0.5f, 1f);
        bubbleRect.sizeDelta = new Vector2(width, height);
        float headingHeight = HeadingHeight(fontSize);
        float bodyHeight = Mathf.Ceil(optionText.GetPreferredValues(message,
            Mathf.Max(1f, width - 2f * HorizontalPadding(width)), Mathf.Infinity).y) + 4f;
        float top = useRoundedCards ? textPadding.y : (height - bodyHeight - headingHeight) * 0.5f;
        top -= 40f;
        PlaceText(optionText.rectTransform, HorizontalPadding(width), top + headingHeight, bodyHeight);
        if (personalityText != null)
        {
            personalityText.fontSize = fontSize * 0.67f;
            PlaceText(personalityText.rectTransform, HorizontalPadding(width), top, headingHeight);
        }
    }

    private static void PlaceText(RectTransform rect, float padding, float top, float height)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-2f * padding, height);
        rect.anchoredPosition = new Vector2(0f, -top);
    }

    public void SetLayoutPosition(Vector2 position, bool animate)
    {
        if (entrance != null) StopCoroutine(entrance);
        initialPosition = position;
        bubbleRect.localScale = Vector3.one;
        bubbleRect.anchoredPosition = position;
        canvasGroup.alpha = animate ? 0f : 1f;
        isInitialized = !animate;
        button.interactable = !animate && !selected;
        if (animate) entrance = StartCoroutine(PlayEntranceAnimation());
    }

    // Kept for other scripts that still position an individual bubble directly.
    public void SetInitialPosition(Vector3 position)
    {
        FitToText(520f, 30f);
        bubbleRect.anchorMin = bubbleRect.anchorMax = bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        SetLayoutPosition(position, true);
    }

    private IEnumerator PlayEntranceAnimation()
    {
        float duration = Mathf.Clamp(entranceDuration, 0.1f, 0.5f);
        float elapsed = 0f;
        Vector2 from = initialPosition + Vector2.ClampMagnitude(slideOffset, 18f);
        Vector3 fromScale = Vector3.one * Mathf.Clamp(startScale, 0.98f, 1f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            canvasGroup.alpha = t;
            bubbleRect.localScale = Vector3.Lerp(fromScale, Vector3.one, t);
            bubbleRect.anchoredPosition = Vector2.Lerp(from, initialPosition, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        bubbleRect.localScale = Vector3.one;
        bubbleRect.anchoredPosition = initialPosition;
        button.interactable = !selected;
        isInitialized = true;
        entrance = null;
    }

    private void Update()
    {
        if (isInitialized)
            bubbleRect.anchoredPosition = initialPosition + Vector2.up *
                (Mathf.Sin((Time.unscaledTime + timeOffset) * floatSpeed) * Mathf.Clamp(floatDistance, 0f, 4f));
    }

    private void OnClicked()
    {
        if (!isInitialized || selected) return;
        selected = true;
        button.interactable = false;
        onSelectCallback?.Invoke(optionIndex);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private static string PersonalityName(HeadPersonality personality)
    {
        switch (personality)
        {
            case HeadPersonality.GrandArchitect: return "ARCHITECT";
            case HeadPersonality.Pragmatist: return "PRAGMATIST";
            case HeadPersonality.Sentinel: return "SENTINEL";
            case HeadPersonality.Genius: return "GENIUS";
            case HeadPersonality.Detective: return "DETECTIVE";
            default: return "";
        }
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 32;
        const int radius = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        { name = "Dialogue Card", filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius));
                float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius));
                byte alpha = (byte)(dx <= 0f || dy <= 0f || dx * dx + dy * dy <= radius * radius ? 255 : 0);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        roundedSprite.name = "Dialogue Card (Sliced)";
        roundedSprite.hideFlags = HideFlags.DontSave;
        return roundedSprite;
    }
}
