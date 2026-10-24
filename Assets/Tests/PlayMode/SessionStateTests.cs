using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R = NullProgrammer.Tests.RuntimeAccess;

namespace NullProgrammer.Tests
{
    public class SessionStateTests
    {
        private GameObject owner;
        private Component manager;

        [SetUp]
        public void SetUp()
        {
            Assert.That(R.Get<object>(R.Type("GameManager"), "Instance"), Is.Null,
                "Run Play Mode tests from an empty scene, without an existing game session.");
            owner = new GameObject("GameManager test fixture");
            manager = R.Add(owner, "GameManager");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (owner != null) Object.Destroy(owner);
            yield return null;
        }

        [Test]
        public void SuspicionClampsAndCrisisOnlyFiresWhenCrossingMaximum()
        {
            int changes = 0, crises = 0;
            System.Action<int> changed = value => changes++;
            System.Action crisis = () => crises++;
            manager.GetType().GetEvent("SuspicionChanged").AddEventHandler(manager, changed);
            manager.GetType().GetEvent("SteveCrisis").AddEventHandler(manager, crisis);
            R.Call(manager, "ChangeSteveSuspicion", int.MaxValue);
            Assert.That(R.Get<int>(manager, "SteveSuspicion"), Is.EqualTo(100));
            R.Call(manager, "ChangeSteveSuspicion", 10);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(crises, Is.EqualTo(1));
            R.Call(manager, "ChangeSteveSuspicion", int.MinValue);
            Assert.That(R.Get<int>(manager, "SteveSuspicion"), Is.Zero);
            R.Call(manager, "ChangeSteveSuspicion", 100);
            Assert.That(crises, Is.EqualTo(2));
        }

        [Test]
        public void NextDayResetsNarrativeToStart()
        {
            R.Call(manager, "setCurrentDay", 2);
            R.Call(manager, "setStep", "EndOfDay");
            R.Call(manager, "AdvanceToNextDay");
            Assert.That(R.Get<int>(manager, "CurrentDay"), Is.EqualTo(3));
            Assert.That(R.Get<string>(manager, "CurrentStep"), Is.EqualTo("Start"));
        }

        [Test]
        public void EmailReplyRequiresReadAndCloseAndRejectsDuplicateStage()
        {
            Assert.That(R.Call(manager, "TryApplyReply", 0, 5, "early"), Is.EqualTo(false));
            R.Call(manager, "MarkReadEmailClosed");
            Assert.That(R.Get<bool>(manager, "ReplyAvailable"), Is.False);
            R.Call(manager, "MarkEmailRead");
            R.Call(manager, "MarkReadEmailClosed");
            Assert.That(R.Call(manager, "TryApplyReply", 0, 5, "first"), Is.EqualTo(true));
            Assert.That(R.Call(manager, "TryApplyReply", 0, 5, "duplicate"), Is.EqualTo(false));
            Assert.That(R.Get<int>(manager, "SteveSuspicion"), Is.EqualTo(5));
            Assert.That(R.Call(manager, "TryApplyReply", 1, 2, "done"), Is.EqualTo(true));
            Assert.That(R.Get<bool>(manager, "ReplyCompleted"), Is.True);
            Assert.That(R.Get<bool>(manager, "ReplyAvailable"), Is.False);
        }

        [Test]
        public void WindowStateIsIndependentAndBringingForwardUpdatesOrder()
        {
            R.Call(manager, "SetWindowOpen", "ide", false);
            R.Call(manager, "SetWindowMaximized", "mail", true);
            Assert.That(R.Call(manager, "IsWindowOpen", "mail"), Is.EqualTo(true));
            Assert.That(R.Call(manager, "IsWindowMaximized", "ide"), Is.EqualTo(false));
            int first = (int)R.Call(manager, "GetOrCreateWindowStackOrder", "ide");
            int second = (int)R.Call(manager, "GetOrCreateWindowStackOrder", "mail");
            Assert.That(second, Is.GreaterThan(first));
            R.Call(manager, "BringWindowToFront", "ide");
            Assert.That((int)R.Call(manager, "GetOrCreateWindowStackOrder", "ide"), Is.GreaterThan(second));
        }
    }
}
