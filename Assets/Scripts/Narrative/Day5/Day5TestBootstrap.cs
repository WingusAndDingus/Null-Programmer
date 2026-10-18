using UnityEngine;

/// <summary>
/// Optional testing helper. Add it only while testing Day 5 before earlier days are
/// connected. It runs after GameManager but before the normal narrative executor.
/// Disable or remove it in the final progression build.
/// </summary>
[DefaultExecutionOrder(-900)]
public class Day5TestBootstrap : MonoBehaviour
{
    [Header("Day / Step")]
    [SerializeField] private bool forceDay5OnPlay = true;
    [SerializeField] private bool overrideNarrativeStep = true;

    [SerializeField] private string testStep = "Start";

    [Header("Optional Personality Test Values")]
    [Tooltip("Adds these points at Play start so you can test the high-skill Day 5 dialogue variants without playing Days 1-4 first.")]
    [Min(0)] [SerializeField] private int grandArchitectTestPoints = 0;
    [Min(0)] [SerializeField] private int sentinelTestPoints = 0;

    private void Awake()
    {
        if (!forceDay5OnPlay) return;

        GameManager gm = GameManager.EnsureInstance();
        gm.setCurrentDay(5);

        if (overrideNarrativeStep)
        {
            GameManager.Instance.setStep(testStep);
        }

        if (grandArchitectTestPoints > 0)
        {
            gm.ChangeGrandArchitectStat(grandArchitectTestPoints);
        }

        if (sentinelTestPoints > 0)
        {
            gm.ChangeSentinelStat(sentinelTestPoints);
        }
    }
}
