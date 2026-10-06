using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueBubble : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private TMP_Text optionText;

    [Header("Floating Animation")]
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatDistance = 10f;

    private Action<int> onSelectCallback;
    private int optionIndex;
    private Vector3 initialPosition;
    private float timeOffset;
    private bool isInitialized = false;

    private void Awake()
    {
        timeOffset = UnityEngine.Random.Range(0f, 100f);
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    public void Setup(DialogueChoiceOption option, int index, Color color, Action<int> callback)
    {
        optionIndex = index;
        if (optionText != null) optionText.text = option.text;
        if (bubbleImage != null) bubbleImage.color = color;
        onSelectCallback = callback;
    }

    /// <summary>
    /// Call this after DialogueManager sets transform.localPosition
    /// so floating undulates around the assigned layout spot.
    /// </summary>
    public void SetInitialPosition(Vector3 position)
    {
        transform.localPosition = position;
        initialPosition = position;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Smooth floating motion (sine wave undulation)
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
