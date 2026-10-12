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

    [Header("Floating Animation")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatDistance = 10f;

    [Header("Entrance Animation")]
    [SerializeField] private float entranceDuration = 0.5f;
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private Vector2 slideOffset = new Vector2(0, -60f);

    private Action<int> onSelectCallback;
    private int optionIndex;
    private Vector3 initialPosition;
    private Vector3 originalScale; // Cached prefab scale
    private float timeOffset;
    private bool isInitialized = false;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        timeOffset = UnityEngine.Random.Range(0f, 100f);
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Cache the original local scale set in the Inspector/Prefab
        originalScale = transform.localScale;
    }

    public void Setup(DialogueChoiceOption option, int index, Color color, Action<int> callback)
    {
        optionIndex = index;
        if (optionText != null) optionText.text = option.text;
        if (bubbleImage != null) bubbleImage.color = color;
        onSelectCallback = callback;
    }

    public void SetInitialPosition(Vector3 position)
    {
        initialPosition = position;

        // Set initial invisible state relative to original scale
        canvasGroup.alpha = 0f;
        transform.localScale = originalScale * startScale;
        transform.localPosition = position + (Vector3)slideOffset;

        StartCoroutine(PlayEntranceAnimation());
    }

    private IEnumerator PlayEntranceAnimation()
    {
        float elapsed = 0f;
        Vector3 startPos = initialPosition + (Vector3)slideOffset;
        Vector3 scaledStart = originalScale * startScale;

        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            transform.localScale = Vector3.Lerp(scaledStart, originalScale, smoothT);
            transform.localPosition = Vector3.Lerp(startPos, initialPosition, smoothT);

            yield return null;
        }

        // Lock in exact target transform using the original prefab scale
        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
        transform.localPosition = initialPosition;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        float newY = initialPosition.y + Mathf.Sin((Time.unscaledTime + timeOffset) * floatSpeed) * floatDistance;
        transform.localPosition = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnClicked()
    {
        onSelectCallback?.Invoke(optionIndex);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }
}
