using UnityEngine;

/// <summary>
/// Optional development-only helper. Put it in the Day 5 test scene and remove/disable it
/// once normal game progression reaches Day 5.
/// </summary>
[DefaultExecutionOrder(-900)]
public class Day5TestBootstrap : MonoBehaviour
{
    [Header("Day 5 Test Setup")]
    [SerializeField] private bool forceDay5OnPlay = true;

    [Tooltip("If enabled, resets the Day 5 narrative to the Test Step when this scene loads.")]
    [SerializeField] private bool resetNarrativeStep = false;

    [Tooltip("Narrative step to use when Reset Narrative Step is enabled.")]
    [SerializeField] private string testStep = "Start";

    private void Awake()
    {
        GameManager manager = GameManager.EnsureInstance();

        if (forceDay5OnPlay)
        {
            manager.setCurrentDay(5);

            if (resetNarrativeStep)
            {
                string step = string.IsNullOrWhiteSpace(testStep)
                    ? "Start"
                    : testStep;

                manager.setStep(step);
            }
        }
    }
}
