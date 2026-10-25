using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using NullProgrammer.Tests;

public sealed class Day3WorkshopTests
{
    private const string Simulation = "NullProgrammer.Day3Workshop.BoardSimulation";

    private static bool Pass(int stage, int[] patches)
    {
        object checks = RuntimeAccess.Call(
            RuntimeAccess.Type(Simulation), "CheckPatch", stage, patches);

        return (bool)RuntimeAccess.Call(
            RuntimeAccess.Type(Simulation), "Passed", checks);
    }

    [Test]
    public void EachRepairChangesTheSimulatedBehavior()
    {
        for (int stage = 0; stage < 4; stage++)
        {
            for (int choice = 0; choice < 3; choice++)
            {
                int[] patches = { 1, 1, 1, 1 };
                patches[stage] = choice;

                Assert.That(
                    Pass(stage, patches),
                    Is.EqualTo(choice == 1),
                    "stage " + stage + ", patch " + choice);
            }
        }
    }

    [Test]
    public void FinalSuiteDetectsEverySingleRegression()
    {
        Assert.That(Pass(4, new[] { 1, 1, 1, 1 }), Is.True);

        for (int stage = 0; stage < 4; stage++)
        {
            foreach (int bug in new[] { 0, 2 })
            {
                int[] patches = { 1, 1, 1, 1 };
                patches[stage] = bug;

                Assert.That(Pass(4, patches), Is.False);
            }
        }
    }

    [Test]
    public void RejectedOverlapPreservesAllCells()
    {
        object board = Activator.CreateInstance(
            RuntimeAccess.Type(Simulation),
            new object[] { new[] { 1, 1, 1, 1 } });

        RuntimeAccess.Call(board, "Seed");

        var before = (bool[,])RuntimeAccess.Call(board, "Snapshot");
        object result = RuntimeAccess.Call(board, "Place", 2, 1, 3, false);

        Assert.That(RuntimeAccess.Get<bool>(result, "accepted"), Is.False);

        var after = (bool[,])RuntimeAccess.Call(board, "Snapshot");

        CollectionAssert.AreEqual(before, after);
    }

    [Test]
    public void WednesdayContainsEveryWorkshopAndBothVoices()
    {
        object day = AssetDatabase.LoadAssetAtPath(
            "Assets/Scripts/Day3BoardWorkshop/Day3_BoardWorkshop.asset",
            RuntimeAccess.Type("DaySequenceData"));

        Assert.That(day, Is.Not.Null);

        var indices = new System.Collections.Generic.HashSet<int>();
        var voices = new System.Collections.Generic.HashSet<string>();

        foreach (object step in RuntimeAccess.Get<IEnumerable>(day, "steps"))
        {
            if (RuntimeAccess.Get<object>(step, "stepType").ToString() == "Day3BoardExercise")
            {
                indices.Add(RuntimeAccess.Get<int>(step, "exerciseIndex"));
            }

            voices.Add(RuntimeAccess.Get<object>(step, "personality").ToString());
        }

        CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, indices);

        Assert.That(
            voices.Contains("GrandArchitect") && voices.Contains("Sentinel"),
            Is.True);
    }
}