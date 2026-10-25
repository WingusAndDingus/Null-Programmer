using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using R = NullProgrammer.Tests.RuntimeAccess;

namespace NullProgrammer.Tests
{
    public class NarrativeDataTests
    {
        [Test]
        public void EveryAuthoredDayHasUniqueIdsAndValidTargets()
        {
            string[] guids = AssetDatabase.FindAssets("t:DaySequenceData");
            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(3));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var day = AssetDatabase.LoadAssetAtPath(path, R.Type("DaySequenceData"));
                var steps = R.Get<IList>(day, "steps");
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (object step in steps)
                {
                    string id = R.Get<string>(step, "stepID");
                    Assert.That(id, Is.Not.Null.And.Not.Empty, path);
                    Assert.That(ids.Add(id), Is.True, path + ": duplicate " + id);
                }
                Assert.That(ids, Does.Contain("Start"), path);
                foreach (object step in steps)
                {
                    string next = R.Get<string>(step, "nextStepID");
                    if (!string.IsNullOrEmpty(next)) Assert.That(ids, Does.Contain(next), path);
                    foreach (object choice in R.Get<IList>(step, "choices"))
                    {
                        string target = R.Get<string>(choice, "targetStepOnClick");
                        Assert.That(target, Is.Not.Null.And.Not.Empty, path);
                        Assert.That(ids, Does.Contain(target), path);
                    }
                }
            }
        }

        [Test]
        public void Day2SelectsOneHighestUnlockedChoicePerPersonalityForEveryProfile()
        {
            // Existing validator checks all four decisions at all 125 skill profiles.
            R.Call(R.Type("Day2SkillDialogueChecks"), "Validate");
        }

        [Test]
        public void AuthoredChoiceTextFitsAndCardsDoNotOverlapAtSupportedResolutions()
        {
            R.Call(R.Type("DialogueBubbleChecks"), "Validate");
        }

        [Test]
        public void ChoiceFilterHandlesNullAndKeepsIndependentChoices()
        {
            var filter = R.Type("NarrativeChoiceFilter");
            var callbackType = typeof(Func<,>).MakeGenericType(R.Type("HeadPersonality"), typeof(int));
            Delegate getLevel = Expression.Lambda(callbackType, Expression.Constant(0),
                Expression.Parameter(R.Type("HeadPersonality"), "personality")).Compile();
            Assert.That((IList)R.Call(filter, "GetAvailableChoices", null, getLevel), Is.Empty);
            var choices = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(R.Type("DialogueStepChoice")));
            choices.Add(R.New("DialogueStepChoice"));
            choices.Add(R.New("DialogueStepChoice"));
            var result = (IList)R.Call(filter, "GetAvailableChoices", choices, getLevel);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.SameAs(choices[0]));
            Assert.That(result[1], Is.SameAs(choices[1]));
        }

    }

    public class Day2ExerciseTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void OnlyCorrectAlternativePassesAllChecks(int exercise, int correct)
        {
            for (int choice = 0; choice < 3; choice++)
            {
                object run = R.Call(R.Type("NullProgrammer.Day2.Day2TestEngine"), "Evaluate", exercise, choice);
                Assert.That(R.Get<bool>(run, "passed"), Is.EqualTo(choice == correct), "choice " + choice);
                if (choice == correct) Assert.That(R.Get<int>(run, "qualityPoints"), Is.EqualTo(5));
                Assert.That(R.Get<string>(run, "output"), Is.Not.Empty);
            }
        }

        [TestCase(-1, 0)]
        [TestCase(4, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 3)]
        public void InvalidExerciseOrChoiceIsRejected(int exercise, int choice)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                R.Call(R.Type("NullProgrammer.Day2.Day2TestEngine"), "Evaluate", exercise, choice));
        }

        [Test]
        public void SubmitGradesCurrentSelectionAndCannotAwardTwice()
        {
            object progress = R.New("NullProgrammer.Day2.Day2LessonProgress");
            R.Call(progress, "RecordTest", 0);
            object[] first = { 0, 1, 0 };
            Assert.That(R.Call(progress, "TrySubmit", first), Is.EqualTo(true));
            Assert.That(first[2], Is.EqualTo(5));
            object[] duplicate = { 0, 0, 0 };
            Assert.That(R.Call(progress, "TrySubmit", duplicate), Is.EqualTo(false));
            Assert.That(duplicate[2], Is.EqualTo(0));
            R.Call(progress, "RecordTest", 0);
            Assert.That(R.Get<int[]>(progress, "attempts")[0], Is.EqualTo(1));
            Assert.That(R.Get<int>(progress, "SubmittedCount"), Is.EqualTo(1));
            Assert.That(R.Get<int>(progress, "TotalQuality"), Is.EqualTo(5));
            Assert.That(R.Get<int[]>(progress, "submittedChoice")[0], Is.EqualTo(1));
        }

        [Test]
        public void SubmitWithoutRunningTestsStillGradesWrongAnswer()
        {
            object progress = R.New("NullProgrammer.Day2.Day2LessonProgress");
            R.Call(progress, "TrySubmit", new object[] { 0, 0, 0 });
            Assert.That(R.Get<int>(progress, "PassedCount"), Is.Zero);
            Assert.That(R.Get<int>(progress, "TotalQuality"), Is.Zero);
            Assert.That(R.Call(progress, "UseDetective", 0), Is.EqualTo(true));
            Assert.That(R.Call(progress, "UseDetective", 0), Is.EqualTo(false));
        }
    }

    public class BoardModelTests
    {
        [TestCase(-1, 0, 2, true, "Bounds")]
        [TestCase(0, 10, 2, true, "Bounds")]
        [TestCase(0, 0, 0, true, "InvalidLength")]
        [TestCase(0, 9, 2, true, "Bounds")]
        [TestCase(9, 0, 2, false, "Bounds")]
        [TestCase(0, 0, 10, true, "Placed")]
        [TestCase(0, 0, 10, false, "Placed")]
        public void PlacementHonorsEdgesAndOrientation(int row, int col, int length, bool horizontal, string expected)
        {
            object board = R.New("NullProgrammer.Day3.Day3BoardModel");
            Assert.That(R.Call(board, "Place", row, col, length, horizontal).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void FailedPlacementDoesNotPartiallyWriteCells()
        {
            object board = R.New("NullProgrammer.Day3.Day3BoardModel");
            R.Call(board, "Place", 4, 4, 3, true);
            Assert.That(R.Call(board, "Place", 2, 5, 5, false).ToString(), Is.EqualTo("Overlap"));
            Assert.That(R.Call(board, "At", 2, 5), Is.EqualTo(0));
            Assert.That(R.Call(board, "At", 3, 5), Is.EqualTo(0));
            Assert.That(R.Call(board, "At", 4, 5), Is.EqualTo(R.Call(board, "At", 4, 4)));
        }
    }

    public class GameplayWaitTests
    {
        [UnityTest]
        public IEnumerator CompilationWaitReturnsActualPayload()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                try
                {
                    var task = (Task<string>)R.Call(R.Type("NarrativeWaitHelpers"),
                        "WaitForCodeCompilation", cancellation.Token);
                    Assert.That(task.IsCompleted, Is.False);
                    R.Call(R.Type("GameplayEvents"), "TriggerCodeCompiled", "Tile.cpp");
                    yield return WaitUntilCompleted(task, "Compile event did not resolve its waiter.");
                    Assert.That(task.IsCanceled, Is.False);
                    Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
                    Assert.That(task.GetAwaiter().GetResult(), Is.EqualTo("Tile.cpp"));
                }
                finally { cancellation.Cancel(); }
            }
        }

        [UnityTest]
        public IEnumerator CancelledZoomWaitDoesNotConsumeFutureEvents()
            => CheckCancelledWait("WaitForZoomCall", "TriggerZoomCallStarted");

        [UnityTest]
        public IEnumerator CancelledScreenShareWaitDoesNotConsumeFutureEvents()
            => CheckCancelledWait("WaitForScreenShareEnd", "TriggerScreenShareEnded");

        private static IEnumerator CheckCancelledWait(string waitMethod, string eventMethod)
        {
            using (var cancellation = new CancellationTokenSource())
            using (var nextCancellation = new CancellationTokenSource())
            {
                try
                {
                    var cancelled = (Task)R.Call(R.Type("NarrativeWaitHelpers"), waitMethod, cancellation.Token);
                    Assert.That(cancelled.IsCompleted, Is.False);
                    cancellation.Cancel();
                    // Cancellation requests completion; the async method's continuation
                    // and cleanup may run on a later Unity Editor update.
                    yield return WaitUntilCompleted(cancelled, waitMethod + " did not finish after cancellation.");
                    Assert.That(cancelled.IsCanceled, Is.True);

                    var next = (Task)R.Call(R.Type("NarrativeWaitHelpers"), waitMethod, nextCancellation.Token);
                    Assert.That(next.IsCompleted, Is.False, "A new waiter must wait for a new event.");
                    R.Call(R.Type("GameplayEvents"), eventMethod);
                    yield return WaitUntilCompleted(next, waitMethod + " did not receive the next event.");
                    Assert.That(next.IsCanceled, Is.False);
                    Assert.That(next.IsFaulted, Is.False, next.Exception?.ToString());
                }
                finally
                {
                    // Release event subscriptions even if an assertion fails.
                    cancellation.Cancel();
                    nextCancellation.Cancel();
                }
            }
        }

        private static IEnumerator WaitUntilCompleted(Task task, string failureMessage)
        {
            double deadline = EditorApplication.timeSinceStartup + 2.0;
            while (!task.IsCompleted && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(task.IsCompleted, Is.True, failureMessage);
        }
    }
}
