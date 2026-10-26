using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NullProgrammer.Day3Workshop
{
    public sealed class WorkshopSession : MonoBehaviour
    {
        public readonly int[] patches = { 0, 0, 0, 0 };
        public readonly bool[] submitted = new bool[5];
        public readonly bool[] discovered = new bool[4];
        public bool emailRead;
        public bool previewMode;
        private TaskCompletionSource<bool> pending;
        private int pendingExercise = -1;
        private WorkshopWindow view;
        public static WorkshopSession Get()
        {
            var gm = GameManager.EnsureInstance();
            var session = gm.GetComponent<WorkshopSession>();
            return session != null ? session : gm.gameObject.AddComponent<WorkshopSession>();
        }
        public static bool RouteIDE(GameObject root)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentDay != 3 || root == null ||
                root.name.IndexOf("IDE", StringComparison.OrdinalIgnoreCase) < 0) return false;
            var session = Get();
            if (session.pending != null) session.Open();
            else Debug.Log("Wednesday: finish the current conversation before opening the board workshop.");
            return true;
        }
        public static async Task RunAsync(int exercise, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (exercise < 0 || exercise > 4) throw new ArgumentOutOfRangeException(nameof(exercise));
            var session = Get();
            if (session.submitted[exercise]) return;
            if (session.pending != null) throw new InvalidOperationException("Another board exercise is waiting.");
            session.pendingExercise = exercise;
            var completion = new TaskCompletionSource<bool>();
            session.pending = completion;
            Debug.Log("Wednesday: click the desktop IDE icon to test " + WorkshopLesson.Titles[exercise]);
            try
            {
                using (token.Register(() => completion.TrySetCanceled())) await completion.Task;
            }
            finally
            {
                if (session.view != null) session.view.Hide();
                session.pending = null;
                session.pendingExercise = -1;
            }
        }
        private void Open()
        {
            if (view == null)
            {
                var prefab = Resources.Load<GameObject>("Day3BoardWorkshop");
                if (prefab != null) view = Instantiate(prefab, transform).GetComponent<WorkshopWindow>();
                else view = gameObject.AddComponent<WorkshopWindow>();
            }
            view.Show(this, pendingExercise);
        }
        public bool Submit(int exercise)
        {
            if (pending == null || pendingExercise != exercise || submitted[exercise]) return false;
            if (!BoardSimulation.Passed(BoardSimulation.CheckPatch(exercise, patches))) return false;
            submitted[exercise] = true;
            if (!previewMode)
            {
                GameManager.EnsureInstance().ChangeCodeQualityStat(exercise == 4 ? 5 : 3);
                if (exercise == 0 || exercise == 2)
                    PersonalitySkills.TryAwardMilestone("Day3.Board.Repair." + exercise);
            }
            if (view != null) view.Hide();
            GameplayEvents.TriggerNarrativeEvent(WorkshopLesson.Event(exercise));
            pending.TrySetResult(true);
            return true;
        }
        public static async Task WaitForMailAsync(CancellationToken token)
        {
            var session = Get();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var inbox = FindAnyObjectByType<IntegratedMailInbox>();
                if (session.emailRead && inbox != null && inbox.WindowObject != null && !inbox.WindowObject.activeInHierarchy) return;
                await Task.Yield();
            }
        }
        private void OnDestroy()
        {
            if (pending != null) pending.TrySetCanceled();
        }
    }
}
