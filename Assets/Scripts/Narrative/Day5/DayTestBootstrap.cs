using UnityEngine;

[DefaultExecutionOrder(-900)]
public class DayTestBootstrap : MonoBehaviour
{
    [Header("Day Testing")]
    [SerializeField] private bool forceDayOnPlay = true;

    [Range(1, 5)]
    [SerializeField] private int testDay = 5;

    [Header("Narrative Step")]
    [SerializeField] private bool resetNarrativeStep = true;
    [SerializeField] private string testStep = "Start";

    private void Awake()
    {
        if (!forceDayOnPlay)
            return;

        GameManager manager = GameManager.EnsureInstance();

        manager.setCurrentDay(testDay);

        if (resetNarrativeStep)
        {
            manager.setStep(
                string.IsNullOrWhiteSpace(testStep)
                    ? "Start"
                    : testStep
            );
        }

        Debug.Log(
            $"[DayTestBootstrap] Forced Day {testDay}, Step '{manager.CurrentStep}'."
        );
    }
}