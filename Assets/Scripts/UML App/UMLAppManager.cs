using UnityEngine;

public class UMLAppManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Drag the UML_Canvas here")]
    public Transform umlCanvas;

    private UMLNode[] allNodes;

    private void Start()
    {
        // Find all UML nodes inside the canvas when the app is created
        if (umlCanvas != null)
        {
            allNodes = umlCanvas.GetComponentsInChildren<UMLNode>(true);
        }
    }

    // Call this when the player opens the app
    public void OnAppOpened()
    {
        if (allNodes == null || allNodes.Length == 0) return;

        // Fetch the current day from your main GameManager (Default to 1 if not found)
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        // Update every node's visibility
        foreach (UMLNode node in allNodes)
        {
            node.CheckRevealState(currentDay);
        }
    }
}