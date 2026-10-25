using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R = NullProgrammer.Tests.RuntimeAccess;

namespace NullProgrammer.Tests
{
    public class DialoguePlaybackTests
    {
        private GameObject owner;
        private Component dialogue;

        [SetUp]
        public void SetUp()
        {
            Assert.That(R.Get<object>(R.Type("DialogueManager"), "Instance"), Is.Null,
                "Run tests from an empty scene without a Dialogue Canvas.");
            owner = new GameObject("Dialogue test fixture");
            dialogue = R.Add(owner, "DialogueManager");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (owner != null) Object.Destroy(owner);
            // The baseline manager has no OnDestroy singleton reset.
            R.Set(R.Type("DialogueManager"), "Instance", null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimedLineRevealsTextAndFinishes()
        {
            object line = R.New("DialogueLine");
            R.Set(line, "text", "Test");
            R.Set(line, "displayDuration", 0.05f);
            R.Call(dialogue, "DisplayLine", line);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (R.Get<object>(dialogue, "activeDialogueCoroutine") != null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(R.Get<object>(dialogue, "activeDialogueCoroutine"), Is.Null, "Line did not finish.");
            Assert.That(R.Get<string>(dialogue, "CurrentText"), Is.EqualTo("Test"));
            Assert.That(R.Get<bool>(dialogue, "IsTypingComplete"), Is.True);
        }

        [Test]
        public void HidingDialogueStopsActivePlayback()
        {
            object line = R.New("DialogueLine");
            R.Set(line, "text", new string('x', 300));
            R.Call(dialogue, "DisplayLine", line);
            R.Call(dialogue, "HideDialogueUI");
            Assert.That(R.Get<object>(dialogue, "activeDialogueCoroutine"), Is.Null);
            Assert.That(R.Get<object>(dialogue, "currentPlayingLine"), Is.Null);
        }
    }
}
