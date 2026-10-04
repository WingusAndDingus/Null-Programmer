using System;
using UnityEngine;

public class DayControllerLoader : MonoBehaviour
{
    [Header("Day Script Mapping")]
    [Tooltip("Add your Day Controllers here in order (Index 0 = Day 1, Index 1 = Day 2, etc.)")]
    [SerializeField]
    private string[] dayControllerTypeNames = new string[]
    {
        "DayOneController",
        "DayTwoController",
        "DayThreeController"
    };

    private DayScriptBase activeDayScript;

    private void Start()
    {
        GameManager.EnsureInstance();

        // 1. Fetch current day from GameManager (assuming 1-indexed: Day 1, Day 2, etc.)
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        // 2. Load the corresponding day script dynamically
        LoadDayScript(currentDay);
    }

    public void LoadDayScript(int dayNumber)
    {
        // Remove any existing DayScriptBase component on this GameObject
        DayScriptBase existingScript = GetComponent<DayScriptBase>();
        if (existingScript != null)
        {
            Destroy(existingScript);
        }

        int targetIndex = dayNumber - 1;

        if (targetIndex < 0 || targetIndex >= dayControllerTypeNames.Length)
        {
            Debug.LogError($"[DayControllerLoader] No day controller configured for Day {dayNumber}. Index out of bounds.");
            return;
        }

        string typeName = dayControllerTypeNames[targetIndex];
        Type scriptType = Type.GetType(typeName);

        if (scriptType == null)
        {
            Debug.LogError($"[DayControllerLoader] Could not resolve System.Type for class '{typeName}'. Check class name spelling.");
            return;
        }

        // Add the script dynamically; Unity will immediately run its Awake() and Start()
        activeDayScript = gameObject.AddComponent(scriptType) as DayScriptBase;
        Debug.Log($"[DayControllerLoader] Successfully attached and started {typeName} for Day {dayNumber}.");
    }
}
