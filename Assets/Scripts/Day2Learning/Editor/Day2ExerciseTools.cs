using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NullProgrammer.Day2.Editor
{
    public static class Day2ExerciseTools
    {
        [MenuItem("Tools/Null Programmer/Validate Day 2 Exercises")]
        public static void Validate()
        {
            Require(Day2LessonData.Exercises.Length == 4, "Expected four exercises.");
            int[][] expectedQuality =
            {
                new[] { 0, 5, 0 }, new[] { 0, 2, 5 },
                new[] { 0, 5, 2 }, new[] { 0, 0, 5 }
            };
            int variants = 0;
            for (int exercise = 0; exercise < 4; exercise++)
            {
                Day2Exercise data = Day2LessonData.Exercises[exercise];
                Require(data.replacements.Length == 3 && data.explanations.Length == 3, "Incomplete answers or feedback.");
                Require(data.hints.Length == 3, "Every exercise needs three personality hint sets.");
                foreach (string[] ranks in data.hints)
                {
                    Require(ranks.Length == 5, "A personality hint set is missing ranks.");
                    foreach (string text in ranks) Require(!string.IsNullOrWhiteSpace(text), "Empty hint.");
                }
                int passing = 0;
                for (int answer = 0; answer < 3; answer++)
                {
                    Day2TestEngine.Run run = Day2TestEngine.Evaluate(exercise, answer);
                    Require(run.passed == (answer == data.correctIndex), "Incorrect grading: exercise " + exercise + ", answer " + answer);
                    Require(run.qualityPoints == expectedQuality[exercise][answer], "Incorrect submission quality.");
                    Require(!string.IsNullOrWhiteSpace(run.output), "Missing test output.");
                    Require(run.expected.Length > 0 && run.observed.Length > 0, "Missing preview cells.");
                    if (run.passed) passing++;
                    var submission = new Day2LessonProgress();
                    int points;
                    Require(submission.TrySubmit(exercise, answer, out points), "Untested selection could not be submitted.");
                    Require(points == expectedQuality[exercise][answer] && submission.submittedChoice[exercise] == answer,
                        "Submission did not grade or retain the selected code.");
                    Require(submission.submitted[exercise] && submission.passed[exercise] == run.passed,
                        "A failing submission must still complete the exercise.");
                    Require(!submission.TrySubmit(exercise, (answer + 1) % 3, out points) && points == 0,
                        "Repeated submission granted points or changed the committed answer.");
                    Require(submission.quality[exercise] == expectedQuality[exercise][answer] && submission.submittedChoice[exercise] == answer,
                        "Repeated submission overwrote the original result.");
                    variants++;
                }
                Require(passing == 1, "Exercise must have exactly one passing correction.");
            }

            // The common square-only shortcuts must fail on the rectangular cases.
            Require(!Day2TestEngine.Evaluate(1, 1).passed, "Loop tests missed the reused row bound.");
            Require(!Day2TestEngine.Evaluate(2, 2).passed, "Resize tests missed the forced square.");
            Require(Day2TestEngine.Evaluate(0, 1).expected[0].row == 2 &&
                Day2TestEngine.Evaluate(0, 1).expected[0].col == 6, "Coordinate reference changed.");
            Require(Day2TestEngine.Evaluate(1, 0).observed.Length == 81, "Inclusive loops should generate 81 cells for 8 by 8.");
            Require(Day2TestEngine.Evaluate(2, 0).observed.Length == 64, "Hardcoded resize should still draw 64 cells.");

            var progress = new Day2LessonProgress();
            Require(Day2TestEngine.Evaluate(0, 1).passed, "Correct pre-submission test should pass.");
            progress.RecordTest(0);
            progress.RecordTest(0);
            Require(progress.attempts[0] == 2, "Test runs not recorded.");
            Require(progress.SubmittedCount == 0 && progress.TotalQuality == 0 && !progress.passed[0],
                "Running tests committed the answer or changed earned quality.");
            int quality;
            Require(progress.TrySubmit(0, 0, out quality) && quality == 0 && !progress.passed[0],
                "Testing the correct answer incorrectly improved a later wrong submission.");
            Require(progress.submittedChoice[0] == 0 && progress.SubmittedCount == 1,
                "Wrong selection was not committed.");
            progress.RecordTest(0);
            Require(progress.attempts[0] == 2, "Committed work should not accept further test runs.");
            Require(progress.UseDetective(0) && !progress.UseDetective(0), "Detective reassurance can be farmed.");
            Require(progress.UseDetective(1), "A different exercise should have its own reassurance allowance.");

            DaySequenceData day = AssetDatabase.LoadAssetAtPath<DaySequenceData>("Assets/Scripts/Day Scripts/Day2_Data.asset");
            Require(day != null, "Day 2 narrative asset missing.");
            var indices = new HashSet<int>();
            foreach (NarrativeStep step in day.steps)
                if (step.stepType == NarrativeStepType.Day2Exercise) indices.Add(step.exerciseIndex);
            Require(indices.SetEquals(new[] { 0, 1, 2, 3 }), "Narrative must include all four exercises.");
            Debug.Log("Day 2 exercise validation passed: all " + variants +
                " answer grades, untested/failing submissions, noncommitting tests, immutable submissions, 60 ranked hints, and narrative integration.");
        }

        [MenuItem("Tools/Null Programmer/Create Day 2 Exercise Test Scene")]
        public static void CreateTestScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before creating the test scene.");
                return;
            }
            Scene original = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Day2_Exercises_Test.unity");
            try
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.045f, .065f, .095f);
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var runner = new GameObject("Day 2 Exercise Playtest", typeof(Day2ExercisePlaytest));
                SceneManager.MoveGameObjectToScene(runner, scene);
                Require(EditorSceneManager.SaveScene(scene, path), "Could not save the exercise test scene.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            Debug.Log("Created " + path + ". Open that scene by itself and press Play to test the exercises without the Day 1/Zoom narrative.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
