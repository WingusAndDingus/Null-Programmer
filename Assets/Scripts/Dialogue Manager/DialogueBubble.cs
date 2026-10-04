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

    private void Awake()
    {
        timeOffset = UnityEngine.Random.Range(0f, 100f);
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    public void Setup(DialogueChoiceOption option, int index, Color color, Action<int> callback)
    {
        optionIndex = index;
        optionText.text = option.text;
        bubbleImage.color = color;
        onSelectCallback = callback;
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        // Smooth floating motion
        float newY = initialPosition.y + Mathf.Sin((Time.unscaledTime + timeOffset) * floatSpeed) * floatDistance;
        transform.localPosition = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnClicked()
    {
        onSelectCallback?.Invoke(optionIndex);
    }
}
