using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Day5TraceCard : MonoBehaviour
{
    [FormerlySerializedAs("cardButton")]
    [SerializeField] private Button button;

    [FormerlySerializedAs("cardLabel")]
    [FormerlySerializedAs("labelText")]
    [SerializeField] private TMP_Text label;

    [FormerlySerializedAs("orderNumber")]
    [FormerlySerializedAs("orderText")]
    [SerializeField] private TMP_Text orderLabel;

    private Day5TraceTurnMiniGame owner;
    private int correctIndex;
    private bool selected;

    public int CorrectIndex => correctIndex;
    public bool IsSelected => selected;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Reset()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (button == null) button = GetComponent<Button>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (label == null && texts.Length > 0)
        {
            label = texts[0];
        }

        if (orderLabel == null && texts.Length > 1)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                string lowerName = texts[i].gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("order") || lowerName.Contains("number"))
                {
                    orderLabel = texts[i];
                    break;
                }
            }

            if (orderLabel == null) orderLabel = texts[texts.Length - 1];
        }
    }

    public void Configure(Day5TraceTurnMiniGame controller, int index, string displayText)
    {
        EnsureReferences();
        owner = controller;
        correctIndex = index;

        if (label != null) label.text = displayText;
        SetSelectionOrder(0);

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }
    }

    // Kept public so an existing prefab Button OnClick can point here too.
    public void Select()
    {
        owner?.SelectCard(this);
    }

    public void SetSelectionOrder(int order)
    {
        selected = order > 0;
        if (orderLabel != null)
        {
            orderLabel.text = selected ? order.ToString() : "";
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }
}
