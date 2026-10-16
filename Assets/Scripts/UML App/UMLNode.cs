using UnityEngine;
using TMPro;

public class UMLNode : MonoBehaviour
{
    [Header("Node Data")]
    public string title;
    [TextArea(3, 5)] public string attributes;
    [TextArea(3, 5)] public string methods;
    public int unlockDay;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI attributesText;
    public TextMeshProUGUI methodsText;
    public GameObject unknownOverlay;

    void OnValidate()
    {
        if (titleText != null) titleText.text = title;
        if (attributesText != null) attributesText.text = attributes;
        if (methodsText != null) methodsText.text = methods;
    }

    void Start()
    {
        if (titleText != null) titleText.text = title;
        if (attributesText != null) attributesText.text = attributes;
        if (methodsText != null) methodsText.text = methods;
    }

    public void CheckRevealState(int currentDay)
    {
        if (unknownOverlay != null) unknownOverlay.SetActive(currentDay < unlockDay);
    }
}