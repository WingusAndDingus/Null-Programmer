using System;
using UnityEngine;

namespace NullProgrammer.Day2
{
    // Kept on GameManager, so changing monitor scenes does not repeat rewards.
    public sealed class Day2LessonSession : MonoBehaviour
    {
        public readonly Day2LessonProgress progress = new Day2LessonProgress();

        public static Day2LessonSession Get()
        {
            GameManager manager = GameManager.EnsureInstance();
            Day2LessonSession session = manager.GetComponent<Day2LessonSession>();
            return session != null ? session : manager.gameObject.AddComponent<Day2LessonSession>();
        }
    }

    public sealed class Day2LessonProgress
    {
        public readonly bool[] passed = new bool[Day2LessonData.ExerciseCount];
        public readonly bool[] submitted = new bool[Day2LessonData.ExerciseCount];
        public readonly int[] submittedChoice = new int[Day2LessonData.ExerciseCount];
        public readonly int[] quality = new int[Day2LessonData.ExerciseCount];
        public readonly bool[] detectiveUsed = new bool[Day2LessonData.ExerciseCount];
        public readonly int[] attempts = new int[Day2LessonData.ExerciseCount];

        public Day2LessonProgress()
        {
            for (int i = 0; i < submittedChoice.Length; i++) submittedChoice[i] = -1;
        }

        public void RecordTest(int exercise)
        {
            CheckIndex(exercise);
            if (submitted[exercise]) return;
            attempts[exercise]++;
        }

        public bool TrySubmit(int exercise, int choice, out int qualityPoints)
        {
            CheckIndex(exercise);
            if (choice < 0 || choice > 2) throw new ArgumentOutOfRangeException(nameof(choice));
            qualityPoints = 0;
            if (submitted[exercise]) return false;

            // Grade the current selection, even if the player tested a different answer or never ran tests.
            Day2TestEngine.Run result = Day2TestEngine.Evaluate(exercise, choice);
            submitted[exercise] = true;
            submittedChoice[exercise] = choice;
            passed[exercise] = result.passed;
            quality[exercise] = result.qualityPoints;
            qualityPoints = result.qualityPoints;
            return true;
        }

        public int SubmittedCount
        {
            get { int count = 0; foreach (bool value in submitted) if (value) count++; return count; }
        }

        public int PassedCount
        {
            get { int count = 0; foreach (bool value in passed) if (value) count++; return count; }
        }

        public int TotalQuality
        {
            get { int total = 0; foreach (int value in quality) total += value; return total; }
        }

        public bool UseDetective(int exercise)
        {
            CheckIndex(exercise);
            if (detectiveUsed[exercise]) return false;
            detectiveUsed[exercise] = true;
            return true;
        }

        private static void CheckIndex(int exercise)
        {
            if (exercise < 0 || exercise >= Day2LessonData.ExerciseCount)
                throw new ArgumentOutOfRangeException(nameof(exercise));
        }
    }
}
