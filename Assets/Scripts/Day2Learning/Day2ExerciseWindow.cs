using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NullProgrammer.Day2
{
    public sealed class Day2ExerciseWindow : MonoBehaviour
    {
        private static readonly Color Background = new Color(.045f, .065f, .095f);
        private static readonly Color PanelColor = new Color(.075f, .11f, .16f);
        private static readonly Color Teal = new Color(.08f, .73f, .77f);
        private static readonly Color Green = new Color(.20f, .77f, .48f);
        private static readonly Color Orange = new Color(.98f, .44f, .19f);

        private GameObject canvasObject, ownedEventSystem;
        private Text title, progressText, code, objective, output, feedback, hintText, previewCaption;
        private RectTransform preview;
        private Button[] answerButtons, hintButtons;
        private Text[] answerLabels;
        private Button runButton, submitButton;
        private Font font;
        private int exercise, selection = -1;
        private bool grantRewards;
        private Day2LessonProgress progress;
        private TaskCompletionSource<bool> completion;
        private readonly List<Collider2D> blocked2D = new List<Collider2D>();
        private readonly List<Collider> blocked3D = new List<Collider>();

        public static async Task RunAsync(int exerciseIndex, CancellationToken cancellationToken, bool grantRewards = true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (exerciseIndex < 0 || exerciseIndex >= Day2LessonData.ExerciseCount)
                throw new ArgumentOutOfRangeException(nameof(exerciseIndex));
            if (Day2LessonSession.Get().progress.submitted[exerciseIndex]) return;
            GameManager manager = GameManager.EnsureInstance();
            Day2ExerciseWindow window = manager.GetComponent<Day2ExerciseWindow>();
            if (window == null) window = manager.gameObject.AddComponent<Day2ExerciseWindow>();
            if (window.completion != null)
                throw new InvalidOperationException("Another Day 2 exercise is already open.");

            window.Open(exerciseIndex, grantRewards);
            Task task = window.completion.Task;
            using (cancellationToken.Register(window.Cancel))
            {
                await task;
            }
        }

        private void Open(int index, bool rewards)
        {
            if (canvasObject == null) BuildView();
            EnsureEventSystem();
            exercise = index;
            grantRewards = rewards;
            selection = -1;
            progress = Day2LessonSession.Get().progress;
            completion = new TaskCompletionSource<bool>();
            if (DialogueManager.Instance != null) DialogueManager.Instance.HideDialogueUI();
            BlockWorld();
            canvasObject.SetActive(true);

            Day2Exercise data = Day2LessonData.Exercises[exercise];
            title.text = data.title;
            code.text = data.faultyCode;
            objective.text = data.objective;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerLabels[i].text = ((char)('A' + i)) + ". " + data.replacements[i];
                answerButtons[i].GetComponent<Image>().color = PanelColor;
            }
            for (int i = 0; i < hintButtons.Length; i++)
            {
                Personality personality = RolePersonality(i);
                hintButtons[i].GetComponentInChildren<Text>().text =
                    new[] { "Architect", "Pragmatist", "Detective" }[i] + "\n" + (PersonalityLevel)Level(personality);
            }
            output.text = "Choose A, B, or C.\n\nRun Tests checks your current selection.\nSubmit commits it and continues. Testing is optional.";
            output.color = new Color(.89f, .94f, .98f);
            feedback.text = "Submit accepts the selected answer even if tests fail. Quality depends on the submitted code.";
            hintText.text = "Need help? Ask a personality below. Their advice depends on your invested rank.";
            previewCaption.text = "Run Tests to compare the required cells with your result.";
            DrawPreview(null);
            runButton.interactable = false;
            submitButton.interactable = false;
            RefreshProgress();
        }

        private void Choose(int index)
        {
            if (completion == null) return;
            selection = index;
            for (int i = 0; i < answerButtons.Length; i++)
                answerButtons[i].GetComponent<Image>().color = i == index ? new Color(.10f, .36f, .43f) : PanelColor;
            runButton.interactable = true;
            submitButton.interactable = true;
            code.text = Day2LessonData.Exercises[exercise].faultyCode + "\n\n// Selected replacement:\n" +
                Day2LessonData.Exercises[exercise].replacements[index];
            output.text = "This selection has not been tested.\n\nRun Tests for feedback, or Submit to commit this choice.";
            output.color = new Color(.89f, .94f, .98f);
            feedback.text = "Selection ready. You can submit immediately or check the tests first.";
            previewCaption.text = "Run Tests to preview this selection.";
            DrawPreview(null);
        }

        private void RunTest()
        {
            if (completion == null || selection < 0) return;
            Day2TestEngine.Run result = Day2TestEngine.Evaluate(exercise, selection);
            progress.RecordTest(exercise);
            output.text = result.output;
            output.color = result.passed ? new Color(.65f, 1f, .78f) : new Color(1f, .78f, .64f);
            feedback.text = Day2LessonData.Exercises[exercise].explanations[selection];
            feedback.text += "\nOn Submit: +" + result.qualityPoints + " / 5 code quality. Testing does not commit or award points.";
            submitButton.interactable = true;
            previewCaption.text = result.previewCaption;
            DrawPreview(result);
            RefreshProgress();
        }

        private void Hint(int role)
        {
            if (completion == null) return;
            int level = Level(RolePersonality(role));
            hintText.text = Day2LessonData.Exercises[exercise].hints[role][level];
            if (role == 2)
            {
                if (progress.UseDetective(exercise))
                {
                    GameManager manager = GameManager.EnsureInstance();
                    int before = manager.SteveSuspicion;
                    manager.ChangeSteveSuspicion(-new[] { 1, 2, 3, 5, 7 }[level]);
                    int change = manager.SteveSuspicion - before;
                    hintText.text += change < 0 ? "\nSuspicion " + change + ". Your code quality still depends on the submitted answer."
                        : "\nSuspicion is already zero. Your code quality still depends on the submitted answer.";
                }
                else hintText.text += "\nReassurance already used for this exercise; no repeated suspicion reduction.";
            }
            RefreshProgress();
        }

        private static Personality RolePersonality(int role)
            => role == 0 ? Personality.Architect : role == 1 ? Personality.Pragmatist : Personality.Detective;

        private static int Level(Personality personality)
        {
            PersonalitySkills skills = FindAnyObjectByType<PersonalitySkills>();
            int level = skills != null ? skills.GetLevel(personality) : PersonalitySkills.GetSavedLevel(personality);
            return Mathf.Clamp(level, 0, PersonalitySkills.MaxLevel);
        }

        private void RefreshProgress()
        {
            GameManager manager = GameManager.EnsureInstance();
            progressText.text = "Exercise " + (exercise + 1) + " / 4  |  Submitted " + progress.SubmittedCount +
                " / 4  |  Passed " + progress.PassedCount + " / 4  |  Test runs " + progress.attempts[exercise] + "  |  Suspicion " + manager.SteveSuspicion +
                "  |  " + (grantRewards ? "Code quality " + manager.CodeQuality : "Preview quality " + progress.TotalQuality + " / 20");
        }

        private void Submit()
        {
            if (completion == null || selection < 0) return;
            int qualityPoints;
            bool firstSubmission = progress.TrySubmit(exercise, selection, out qualityPoints);
            if (firstSubmission && grantRewards)
            {
                GameManager.EnsureInstance().ChangeCodeQualityStat(qualityPoints);
                if (progress.passed[exercise])
                    PersonalitySkills.TryAwardMilestone("Day2_Exercise_" + exercise + "_Passed");
            }
            TaskCompletionSource<bool> task = completion;
            completion = null;
            canvasObject.SetActive(false);
            UnblockWorld();
            task.TrySetResult(true);
        }

        private void Cancel()
        {
            TaskCompletionSource<bool> task = completion;
            completion = null;
            if (canvasObject != null) canvasObject.SetActive(false);
            UnblockWorld();
            if (task != null) task.TrySetCanceled();
        }

        private void OnDestroy()
        {
            Cancel();
            if (canvasObject != null) Destroy(canvasObject);
            if (ownedEventSystem != null) Destroy(ownedEventSystem);
        }

        private void BlockWorld()
        {
            foreach (Collider2D collider in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                if (collider.enabled) { blocked2D.Add(collider); collider.enabled = false; }
            foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (collider.enabled) { blocked3D.Add(collider); collider.enabled = false; }
        }

        private void UnblockWorld()
        {
            foreach (Collider2D collider in blocked2D) if (collider != null) collider.enabled = true;
            foreach (Collider collider in blocked3D) if (collider != null) collider.enabled = true;
            blocked2D.Clear(); blocked3D.Clear();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            ownedEventSystem = new GameObject("Day 2 EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            ownedEventSystem.transform.SetParent(transform, false);
        }

        private void BuildView()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasObject = new GameObject("Day 2 Code Exercises", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.matchWidthOrHeight = .5f;
            RectTransform shade = Panel("Backdrop", canvasObject.transform, new Color(0, 0, 0, .90f), 0, 0, 1, 1);
            RectTransform window = Panel("Exercise Window", shade, Background, .025f, .025f, .975f, .975f);
            title = Label("Title", window, 27, .025f, .94f, .975f, .99f);
            title.color = Teal;
            progressText = Label("Progress", window, 17, .025f, .897f, .975f, .934f);

            RectTransform sourcePanel = Panel("Source", window, PanelColor, .025f, .535f, .625f, .885f);
            code = Label("Code", sourcePanel, 17, .025f, .025f, .975f, .975f);
            code.supportRichText = false;
            objective = Label("Objective", window, 18, .025f, .455f, .625f, .525f);

            RectTransform previewPanel = Panel("Grid Preview", window, PanelColor, .65f, .535f, .975f, .885f);
            Text legend = Label("Legend", previewPanel, 14, .03f, .88f, .97f, .98f);
            legend.resizeTextForBestFit = true;
            legend.resizeTextMinSize = 11; legend.resizeTextMaxSize = 14;
            legend.text = "TEAL: expected   GREEN: match   ORANGE: mismatch";
            preview = Panel("Cells", previewPanel, Background, .18f, .22f, .82f, .86f);
            preview.SetParent(Panel("Preview Bounds", previewPanel, PanelColor, .08f, .23f, .92f, .86f), false);
            AspectRatioFitter square = preview.gameObject.AddComponent<AspectRatioFitter>();
            square.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            square.aspectRatio = 1;
            previewCaption = Label("Preview Caption", previewPanel, 14, .03f, .02f, .97f, .20f);

            RectTransform outputPanel = Panel("Test Output", window, PanelColor, .65f, .265f, .975f, .525f);
            output = Label("Output", outputPanel, 15, .035f, .03f, .965f, .97f);
            feedback = Label("Feedback", window, 15, .65f, .145f, .975f, .255f);
            feedback.resizeTextForBestFit = true;
            feedback.resizeTextMinSize = 12; feedback.resizeTextMaxSize = 15;
            hintText = Label("Personality Advice", window, 17, .025f, .14f, .625f, .245f);
            hintText.resizeTextForBestFit = true;
            hintText.resizeTextMinSize = 13; hintText.resizeTextMaxSize = 17;

            answerButtons = new Button[3]; answerLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                float bottom = .39f - i * .067f;
                answerButtons[i] = MakeButton("Answer " + i, window, "", .025f, bottom, .625f, bottom + .060f,
                    PanelColor, () => Choose(index));
                answerLabels[i] = answerButtons[i].GetComponentInChildren<Text>();
                answerLabels[i].fontSize = 15;
                answerLabels[i].alignment = TextAnchor.MiddleLeft;
                answerLabels[i].supportRichText = false;
                answerLabels[i].resizeTextForBestFit = true;
                answerLabels[i].resizeTextMinSize = 12; answerLabels[i].resizeTextMaxSize = 15;
            }
            hintButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int role = i;
                float left = .025f + i * .205f;
                hintButtons[i] = MakeButton("Personality " + i, window, "", left, .035f, left + .19f, .115f,
                    i == 0 ? new Color(.04f, .35f, .40f) : i == 1 ? new Color(.46f, .23f, .08f) : new Color(.29f, .15f, .43f),
                    () => Hint(role));
                hintButtons[i].GetComponentInChildren<Text>().color = Color.white;
            }
            runButton = MakeButton("Run", window, "Run Tests", .65f, .035f, .795f, .115f,
                new Color(.08f, .37f, .41f), RunTest);
            submitButton = MakeButton("Submit", window, "Submit", .815f, .035f, .975f, .115f,
                new Color(.10f, .34f, .23f), Submit);
            canvasObject.SetActive(false);
        }

        private RectTransform Panel(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            RectTransform rect = go.GetComponent<RectTransform>();
            Place(rect, x0, y0, x1, y1);
            return rect;
        }

        private Text Label(string name, Transform parent, int size, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.color = new Color(.89f, .94f, .98f);
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Place(go.GetComponent<RectTransform>(), x0, y0, x1, y1);
            return text;
        }

        private Button MakeButton(string name, Transform parent, string caption,
            float x0, float y0, float x1, float y1, Color color, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Panel(name, parent, color, x0, y0, x1, y1);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Text text = Label("Text", rect, 18, .035f, .035f, .965f, .965f);
            text.text = caption; text.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(action);
            return button;
        }

        private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void DrawPreview(Day2TestEngine.Run run)
        {
            for (int i = preview.childCount - 1; i >= 0; i--) Destroy(preview.GetChild(i).gameObject);
            if (run == null) return;
            int rows = run.rows, cols = run.cols;
            foreach (Day2TestEngine.Cell cell in run.observed)
            {
                rows = Math.Max(rows, (int)Math.Ceiling(cell.row + 1));
                cols = Math.Max(cols, (int)Math.Ceiling(cell.col + 1));
            }
            int extent = Math.Max(rows, cols);
            for (int row = 0; row < run.rows; row++)
                for (int col = 0; col < run.cols; col++) DrawCell(row, col, extent, new Color(.11f, .18f, .24f));
            foreach (Day2TestEngine.Cell cell in run.expected) DrawCell(cell.row, cell.col, extent, Teal);
            foreach (Day2TestEngine.Cell cell in run.observed)
            {
                bool match = false;
                foreach (Day2TestEngine.Cell expected in run.expected)
                    if (Math.Abs(cell.row - expected.row) < .001f && Math.Abs(cell.col - expected.col) < .001f)
                    { match = true; break; }
                DrawCell(cell.row, cell.col, extent, match ? Green : Orange);
            }
        }

        private void DrawCell(float row, float col, int extent, Color color)
        {
            float unit = .90f / Math.Max(1, extent);
            RectTransform cell = Panel("Cell", preview, color, .05f + col * unit, .95f - (row + 1) * unit,
                .05f + (col + 1) * unit, .95f - row * unit);
            cell.offsetMin = new Vector2(.5f, .5f);
            cell.offsetMax = new Vector2(-.5f, -.5f);
            cell.GetComponent<Image>().raycastTarget = false;
        }
    }
}
