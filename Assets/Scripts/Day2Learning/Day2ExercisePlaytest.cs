using System;
using System.Threading;
using UnityEngine;

namespace NullProgrammer.Day2
{
    // Used only by the isolated exercise test scene generated from the Tools menu.
    public sealed class Day2ExercisePlaytest : MonoBehaviour
    {
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool complete;

        private async void Start()
        {
            GameManager.EnsureInstance().setCurrentDay(2);
            try
            {
                for (int i = 0; i < Day2LessonData.ExerciseCount; i++)
                    await Day2ExerciseWindow.RunAsync(i, lifetime.Token, false);
                complete = true;
                Debug.Log("Day 2 playtest complete: all four exercises submitted. Exit Play Mode to return to the editor.");
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private void OnGUI()
        {
            if (!complete) return;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                wordWrap = true
            };
            style.normal.textColor = Color.white;
            Day2LessonProgress progress = Day2LessonSession.Get().progress;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                "Submitted: " + progress.SubmittedCount + " / 4   |   Fully passed: " + progress.PassedCount +
                " / 4\nSubmission quality: " + progress.TotalQuality + " / 20 (playtest rewards disabled)" +
                "\nExit Play Mode to return to the editor.", style);
        }
    }
}
