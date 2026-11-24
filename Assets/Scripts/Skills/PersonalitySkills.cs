using UnityEngine;
using System;

/// <summary>
/// Public API and night-allocation controller for personality skills.
///
/// Skill progression is intentionally session-only: GameManager owns the current run's
/// unspent points, rewarded milestone IDs, and invested levels. Nothing in this class
/// persists personality progression to PlayerPrefs, so a new game cannot inherit skill
/// state from a previous run.
/// </summary>
public class PersonalitySkills : MonoBehaviour
{
    public const int MaxLevel = (int)PersonalityLevel.Expert;

    // Older builds stored personality progression here. The new run-scoped system never
    // reads it; remove it when encountered so stale development/test data cannot linger.
    private const string LegacySaveKey = "NullProgrammer.PersonalitySkills";

    private bool nightAllocationOpen;

    public int AvailablePoints => GameManager.Instance != null
        ? GameManager.Instance.UnspentSkillPoints
        : 0;

    public event Action Changed;

    private void Awake()
    {
        GameManager.EnsureInstance();

        if (PlayerPrefs.HasKey(LegacySaveKey))
        {
            PlayerPrefs.DeleteKey(LegacySaveKey);
            PlayerPrefs.Save();
            Debug.Log("[PersonalitySkills] Removed legacy persisted personality-skill data.");
        }
    }

    // Kept under the existing public name so dialogue/weekend code does not need a
    // broad refactor. The value now comes from the current GameManager session only.
    public static int GetSavedLevel(Personality personality)
    {
        return GameManager.EnsureInstance().GetPersonalitySkillLevel(personality);
    }

    /// <summary>
    /// Awards a generic point to the current run even when the Night scene (and therefore
    /// no PersonalitySkills component) is loaded. This is the normal daytime reward path.
    /// </summary>
    public static bool TryAwardMilestone(string milestoneID)
    {
        if (string.IsNullOrWhiteSpace(milestoneID)) return false;
        return GameManager.EnsureInstance().TryAwardSkillPoint(milestoneID);
    }

    public int GetLevel(Personality personality)
    {
        return GameManager.EnsureInstance().GetPersonalitySkillLevel(personality);
    }

    public bool HasLevel(Personality personality, int requiredLevel)
    {
        return GetLevel(personality) >= requiredLevel;
    }

    public void AwardPoint(string milestoneID)
    {
        if (TryAwardMilestone(milestoneID))
        {
            Changed?.Invoke();
        }
    }

    public void BeginNightAllocation()
    {
        nightAllocationOpen = true;
        Changed?.Invoke();
    }

    public void EndNightAllocation()
    {
        nightAllocationOpen = false;
    }

    public bool CanInvest(PersonalityDefinition definition)
    {
        return nightAllocationOpen && definition != null && definition.levelUnlocks != null
            && AvailablePoints > 0
            && GetLevel(definition.personality) < MaxLevel
            && GetLevel(definition.personality) < definition.levelUnlocks.Length;
    }

    public bool TryInvest(PersonalityDefinition definition)
    {
        if (!CanInvest(definition))
        {
            return false;
        }

        bool invested = GameManager.EnsureInstance().TryInvestPersonalitySkill(definition.personality);
        if (invested)
        {
            Changed?.Invoke();
        }

        return invested;
    }

    internal void RefreshShowcaseLevels() => Changed?.Invoke();

    [ContextMenu("Reset Skill Progress")]
    private void ResetSkillProgress()
    {
        GameManager.EnsureInstance().ResetPersonalitySkillsForCurrentRun();
        nightAllocationOpen = false;
        Changed?.Invoke();
    }
}
