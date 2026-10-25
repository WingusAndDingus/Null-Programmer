using UnityEngine;
using System;
using System.Collections.Generic;

public class PersonalitySkills : MonoBehaviour
{
    public const int MaxLevel = (int)PersonalityLevel.Expert;

    [Serializable]
    private class SaveData
    {
        public int availablePoints;
        public int[] levels = new int[5];
        public List<string> rewardedMilestones = new List<string>();
    }

    private const string SaveKey = "NullProgrammer.PersonalitySkills";

    private SaveData data;
    private bool nightAllocationOpen;

    // Pull available points directly from the persistent GameManager
    public int AvailablePoints => GameManager.Instance != null ? GameManager.Instance.UnspentSkillPoints : 0;
    public event Action Changed;

    private void Awake()
    {
        data = LoadData();
    }

    private static SaveData LoadData()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        SaveData data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            data = new SaveData();
        }

        if (data.levels == null || data.levels.Length != 5)
        {
            data.levels = new int[5];
        }

        for (int i = 0; i < data.levels.Length; i++)
        {
            data.levels[i] = Mathf.Clamp(data.levels[i], 0, MaxLevel);
        }

        if (data.rewardedMilestones == null)
        {
            data.rewardedMilestones = new List<string>();
        }

        return data;
    }

    // Dialogue can read invested levels even when the Sleep scene is not loaded.
    public static int GetSavedLevel(Personality personality)
    {
        return LoadData().levels[(int)personality];
    }

    public static bool TryAwardMilestone(string milestoneID)
    {
        if (string.IsNullOrWhiteSpace(milestoneID)) return false;
        PersonalitySkills skills = FindAnyObjectByType<PersonalitySkills>();
        if (skills != null)
        {
            if (skills.data.rewardedMilestones.Contains(milestoneID)) return false;
            skills.AwardPoint(milestoneID);
            return true;
        }

        SaveData saved = LoadData();
        if (saved.rewardedMilestones.Contains(milestoneID)) return false;
        saved.rewardedMilestones.Add(milestoneID);
        saved.availablePoints++;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
        return true;
    }

    public int GetLevel(Personality personality)
    {
        return data.levels[(int)personality];
    }

    public bool HasLevel(Personality personality, int requiredLevel)
    {
        return GetLevel(personality) >= requiredLevel;
    }

    public void AwardPoint(string milestoneID)
    {
        if (string.IsNullOrWhiteSpace(milestoneID) || data.rewardedMilestones.Contains(milestoneID))
        {
            return;
        }

        data.rewardedMilestones.Add(milestoneID);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AwardSkillPoint(milestoneID);
        }
        else
        {
            data.availablePoints++;
        }

        Save();
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

        // Deduct from GameManager's persistent pool
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpendSkillPoint();
        }

        data.levels[(int)definition.personality]++;
        Save();
        return true;
    }


    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    [ContextMenu("Reset Skill Progress")]
    private void ResetSkillProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        data = new SaveData();
        nightAllocationOpen = false;
        Changed?.Invoke();
    }
}
