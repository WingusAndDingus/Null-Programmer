using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class NightSkillScreen : MonoBehaviour
{
    [Serializable]
    public class PersonalityCard
    {
        public PersonalityDefinition definition;
        public TMP_Text nameText;
        public TMP_Text levelText;
        public TMP_Text nextUnlockText;
        public Button investButton;
    }

    [SerializeField] private PersonalitySkills skills;
    [SerializeField] private TMP_Text availablePointsText;
    [SerializeField] private PersonalityCard[] cards;

    
    private void Start()
    {
        OpenNight();
    }
    private void Awake()
    {
        foreach (PersonalityCard card in cards)
        {
            if (card.definition == null || card.investButton == null)
            {
                continue;
            }
            PersonalityDefinition chosen = card.definition;
            card.investButton.onClick.AddListener(() => skills.TryInvest(chosen));
        }
    }

    private void OnEnable()
    {
        skills.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        skills.Changed -= Refresh;
        skills.EndNightAllocation();
    }

    public void OpenNight()
    {
        skills.BeginNightAllocation();
        gameObject.SetActive(true);
    }

    public void CloseNight()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        availablePointsText.text = $"Available Points: {skills.AvailablePoints}";

        foreach (PersonalityCard card in cards)
        {
            if (card.definition == null)
            {
                continue;
            }

            int level = skills.GetLevel(card.definition.personality);
            int maxLevel = card.definition.levelUnlocks.Length;

            card.nameText.text = card.definition.displayName;
            card.levelText.text = $"Level {level}/{maxLevel}";
            card.nextUnlockText.text = level < maxLevel
                ? card.definition.levelUnlocks[level]
                : "Fully Unlocked";

            card.investButton.interactable = skills.CanInvest(card.definition);
        }
        
    }
    
    [SerializeField] private string nextSceneName = "StartScreen";

    public void WakeUp()
    {
        CloseNight();
        SceneManager.LoadScene(nextSceneName);
    }
    
}
