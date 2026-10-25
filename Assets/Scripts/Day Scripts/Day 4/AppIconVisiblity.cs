using UnityEngine;

public class AppIconVisibility : MonoBehaviour
{
    [Header("App Unlock Settings")]
    [Tooltip("The exact day this app becomes visible on the desktop.")]
    public int unlockDay = 4;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Subscribe to the event so it updates live
            GameManager.Instance.DayChanged += OnDayChanged;
            
            // Set initial state based on the current day upon loading the desktop
            UpdateVisibility(GameManager.Instance.CurrentDay);
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks when the scene unloads
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DayChanged -= OnDayChanged;
        }
    }

    private void OnDayChanged(int newDay)
    {
        UpdateVisibility(newDay);
    }

    private void UpdateVisibility(int currentDay)
    {
        // gameObject.SetActive automatically hides or shows the icon
        gameObject.SetActive(currentDay >= unlockDay);
    }
}