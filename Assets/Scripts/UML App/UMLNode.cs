using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UMLNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Battleship Class Data")]
    public string title;
    [TextArea(3, 5)] public string attributes;
    [TextArea(3, 5)] public string methods;
    [TextArea(4, 6)] public string description;
    public int unlockDay;

    [Header("UI Component Links")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI attributesText;
    public TextMeshProUGUI methodsText;
    public GameObject unknownOverlay;
    public Image backgroundImage; 

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.05f;
    public Color hoverColor = new Color(0.4f, 0.4f, 0.5f, 0.9f);
    public float transitionSpeed = 12f;

    private Vector3 originalScale;
    private Color originalColor;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isUnlocked = false;

    private void OnValidate()
    {
        if (titleText != null) titleText.text = title;
        if (attributesText != null) attributesText.text = attributes;
        if (methodsText != null) methodsText.text = methods;
    }

    private void Start()
    {
        OnValidate();
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
            targetColor = originalColor;
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    public void CheckRevealState(int currentInGameDay)
    {
        isUnlocked = currentInGameDay >= unlockDay;
        if (unknownOverlay != null)
        {
            unknownOverlay.SetActive(!isUnlocked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        targetScale = originalScale * hoverScaleMultiplier;
        if (backgroundImage != null) targetColor = hoverColor;
        transform.SetAsLastSibling(); 
    }

    public void OnPointerExit(PointerEventData eventData) {
        targetScale = originalScale;
        if (backgroundImage != null) targetColor = originalColor;
    }

    // Automatically triggers when the node is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            // Find the manager on the parent window and send the text
            UMLAppManager manager = GetComponentInParent<UMLAppManager>();
            if (manager != null)
            {
                manager.ShowDescription(title, description);
            }
        }
    }
}