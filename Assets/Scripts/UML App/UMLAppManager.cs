using UnityEngine;
using TMPro;

public class UMLAppManager : MonoBehaviour
{
    [Header("Configuration")]
    public Transform umlCanvas;

    [Header("Description Panel UI")]
    public GameObject descriptionPanel;
    public TextMeshProUGUI descriptionTitleText;
    public TextMeshProUGUI descriptionBodyText;

    private UMLNode[] allNodes;

    private void Start()
    {
        if (umlCanvas != null)
        {
            allNodes = umlCanvas.GetComponentsInChildren<UMLNode>(true);
        }
        
        // Ensure the panel is hidden when the app first opens
        if (descriptionPanel != null) descriptionPanel.SetActive(false);
    }

    public void OnAppOpened()
    {
        if (allNodes == null || allNodes.Length == 0) return;
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        foreach (UMLNode node in allNodes)
        {
            node.CheckRevealState(currentDay);
        }
    }

    // Called by the node when it is clicked
    public void ShowDescription(string title, string body)
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(true);
            if (descriptionTitleText != null) descriptionTitleText.text = title + " Overview";
            if (descriptionBodyText != null) descriptionBodyText.text = body;
            
            // Pull the panel to the absolute front of the Canvas
            descriptionPanel.transform.SetAsLastSibling();
        }
    }

    public void CloseDescription()
    {
        if (descriptionPanel != null) descriptionPanel.SetActive(false);
    }
}