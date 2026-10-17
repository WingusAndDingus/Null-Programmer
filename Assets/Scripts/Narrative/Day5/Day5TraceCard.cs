using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Day5TraceCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text cardText;
    [SerializeField] private TMP_Text orderText;

    public int CorrectOrderIndex { get; private set; }

    private Day5TraceTurnMiniGame owner;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Configure(Day5TraceTurnMiniGame game, int correctOrderIndex, string label)
    {
        owner = game;
        CorrectOrderIndex = correctOrderIndex;

        if (cardText != null)
        {
            cardText.text = label;
        }

        ResetCard();
    }

    public void MarkSelected(int selectedOrderIndex)
    {
        if (button != null)
        {
            button.interactable = false;
        }

        if (orderText != null)
        {
            orderText.text = (selectedOrderIndex + 1).ToString();
            orderText.gameObject.SetActive(true);
        }
    }

    public void ResetCard()
    {
        if (button != null)
        {
            button.interactable = true;
        }

        if (orderText != null)
        {
            orderText.text = string.Empty;
            orderText.gameObject.SetActive(false);
        }
    }

    private void HandleClick()
    {
        owner?.SelectCard(this);
    }
}
