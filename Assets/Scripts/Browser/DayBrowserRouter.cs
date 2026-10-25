using System;
using UnityEngine;

[Serializable]
public class DayBrowserUrlRule
{
    public int day;
    public string url;
}

[Serializable]
public class StepBrowserUrlRule
{
    public int day;
    public string stepId;
    public string url;
}

public class DayBrowserRouter : MonoBehaviour
{
    [Header("Default URL for each day")]
    [SerializeField]
    private DayBrowserUrlRule[] dayUrls;

    [Header("Optional CurrentStep URL overrides")]
    [SerializeField]
    private StepBrowserUrlRule[] stepUrls;

    private GameManager gameManager;

    private int lastDay = int.MinValue;
    private string lastStep;

    private bool initialized;

    private void Start()
    {
        gameManager = GameManager.EnsureInstance();

        EvaluateGameState();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.EnsureInstance();
        }

        EvaluateGameState();
    }

    private void EvaluateGameState()
    {
        if (gameManager == null)
        {
            return;
        }

        int currentDay = gameManager.CurrentDay;
        string currentStep = gameManager.CurrentStep ?? "";

        bool dayChanged =
            !initialized ||
            currentDay != lastDay;

        bool stepChanged =
            !initialized ||
            currentStep != lastStep;

        if (!dayChanged && !stepChanged)
        {
            return;
        }

        if (dayChanged)
        {
            // A specific step URL has priority.
            string stepUrl = FindStepUrl(currentDay, currentStep);

            if (!string.IsNullOrWhiteSpace(stepUrl))
            {
                BrowserManager.Instance?.NavigateTo(stepUrl);
            }
            else
            {
                string dayUrl = FindDayUrl(currentDay);

                if (!string.IsNullOrWhiteSpace(dayUrl))
                {
                    BrowserManager.Instance?.NavigateTo(dayUrl);
                }
            }
        }
        else if (stepChanged)
        {
            // Do nothing unless this specific step has an override.
            string stepUrl = FindStepUrl(currentDay, currentStep);

            if (!string.IsNullOrWhiteSpace(stepUrl))
            {
                BrowserManager.Instance?.NavigateTo(stepUrl);
            }
        }

        lastDay = currentDay;
        lastStep = currentStep;
        initialized = true;
    }

    private string FindDayUrl(int day)
    {
        if (dayUrls == null)
        {
            return null;
        }

        foreach (DayBrowserUrlRule rule in dayUrls)
        {
            if (rule != null && rule.day == day)
            {
                return rule.url;
            }
        }

        return null;
    }

    private string FindStepUrl(int day, string stepId)
    {
        if (stepUrls == null)
        {
            return null;
        }

        foreach (StepBrowserUrlRule rule in stepUrls)
        {
            if (rule == null)
            {
                continue;
            }

            if (rule.day == day &&
                string.Equals(
                    rule.stepId,
                    stepId,
                    StringComparison.Ordinal
                ))
            {
                return rule.url;
            }
        }

        return null;
    }
}