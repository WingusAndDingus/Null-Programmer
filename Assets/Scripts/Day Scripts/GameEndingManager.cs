using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameEndingType
{
    ObsoleteAndDeprecated,    // Worst ending: Steve upstages you, fired immediately.
    ObsoleteSoftRelaunch,     // Moderate/bad ending: Fired next day via email, Steve had a better project.
    ImposterProtocol,         // Bad code, but spent weekend with Steve -> he fixes it, tenuous future.
    PerfectionistTrap,        // High code quality + study alone -> amazing project, empty/lonely life.
    UltimateMentor,           // High code quality + weekend with Steve -> shared credit, best friends.
    ArchitectRedemption,      // High code quality + spent time with family/friends -> healthy & balanced.
    SegmentationFaultOfSoul   // Secret ending: Maxed detective level & all clues found.
}

public static class GameEndingEvaluator
{
    // Configure how many total clues exist in the game for the secret ending
    public const int TotalCluesAvailable = 5;

    public static GameEndingType EvaluateEnding()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[GameEndingEvaluator] GameManager instance is null! Defaulting to ObsoleteAndDeprecated.");
            return GameEndingType.ObsoleteAndDeprecated;
        }

        // 1. Check Secret Ending Condition First: Maxed Detective Level & All Clues Found
        int detectiveLevel = PersonalitySkills.GetSavedLevel(Personality.Detective); // Adjust enum/class reference if your project uses a different structure
        if (detectiveLevel >= PersonalitySkills.MaxLevel && gm.DiscoveredCluesCount >= TotalCluesAvailable)
        {
            return GameEndingType.SegmentationFaultOfSoul;
        }

        // 2. High Code Quality Endings
        bool hasHighCodeQuality = gm.CodeQuality >= 70; // Adjust threshold based on your balance design
        bool hasModerateCodeQuality = gm.CodeQuality >= 40 && gm.CodeQuality < 70;
        bool hasLowCodeQuality = gm.CodeQuality < 40;

        bool steveSuspicionHigh = gm.SteveSuspicion >= 70;

        if (hasHighCodeQuality)
        {
            if (gm.SelectedWeekendChoice == WeekendChoice.Steve)
            {
                return GameEndingType.UltimateMentor;
            }
            else if (gm.SelectedWeekendChoice == WeekendChoice.Study)
            {
                return GameEndingType.PerfectionistTrap;
            }
            else if (gm.SelectedWeekendChoice == WeekendChoice.PersonalConnections)
            {
                return GameEndingType.ArchitectRedemption;
            }
            else
            {
                // Default high quality fallback
                return GameEndingType.ArchitectRedemption;
            }
        }

        // 3. Low/Moderate Code Quality & High Suspicion Endings
        if (steveSuspicionHigh)
        {
            if (hasLowCodeQuality && gm.SelectedWeekendChoice == WeekendChoice.Steve)
            {
                return GameEndingType.ImposterProtocol;
            }
            else if (hasLowCodeQuality)
            {
                return GameEndingType.ObsoleteAndDeprecated;
            }
            else if (hasModerateCodeQuality)
            {
                return GameEndingType.ObsoleteSoftRelaunch;
            }
        }

        // 4. Fallback default ending if criteria don't match specific extremes
        return gm.SelectedWeekendChoice == WeekendChoice.Steve ? GameEndingType.ImposterProtocol : GameEndingType.ObsoleteSoftRelaunch;
    }

    public static void DetermineAndTriggerEnding()
    {
        GameEndingType ending = EvaluateEnding();
        Debug.Log($"[GameEndingEvaluator] Final Ending Decided: {ending}");

        // Map each ending type to its corresponding Unity Scene name
        switch (ending)
        {
            case GameEndingType.ObsoleteAndDeprecated:
                SceneManager.LoadScene("Ending_ObsoleteAndDeprecated");
                break;
            case GameEndingType.ObsoleteSoftRelaunch:
                SceneManager.LoadScene("Ending_ObsoleteSoftRelaunch");
                break;
            case GameEndingType.ImposterProtocol:
                SceneManager.LoadScene("Ending_ImposterProtocol");
                break;
            case GameEndingType.PerfectionistTrap:
                SceneManager.LoadScene("Ending_PerfectionistTrap");
                break;
            case GameEndingType.UltimateMentor:
                SceneManager.LoadScene("Ending_UltimateMentor");
                break;
            case GameEndingType.ArchitectRedemption:
                SceneManager.LoadScene("Ending_ArchitectRedemption");
                break;
            case GameEndingType.SegmentationFaultOfSoul:
                SceneManager.LoadScene("Ending_SecretSegmentationFault");
                break;
        }
    }
}
