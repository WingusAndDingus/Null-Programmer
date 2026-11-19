using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NullProgrammer.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NullProgrammer.Showcase
{
    public sealed class ShowcasePanel : MonoBehaviour
    {
        private const string WeekendScene = "Weekend Choice";
        private const int EvaluationDay = 7;
        private const float GridWidth = 1080f;
        private static readonly Color Panel = new Color32(24, 34, 50, 255);
        private static readonly Color Tile = new Color32(37, 53, 73, 255);
        private static readonly Color Selected = new Color32(31, 110, 106, 255);
        private static readonly Color Ink = new Color32(235, 242, 252, 255);
        private static readonly Color Muted = new Color32(173, 198, 221, 255);

        private static ShowcasePanel instance;
        public static bool IsOpen => instance != null && instance.open;

        private readonly List<NumberField> numbers = new List<NumberField>();
        private readonly Dictionary<string, Button> clueButtons = new Dictionary<string, Button>();
        private readonly List<Collider2D> disabled2D = new List<Collider2D>();
        private readonly List<Collider> disabled3D = new List<Collider>();
        private Dictionary<Button, WeekendChoice?> weekendButtons;
        private GameObject root;
        private GameObject ownedEventSystem;
        private ScrollRect scroll;
        private GameObject priorSelection;
        private TMP_Text status;
        private TMP_Text context;
        private Button unlockButton;
        private TMP_Text unlockLabel;
        private ShowcaseState draft;
        private bool open;
        private bool jumping;
        private float previousTimeScale;

        private sealed class NumberField
        {
            internal string Label;
            internal TMP_InputField Input;
            internal Func<ShowcaseState, int> Read;
            internal Action<ShowcaseState, int> Write;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var host = new GameObject("Showcase shortcut");
            instance = host.AddComponent<ShowcasePanel>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f12Key.wasPressedThisFrame || jumping) return;
            if (open) Close();
            else if (!AudioSettingsPanel.IsOpen) Open();
        }

        private void OnEnable() => SceneManager.sceneLoaded += CloseOnSceneLoad;

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= CloseOnSceneLoad;
            Close();
        }

        private void CloseOnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && !jumping) Close();
        }

        private void Open()
        {
            if (root == null) Build();
            if (EventSystem.current == null &&
                FindAnyObjectByType<EventSystem>() == null)
            {
                ownedEventSystem = new GameObject("Showcase EventSystem", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }

            priorSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            open = true;
            foreach (Collider2D collider in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (!collider.enabled) continue;
                disabled2D.Add(collider);
                collider.enabled = false;
            }
            foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!collider.enabled) continue;
                disabled3D.Add(collider);
                collider.enabled = false;
            }

            root.SetActive(true);
            LoadForm(GameManager.Instance);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void Close()
        {
            if (!open) return;
            open = false;
            root.SetActive(false);
            Time.timeScale = previousTimeScale;
            foreach (Collider2D collider in disabled2D)
                if (collider != null) collider.enabled = true;
            foreach (Collider collider in disabled3D)
                if (collider != null) collider.enabled = true;
            disabled2D.Clear();
            disabled3D.Clear();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(
                    priorSelection != null && priorSelection.activeInHierarchy ? priorSelection : null);
            priorSelection = null;
            if (ownedEventSystem != null)
            {
                Destroy(ownedEventSystem);
                ownedEventSystem = null;
            }
        }

        private void LoadForm(GameManager game)
        {
            draft = ShowcaseState.Capture(game);
            foreach (NumberField field in numbers)
                field.Input.SetTextWithoutNotify(field.Read(draft).ToString(CultureInfo.InvariantCulture));
            context.text = game == null ? "Start screen" :
                $"Day {game.CurrentDay} · {game.CurrentStep}   |   Ending with applied values: {EndingName(game.PreviewShowcaseEnding())}";
            status.text = "";
            RefreshButtons();
        }

        private bool ReadNumbers()
        {
            var values = new int[numbers.Count];
            for (int i = 0; i < numbers.Count; i++)
            {
                NumberField field = numbers[i];
                if (int.TryParse(field.Input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
                    continue;

                status.text = $"Enter a whole number for {field.Label}.";
                return false;
            }
            for (int i = 0; i < numbers.Count; i++) numbers[i].Write(draft, values[i]);
            return true;
        }

        private void Apply()
        {
            GameManager game = GameManager.Instance;
            if (game == null)
            {
                status.text = "Use a day, weekend, or ending button to start with these values.";
                return;
            }
            if (!ReadNumbers()) return;

            game.ApplyShowcaseState(draft);
            FindAnyObjectByType<WeekendChoiceController>()?.SetupChoiceAvailability();
            FindAnyObjectByType<PersonalitySkills>()?.RefreshShowcaseLevels();
            LoadForm(game);
            status.text = "Applied to this run.";
        }

        private void OpenDay(int day)
        {
            if (!ReadNumbers()) return;
            if (day < EvaluationDay) draft.Weekend = null;
            StartJump(SceneSwitcher.CenterMonitorScene, day, draft);
        }

        private void OpenWeekend(bool unlock)
        {
            if (!ReadNumbers()) return;
            draft.Weekend = null;
            if (unlock) draft.UnlockWeekend = true;
            StartJump(WeekendScene, 6, draft);
        }

        private void StartJump(string scene, int day, ShowcaseState state, GameEndingType? ending = null)
        {
            if (jumping) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                status.text = $"Add {scene} to the active build profile.";
                return;
            }
            jumping = true;
            StartCoroutine(Jump(scene, day, state, ending));
        }

        private IEnumerator Jump(string scene, int day, ShowcaseState state, GameEndingType? ending)
        {
            // The center-monitor executor persists, even if DesktopUI reloads.
            // Destroy it before loading so the destination gets a fresh executor.
            foreach (DayNarrativeExecutor executor in FindObjectsByType<DayNarrativeExecutor>(FindObjectsSortMode.None))
                Destroy(executor.gameObject);
            yield return null;

            GameManager.Instance?.CancelShowcaseClueReactions();
            DialogueManager.Instance?.HideDialogueUI();
            SteveScreenShareManager.ResetChallenge();
            RefactoFightApp.ResetAppLock();

            void InitializeDestination(Scene loaded, LoadSceneMode mode)
            {
                if (mode != LoadSceneMode.Single || loaded.name != scene) return;
                SceneManager.sceneLoaded -= InitializeDestination;

                GameManager game = GameManager.EnsureInstance();
                game.ApplyShowcaseState(state);
                if (ending.HasValue) game.ApplyShowcaseEnding(ending.Value);
                game.PrepareShowcaseJump(day);
                jumping = false;
                Close();
            }

            // Awake has set up the destination's GameManager at this point;
            // DayNarrativeExecutor.Start will see the updated day and step.
            SceneManager.sceneLoaded += InitializeDestination;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        private void Build()
        {
            root = new GameObject("Showcase UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0f;
            RectTransform canvasRect = root.GetComponent<RectTransform>();

            RectTransform backdrop = Box(canvasRect, "Backdrop", new Color32(10, 16, 26, 222), true);
            Stretch(backdrop, 0f, 0f, 0f, 0f);
            RectTransform card = Box(backdrop, "Showcase", Panel, true);
            card.anchorMin = new Vector2(.04f, .04f);
            card.anchorMax = new Vector2(.96f, .96f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            RectTransform title = Text(card, "Showcase controls", 27f, Ink).rectTransform;
            TopBar(title, 22f, 210f, 14f, 34f);
            context = Text(card, "", 17f, Muted);
            TopBar(context.rectTransform, 22f, 178f, 50f, 28f);
            Button close = MakeButton(card, "Close   F12", Close);
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-20f, -17f);
            closeRect.sizeDelta = new Vector2(144f, 42f);

            RectTransform area = Box(card, "Scroll area", new Color32(19, 28, 41, 255), true);
            Stretch(area, 22f, 82f, 22f, 88f);
            scroll = area.gameObject.AddComponent<ScrollRect>();
            area.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = area;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            RectTransform content = NewRect(area, "Controls");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.offsetMin = new Vector2(12f, 0f);
            content.offsetMax = new Vector2(-12f, 0f);
            scroll.content = content;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 7, 9);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Section(content, "Jump to a day");
            RectTransform days = Grid(content, 8, 38f);
            for (int day = 1; day <= 5; day++)
            {
                int destination = day;
                MakeButton(days, $"Day {day}", () => OpenDay(destination));
            }
            MakeButton(days, "Weekend choice", () => OpenWeekend(false));
            MakeButton(days, "Unlocked weekend", () => OpenWeekend(true));
            MakeButton(days, "Day 7 · Evaluate", () => OpenDay(EvaluationDay));

            Section(content, "Play an ending now");
            RectTransform endings = Grid(content, 4, 40f);
            foreach (GameEndingType ending in (GameEndingType[])Enum.GetValues(typeof(GameEndingType)))
                MakeButton(endings, EndingName(ending),
                    () =>
                    {
                        if (ReadNumbers()) StartJump(SceneSwitcher.CenterMonitorScene, EvaluationDay, draft, ending);
                    });

            Section(content, "Run stats");
            RectTransform stats = Grid(content, 4, 34f);
            Number(stats, "Code quality", s => s.CodeQuality, (s, n) => s.CodeQuality = n);
            Number(stats, "Steve suspicion", s => s.Suspicion, (s, n) => s.Suspicion = n);
            Number(stats, "Connectedness", s => s.Connectedness, (s, n) => s.Connectedness = n);
            Number(stats, "Unspent points", s => s.SkillPoints, (s, n) => s.SkillPoints = n);
            Number(stats, "Architect stat", s => s.Architect, (s, n) => s.Architect = n);
            Number(stats, "Pragmatist stat", s => s.Pragmatist, (s, n) => s.Pragmatist = n);
            Number(stats, "Detective stat", s => s.Detective, (s, n) => s.Detective = n);

            Section(content, $"Invested skills · 0–{PersonalitySkills.MaxLevel}");
            RectTransform levels = Grid(content, 5, 34f);
            foreach (Personality personality in ShowcaseState.Personalities)
            {
                int slot = (int)personality;
                Number(levels, personality.ToString(), s => s.Levels[slot], (s, n) => s.Levels[slot] = n);
            }

            Section(content, "Weekend result used for evaluation");
            RectTransform choices = Grid(content, 6, 38f);
            weekendButtons = new Dictionary<Button, WeekendChoice?>();
            AddWeekendButton(choices, "None", null);
            AddWeekendButton(choices, "Steve", WeekendChoice.Steve);
            AddWeekendButton(choices, "Study", WeekendChoice.Study);
            AddWeekendButton(choices, "Personal connections", WeekendChoice.PersonalConnections);
            AddWeekendButton(choices, "Wallow", WeekendChoice.Wallow);
            unlockButton = MakeButton(choices, "Unlock all", () =>
            {
                draft.UnlockWeekend = !draft.UnlockWeekend;
                RefreshButtons();
            });
            unlockLabel = unlockButton.GetComponentInChildren<TMP_Text>();

            Section(content, "Discovered clues");
            RectTransform clues = Grid(content, 5, 37f);
            foreach (string clue in GameManager.ShowcaseClueIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                string caption = Regex.Replace(clue.Substring(clue.IndexOf('_') + 1), "(?<=\\p{Ll})(?=\\p{Lu})", " ");
                clueButtons[clue] = MakeButton(clues, caption, () =>
                {
                    if (!draft.Clues.Add(clue)) draft.Clues.Remove(clue);
                    RefreshButtons();
                });
            }
            MakeButton(clues, "Mark all", () =>
            {
                draft.Clues.UnionWith(GameManager.ShowcaseClueIds);
                RefreshButtons();
            });
            MakeButton(clues, "Clear all", () =>
            {
                draft.Clues.Clear();
                RefreshButtons();
            });

            status = Text(card, "", 17f, Muted);
            RectTransform message = status.rectTransform;
            message.anchorMin = Vector2.zero;
            message.anchorMax = new Vector2(1f, 0f);
            message.offsetMin = new Vector2(24f, 10f);
            message.offsetMax = new Vector2(-185f, 54f);
            Button apply = MakeButton(card, "Apply edits", Apply);
            RectTransform applyRect = (RectTransform)apply.transform;
            applyRect.anchorMin = applyRect.anchorMax = applyRect.pivot = new Vector2(1f, 0f);
            applyRect.anchoredPosition = new Vector2(-22f, 12f);
            applyRect.sizeDelta = new Vector2(146f, 41f);
            root.SetActive(false);
        }

        private void AddWeekendButton(RectTransform parent, string label, WeekendChoice? choice)
        {
            Button button = MakeButton(parent, label, () =>
            {
                draft.Weekend = choice;
                RefreshButtons();
            });
            weekendButtons.Add(button, choice);
        }

        private void RefreshButtons()
        {
            foreach (var pair in weekendButtons)
                pair.Key.image.color = pair.Value == draft.Weekend ? Selected : Tile;
            foreach (var pair in clueButtons)
                pair.Value.image.color = draft.Clues.Contains(pair.Key) ? Selected : Tile;
            unlockButton.image.color = draft.UnlockWeekend ? Selected : Tile;
            unlockLabel.text = draft.UnlockWeekend ? "Unlocked" : "Unlock all";
        }

        private static string EndingName(GameEndingType ending)
        {
            return ending switch
            {
                GameEndingType.ObsoleteAndDeprecated => "Obsolete & Deprecated",
                GameEndingType.ObsoleteSoftRelaunch => "Obsolete, with a Soft Relaunch",
                GameEndingType.ImposterProtocol => "Impostor Protocol",
                GameEndingType.PerfectionistTrap => "The Perfectionist Trap",
                GameEndingType.UltimateMentor => "The Ultimate Mentor",
                GameEndingType.ArchitectRedemption => "The Architect's Redemption",
                GameEndingType.SegmentationFaultOfSoul => "Segmentation Fault of the Soul",
                _ => ending.ToString()
            };
        }

        private void Number(RectTransform parent, string name, Func<ShowcaseState, int> read,
            Action<ShowcaseState, int> write)
        {
            RectTransform field = NewRect(parent, name);
            TMP_Text label = Text(field, name, 17f, Ink);
            Stretch(label.rectTransform, 2f, 0f, 78f, 0f);

            RectTransform area = Box(field, name + " input", Tile, true);
            area.anchorMin = new Vector2(1f, 0f);
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(-74f, 2f);
            area.offsetMax = new Vector2(-4f, -2f);
            RectTransform viewport = NewRect(area, "Text viewport");
            Stretch(viewport, 7f, 0f, 7f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text value = Text(viewport, "", 17f, Ink);
            Stretch(value.rectTransform, 0f, 0f, 0f, 0f);

            TMP_InputField input = area.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = area.GetComponent<Image>();
            input.textViewport = viewport;
            input.textComponent = value;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            numbers.Add(new NumberField { Label = name, Input = input, Read = read, Write = write });
        }

        private static RectTransform Grid(RectTransform parent, int columns, float height)
        {
            RectTransform row = NewRect(parent, "Grid");
            GridLayoutGroup grid = row.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2((GridWidth - (columns - 1) * 8f) / columns, height);
            grid.spacing = new Vector2(8f, 5f);
            return row;
        }

        private static void Section(RectTransform parent, string name)
        {
            TMP_Text label = Text(parent, name, 20f, Muted);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 27f;
        }

        private static Button MakeButton(RectTransform parent, string label, Action click)
        {
            RectTransform box = Box(parent, label, Tile, true);
            Button button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text caption = Text(box, label, 17f, Ink);
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 13f;
            caption.fontSizeMax = 17f;
            Stretch(caption.rectTransform, 5f, 2f, 5f, 2f);
            button.onClick.AddListener(() => click());
            return button;
        }

        private static TMP_Text Text(RectTransform parent, string content, float size, Color color)
        {
            RectTransform box = NewRect(parent, "Text");
            var label = box.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Box(RectTransform parent, string name, Color color, bool raycast)
        {
            RectTransform box = NewRect(parent, name);
            Image image = box.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return box;
        }

        private static RectTransform NewRect(RectTransform parent, string name)
        {
            var box = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            box.SetParent(parent, false);
            return box;
        }

        private static void Stretch(RectTransform box, float left, float bottom, float right, float top)
        {
            box.anchorMin = Vector2.zero;
            box.anchorMax = Vector2.one;
            box.offsetMin = new Vector2(left, bottom);
            box.offsetMax = new Vector2(-right, -top);
        }

        private static void TopBar(RectTransform box, float left, float right, float top, float height)
        {
            box.anchorMin = new Vector2(0f, 1f);
            box.anchorMax = Vector2.one;
            box.offsetMin = new Vector2(left, -top - height);
            box.offsetMax = new Vector2(-right, -top);
        }
    }
}
