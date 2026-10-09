using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WeekendLockedTooltip : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip UI")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Locked Choice Explanation")]
    [TextArea(2, 5)]
    [SerializeField] private string lockedMessage;

    private Toggle choiceToggle;

    private void Awake()
    {
        choiceToggle = GetComponent<Toggle>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only display the tooltip for locked choices.
        if (choiceToggle == null || choiceToggle.interactable)
        {
            return;
        }

        if (tooltipPanel == null || tooltipText == null)
        {
            return;
        }

        tooltipText.text = lockedMessage;
        tooltipPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}