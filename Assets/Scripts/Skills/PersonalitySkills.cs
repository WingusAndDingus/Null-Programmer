using UnityEngine;
using System;
using System.Collections.Generic;

public class PersonalitySkills : MonoBehaviour
{
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

    public int AvailablePoints => data.availablePoints;
    public event Action Changed;

    private void Awake()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            data = new SaveData();
        }

        if (data.levels == null || data.levels.Length != 5)
        {
            data.levels = new int[5];
        }

        if (data.rewardedMilestones == null)
        {
            data.rewardedMilestones = new List<string>();
        }

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
        data.availablePoints++;
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
            && data.availablePoints > 0 
            && GetLevel(definition.personality) <= definition.levelUnlocks.Length;
    }

    public bool TryInvest(PersonalityDefinition definition)
    {
        if (!CanInvest(definition))
        {
            return false;
        }

        data.availablePoints--;
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
