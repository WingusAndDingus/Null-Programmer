using UnityEngine;
using TMPro;

public class DesktopWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text dayText;

    private void Start()
    {
        if (dayText == null)
        {
            dayText = GetComponent<TMP_Text>();
        }

        RefreshText();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.DayChanged += OnDayChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DayChanged -= OnDayChanged;
        }
    }

    private void OnDayChanged(int newDay)
    {
        RefreshText();
    }

    public void RefreshText()
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
        if (dayText != null)
        {
            dayText.text = GetDayString(currentDay);
        }
    }

    private string GetDayString(int dayNumber)
    {
        return dayNumber switch
        {
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            _ => $"Day {dayNumber}"
        };
    }
}