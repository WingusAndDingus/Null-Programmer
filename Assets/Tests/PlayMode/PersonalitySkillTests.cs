using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R = NullProgrammer.Tests.RuntimeAccess;

namespace NullProgrammer.Tests
{
    public class PersonalitySkillTests
    {
        private const string SaveKey = "NullProgrammer.PersonalitySkills";
        private bool hadSave;
        private bool saveCaptured;
        private string originalSave;
        private GameObject owner;
        private Component skills;
        private ScriptableObject definition;

        [SetUp]
        public void SetUp()
        {
            saveCaptured = false;
            owner = null;
            skills = null;
            definition = null;
            Assert.That(Object.FindAnyObjectByType(R.Type("PersonalitySkills")), Is.Null,
                "Run tests without an existing skill allocation screen.");
            Assert.That(R.Get<object>(R.Type("GameManager"), "Instance"), Is.Null,
                "Run tests from an empty scene without an existing GameManager.");
            hadSave = PlayerPrefs.HasKey(SaveKey);
            originalSave = PlayerPrefs.GetString(SaveKey);
            saveCaptured = true;
            PlayerPrefs.DeleteKey(SaveKey);
            owner = new GameObject("Skill test fixture");
            // The current skill system reads/spends points through GameManager.
            // Create that dependency before the skill component initializes.
            R.Add(owner, "GameManager");
            skills = R.Add(owner, "PersonalitySkills");
            definition = ScriptableObject.CreateInstance(R.Type("PersonalityDefinition"));
            R.Set(definition, "personality", R.Enum("Personality", 0));
            R.Set(definition, "levelUnlocks", new string[4]);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                // Both components belong to this fixture; this also releases
                // GameManager.Instance before the next test runs.
                if (owner != null) Object.Destroy(owner);
                if (definition != null) Object.Destroy(definition);
            }
            finally
            {
                if (saveCaptured)
                {
                    if (hadSave) PlayerPrefs.SetString(SaveKey, originalSave);
                    else PlayerPrefs.DeleteKey(SaveKey);
                    PlayerPrefs.Save();
                    saveCaptured = false;
                }
            }
            yield return null;
        }

        [Test]
        public void DuplicateMilestoneAwardsOnlyOnePoint()
        {
            R.Call(skills, "AwardPoint", "test-milestone");
            R.Call(skills, "AwardPoint", "test-milestone");
            Assert.That(R.Get<int>(skills, "AvailablePoints"), Is.EqualTo(1));
            Assert.That(R.Call(R.Type("PersonalitySkills"), "TryAwardMilestone", "test-milestone"), Is.EqualTo(false));
        }

        [Test]
        public void InvestmentRequiresNightAndStopsAtExpert()
        {
            for (int i = 0; i < 5; i++) R.Call(skills, "AwardPoint", "test-" + i);
            Assert.That(R.Call(skills, "TryInvest", definition), Is.EqualTo(false));
            R.Call(skills, "BeginNightAllocation");
            for (int i = 0; i < 4; i++) Assert.That(R.Call(skills, "TryInvest", definition), Is.EqualTo(true));
            Assert.That(R.Call(skills, "TryInvest", definition), Is.EqualTo(false));
            Assert.That(R.Get<int>(skills, "AvailablePoints"), Is.EqualTo(1));
            Assert.That(R.Call(skills, "GetLevel", R.Enum("Personality", 0)), Is.EqualTo(4));
            Assert.That(R.Call(R.Type("PersonalitySkills"), "GetSavedLevel", R.Enum("Personality", 0)), Is.EqualTo(4));
            R.Call(skills, "EndNightAllocation");
        }
    }
}
