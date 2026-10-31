using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NullProgrammer.Day3Workshop
{
    // Day 3 presentation only. The existing session owns progression and rewards.
    public sealed class WorkshopWindow : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private WorkshopSession session;
        private WorkshopUIAssets art;
        private int exercise, row, column, length = 3, hoverRow, hoverColumn;
        private int[] displayOrder = { 0, 1, 2 };
        private bool vertical, tested, running, hovering, showData, inspectorOpen;
        private bool finalSuiteShown, displayedGraphics;
        private GameObject root, choiceOverlay, ownedEvents;
        private RectTransform frame, titleBar, toolbar, inspector, viewport, board;
        private RectTransform baseLayer, previewLayer, choicePanel, codeContent, codeHighlight;
        private TMP_Text heading, objective, stageLabel, status, detail, badgeLabel, code, lineNumbers;
        private TMP_Text orientationLabel, lengthLabel, dataLabel, coordinates, codeScrollHint;
        private Image badge, statusIcon, dataToggle, submitIcon;
        private Button submit, inspect, run, rotate, lengthButton, dataButton, editPatch;
        private readonly Button[] patchButtons = new Button[3];
        private readonly TMP_Text[] patchLabels = new TMP_Text[3];
        private ScrollRect codeScroll;
        private Coroutine animation;
        private BoardSimulation displayedBoard;
        private BoardSimulation.Run displayedRun;
        private WorkshopVisualModel.Sample displayedSample;
        private Vector2 lastSize;
        private readonly List<Collider2D> blocked2D = new List<Collider2D>();
        private readonly List<Collider> blocked3D = new List<Collider>();
        private readonly List<GraphicRaycaster> blockedRaycasters = new List<GraphicRaycaster>();
        private readonly List<Selectable> blockedSelectables = new List<Selectable>();
        private GameObject previousSelection;
        private readonly List<Marker> boardMarks = new List<Marker>();
        private readonly List<Marker> ghostMarks = new List<Marker>();
        private int usedBoardMarks, usedGhostMarks;

        private static readonly Color Background = new Color32(7, 21, 34, 255);
        private static readonly Color PanelColor = new Color32(12, 33, 52, 255);
        private static readonly Color LineColor = new Color32(49, 83, 108, 255);
        private static readonly Color Ink = new Color32(231, 239, 244, 255);
        private static readonly Color Muted = new Color32(145, 169, 188, 255);
        private static readonly Color Cyan = new Color32(82, 216, 245, 255);
        private static readonly Color Amber = new Color32(230, 173, 81, 255);
        private static readonly Color Green = new Color32(121, 202, 162, 255);
        private static readonly Color Red = new Color32(232, 134, 107, 255);

        private sealed class Marker
        {
            public RectTransform rect;
            public Image image;
        }

        public void Show(WorkshopSession owner, int index)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (index < 0 || index > 4) throw new ArgumentOutOfRangeException(nameof(index));
            if (root == null) Build();
            if (root.activeSelf) return;
            session = owner; exercise = index;
            displayOrder = new[] { new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 2, 1, 0 }, new[] { 0, 1, 2 }, new[] { 0, 1, 2 } }[index];
            row = index < 4 ? WorkshopLesson.DemoRow[index] : 1;
            column = index < 4 ? WorkshopLesson.DemoColumn[index] : 1;
            vertical = index < 4 ? WorkshopLesson.DemoVertical[index] : true;
            length = 3;
            tested = running = hovering = inspectorOpen = false;
            finalSuiteShown = displayedGraphics = false;
            showData = index == 3;
            displayedBoard = null; displayedRun = null;
            heading.text = "BATTLESHIP / BOARD WORKSHOP";
            stageLabel.text = "Wednesday / " + WorkshopLesson.Titles[index];
            objective.text = WorkshopLesson.Objectives[index];
            inspector.gameObject.SetActive(false);
            choiceOverlay.SetActive(false);
            root.SetActive(true);
            if (EventSystem.current == null)
            {
                ownedEvents = new GameObject("Day 3 UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ownedEvents.transform.SetParent(transform, false);
            }
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (DialogueManager.Instance != null) DialogueManager.Instance.HideDialogueUI();
            BlockDesktop();
            SetStatus("READY", "Choose a start cell, then run the build.", "Cyan = intended cells. The current code may behave differently.", Cyan, null);
            Canvas.ForceUpdateCanvases();
            ApplyLayout();
            UpdateControls();
            DrawBoard();
        }

        public void Hide()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null; running = hovering = false;
            if (root != null) root.SetActive(false);
            foreach (var c in blocked2D) if (c != null) c.enabled = true;
            foreach (var c in blocked3D) if (c != null) c.enabled = true;
            foreach (var c in blockedRaycasters) if (c != null) c.enabled = true;
            foreach (var c in blockedSelectables) if (c != null) c.interactable = true;
            blocked2D.Clear(); blocked3D.Clear(); blockedRaycasters.Clear(); blockedSelectables.Clear();
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
            if (ownedEvents != null) Destroy(ownedEvents);
            ownedEvents = null;
        }

        private void BlockDesktop()
        {
            foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                if (c.enabled) { blocked2D.Add(c); c.enabled = false; }
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (c.enabled) { blocked3D.Add(c); c.enabled = false; }
            foreach (var c in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
                if (c.enabled && !c.transform.IsChildOf(root.transform)) { blockedRaycasters.Add(c); c.enabled = false; }
            foreach (var c in FindObjectsByType<Selectable>(FindObjectsSortMode.None))
                if (c.interactable && !c.transform.IsChildOf(root.transform)) { blockedSelectables.Add(c); c.interactable = false; }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private WorkshopVisualModel.Sample Selection()
        { return new WorkshopVisualModel.Sample(row, column, length, vertical, exercise == 2); }

        public void HoverCell(int r, int c, bool entered)
        {
            if (root == null || !root.activeSelf || running || choiceOverlay.activeSelf) return;
            if (entered) { hoverRow = r; hoverColumn = c; hovering = true; }
            else if (hovering && hoverRow == r && hoverColumn == c) hovering = false;
            DrawPreview();
        }

        private void Pick(int r, int c)
        {
            if (running || choiceOverlay.activeSelf) return;
            row = r; column = c;
            displayedBoard = null; displayedRun = null;
            // A placement selection does not change the code's last regression grade.
            SetStatus(tested ? "PASS" : "READY", "Selected row " + row + ", column " + column + ".", "Run Test executes the selected placement and checks the current patch.", tested ? Green : Cyan, tested ? "pass-icon" : null);
            DrawBoard();
        }

        private void Rotate()
        {
            if (running) return;
            vertical = !vertical; displayedBoard = null; displayedRun = null;
            UpdateControls(); DrawBoard();
        }

        private void ChangeLength()
        {
            if (running) return;
            length = length == 4 ? 2 : length + 1;
            displayedBoard = null; displayedRun = null;
            UpdateControls(); DrawBoard();
        }

        private void ToggleData()
        {
            if (running || exercise == 3) return;
            showData = !showData;
            UpdateControls(); DrawBoard();
        }

        private void Inspect()
        {
            if (running || exercise == 4 || !session.discovered[exercise]) return;
            SetInspector(!inspectorOpen);
        }

        private void SetInspector(bool visible)
        {
            inspectorOpen = visible;
            inspector.gameObject.SetActive(visible);
            choiceOverlay.SetActive(false);
            hovering = false;
            if (visible) RefreshCode();
            Canvas.ForceUpdateCanvases();
            ApplyLayout();
            DrawPreview();
        }

        private void OpenChoices()
        {
            if (running || exercise == 4) return;
            for (int i = 0; i < patchLabels.Length; i++)
                patchLabels[i].text = WorkshopLesson.Choices[exercise][displayOrder[i]];
            choiceOverlay.SetActive(true);
            hovering = false; DrawPreview();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(patchButtons[0].gameObject);
        }

        private void CloseChoices()
        {
            choiceOverlay.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(editPatch.gameObject);
        }

        private void Choose(int value)
        {
            if (running) return;
            session.patches[exercise] = value;
            tested = false; displayedBoard = null; displayedRun = null;
            choiceOverlay.SetActive(false);
            SetInspector(false);
            SetStatus("READY", "Patch selected. Run the build to see the result.", "Tests evaluate multiple placements; selecting a patch awards no points.", Cyan, null);
            UpdateControls(); DrawBoard();
        }

        private void RunTest()
        {
            if (running) return;
            SetInspector(false);
            hovering = false;
            animation = StartCoroutine(AnimateTest());
        }

        private IEnumerator AnimateTest()
        {
            running = true; UpdateControls();
            var checks = BoardSimulation.CheckPatch(exercise, session.patches);
            if (exercise == 4 && !finalSuiteShown)
            {
                for (int i = 0; i < checks.Count; i++)
                {
                    WorkshopVisualModel.Sample sample = WorkshopVisualModel.FinalSamples[i];
                    ExecutePreview(sample);
                    displayedGraphics = i >= checks.Count - 2;
                    SetStatus("RUN", checks[i].name, "Regression " + (i + 1) + " / " + checks.Count + "  |  " + WorkshopVisualModel.PlacementSummary(displayedRun), checks[i].passed ? Green : Red, checks[i].passed ? "pass-icon" : "fail-icon");
                    DrawBoard();
                    yield return new WaitForSecondsRealtime(.48f);
                }
                finalSuiteShown = true;
                // End on the player's chosen placement so free testing remains meaningful.
                ExecutePreview(Selection());
            }
            else
            {
                ExecutePreview(Selection());
                SetStatus("RUN", "Executing placement...", "Watch the ship segments and occupied cells.", Cyan, "run-icon");
                DrawBoard();
                for (int i = 0; i < usedBoardMarks; i++) boardMarks[i].rect.gameObject.SetActive(false);
                for (int i = 0; i < usedBoardMarks; i++)
                {
                    boardMarks[i].rect.gameObject.SetActive(true);
                    yield return new WaitForSecondsRealtime(.07f);
                }
                yield return new WaitForSecondsRealtime(.15f);
            }
            bool passed = BoardSimulation.Passed(checks);
            tested = passed;
            int passedCount = checks.FindAll(c => c.passed).Count;
            string summary = exercise == 4 ? "Final build: " + passedCount + " / " + checks.Count + " checks passed."
                : WorkshopVisualModel.PlacementSummary(displayedRun);
            string info = passed ? "All " + checks.Count + " checks passed. Try another placement, or Submit to continue."
                : "Failed: " + checks.Find(c => !c.passed).name + ". Inspect Code to repair.";
            SetStatus(passed ? "PASS" : "FAIL", summary, info, passed ? Green : Red, passed ? "pass-icon" : "fail-icon");
            if (!passed && exercise < 4) session.discovered[exercise] = true;
            running = false; animation = null;
            UpdateControls(); DrawBoard();
            if (!passed && exercise < 4) SetInspector(true);
        }

        private void ExecutePreview(WorkshopVisualModel.Sample sample)
        {
            displayedSample = sample;
            displayedGraphics = exercise == 3 || exercise == 4;
            displayedBoard = new BoardSimulation(session.patches);
            if (sample.seeded) displayedBoard.Seed();
            displayedRun = displayedBoard.Place(sample.row, sample.column, sample.length, sample.vertical);
        }

        private void Submit()
        {
            if (tested && !running) session.Submit(exercise);
        }

        private void UpdateControls()
        {
            orientationLabel.text = vertical ? "Rotate / Vertical" : "Rotate / Horizontal";
            lengthLabel.text = "Ship length: " + length;
            dataLabel.text = exercise == 3 ? "Cell data (lesson)" : "Show cell data";
            art.Skin(dataToggle, showData ? "toggle-on" : "toggle-off");
            rotate.interactable = lengthButton.interactable = !running;
            dataButton.interactable = !running && exercise != 3;
            inspect.interactable = !running && exercise < 4 && session.discovered[exercise];
            run.interactable = !running;
            submit.interactable = tested && !running;
            editPatch.interactable = !running;
            foreach (var button in new[] { rotate, lengthButton, dataButton, inspect, run, submit })
                button.GetComponentInChildren<TMP_Text>().color = button.interactable ? Ink : new Color(.42f,.51f,.59f);
            submitIcon.color = submit.interactable ? Color.white : new Color(.5f,.5f,.5f,.45f);
        }

        private void SetStatus(string state, string summary, string explanation, Color color, string icon)
        {
            badgeLabel.text = state; badgeLabel.color = color;
            badge.color = new Color(color.r, color.g, color.b, .12f);
            status.text = summary; status.color = Ink;
            detail.text = explanation; detail.color = Muted;
            statusIcon.sprite = icon == null ? null : art.Get(icon);
            statusIcon.gameObject.SetActive(statusIcon.sprite != null);
        }

        private void RefreshCode()
        {
            string source = WorkshopLesson.Code(exercise, session.patches[exercise]);
            code.text = "<mspace=11.5>" + WorkshopVisualModel.HighlightCode(source) + "</mspace>";
            lineNumbers.text = WorkshopVisualModel.LineNumbers(source);
            float lineHeight = code.GetPreferredValues("X\nX").y - code.GetPreferredValues("X").y;
            lineHeight = Mathf.Max(22, lineHeight);
            int longest = 0;
            foreach (string text in source.Split('\n')) longest = Mathf.Max(longest, text.Length);
            float width = Mathf.Max(((RectTransform)codeScroll.transform).rect.width, longest * 11.5f + 16);
            codeContent.sizeDelta = new Vector2(width, Mathf.Max(116, source.Split('\n').Length * lineHeight + 12));
            codeHighlight.anchorMin = new Vector2(0, 1); codeHighlight.anchorMax = new Vector2(1, 1);
            codeHighlight.pivot = new Vector2(.5f, 1);
            codeHighlight.offsetMin = new Vector2(0, -WorkshopVisualModel.EditableLine(source) * lineHeight - lineHeight - 5);
            codeHighlight.offsetMax = new Vector2(0, -WorkshopVisualModel.EditableLine(source) * lineHeight);
            codeScroll.horizontalNormalizedPosition = 0;
            codeScrollHint.gameObject.SetActive(width > ((RectTransform)codeScroll.transform).rect.width + 3);
        }

        private Marker Take(List<Marker> pool, ref int used, Transform parent, string sprite, int r, int c, Color color, float inset = 4)
        {
            Marker mark;
            if (used == pool.Count)
            {
                var rect = Panel("Board marker", parent, Color.white);
                rect.GetComponent<Image>().raycastTarget = false;
                mark = new Marker { rect = rect, image = rect.GetComponent<Image>() };
                pool.Add(mark);
            }
            else mark = pool[used];
            used++;
            mark.rect.gameObject.name = sprite + " (" + r + "," + c + ")";
            mark.rect.gameObject.SetActive(true);
            CellRect(mark.rect, r, c, inset);
            art.Skin(mark.image, sprite, sprite == "sfml-outline" || sprite == "placement-preview" || sprite == "hover-cell");
            mark.image.color = color;
            return mark;
        }

        private static void TrimPool(List<Marker> pool, int used)
        { for (int i = used; i < pool.Count; i++) pool[i].rect.gameObject.SetActive(false); }

        private static bool VisibleCell(BoardSimulation.Cell cell)
        { return cell.row >= -1 && cell.row <= BoardSimulation.Rows && cell.column >= -1 && cell.column <= BoardSimulation.Columns; }

        private void DrawBoard()
        {
            usedBoardMarks = 0;
            bool seeded = displayedRun != null ? displayedSample.seeded : exercise == 2;
            if (seeded)
            {
                Take(boardMarks, ref usedBoardMarks, baseLayer, "occupied-cell", 2, 3, Color.white);
                Take(boardMarks, ref usedBoardMarks, baseLayer, "occupied-cell", 2, 4, Color.white);
            }
            bool graphicsLesson = exercise == 3 || (exercise == 4 && (displayedGraphics || showData));
            if (displayedBoard != null)
            {
                for (int r = 0; r < BoardSimulation.Rows; r++)
                    for (int c = 0; c < BoardSimulation.Columns; c++)
                    {
                        if (!displayedBoard.occupied[r, c] || (seeded && r == 2 && (c == 3 || c == 4))) continue;
                        if (displayedRun != null && !displayedRun.accepted)
                            Take(boardMarks, ref usedBoardMarks, baseLayer, "stale-cell", r, c, Color.white);
                        else if (!graphicsLesson)
                            Take(boardMarks, ref usedBoardMarks, baseLayer, "occupied-cell", r, c, Color.white);
                        if (showData || exercise == 3)
                            Take(boardMarks, ref usedBoardMarks, baseLayer, "placement-preview", r, c, new Color(.65f, 1f, .82f, .65f), 9);
                    }
            }
            if (displayedRun != null && displayedRun.accepted && graphicsLesson)
            {
                for (int dr = 0; dr < displayedRun.visualRows; dr++)
                    for (int dc = 0; dc < displayedRun.visualColumns; dc++)
                    {
                        int r = displayedRun.visualRow + dr, c = displayedRun.visualColumn + dc;
                        if (!VisibleCell(new BoardSimulation.Cell(r, c))) continue;
                        Take(boardMarks, ref usedBoardMarks, baseLayer, "occupied-cell", r, c, Color.white, 12);
                        Take(boardMarks, ref usedBoardMarks, baseLayer, "sfml-outline", r, c, Color.white, 3);
                    }
            }
            if (displayedRun != null)
                foreach (var cell in displayedRun.cells)
                    if (!BoardSimulation.Inside(cell) && VisibleCell(cell))
                        Take(boardMarks, ref usedBoardMarks, baseLayer, "collision-marker", cell.row, cell.column, Color.white);
            TrimPool(boardMarks, usedBoardMarks);
            DrawPreview();
        }

        private void DrawPreview()
        {
            if (session == null || board == null) return;
            usedGhostMarks = 0;
            WorkshopVisualModel.Sample sample = running && displayedRun != null ? displayedSample : Selection();
            if (hovering && !running) { sample.row = hoverRow; sample.column = hoverColumn; }
            foreach (var cell in WorkshopVisualModel.IntendedCells(sample))
            {
                if (!VisibleCell(cell)) continue;
                bool conflict = WorkshopVisualModel.Conflict(cell, sample.seeded);
                Take(ghostMarks, ref usedGhostMarks, previewLayer,
                    conflict ? "collision-marker" : "placement-preview", cell.row, cell.column,
                    new Color(1, 1, 1, running ? .35f : (displayedRun != null && !hovering ? .22f : .62f)), 3);
            }
            if (hovering && !running)
                Take(ghostMarks, ref usedGhostMarks, previewLayer, "hover-cell", hoverRow, hoverColumn, Color.white, 1);
            TrimPool(ghostMarks, usedGhostMarks);
            coordinates.text = "Row " + sample.row + "  /  Column " + sample.column + "     " + (sample.vertical ? "Vertical" : "Horizontal") + "     Length " + sample.length +
                (exercise == 3 ? "     Mint: cell data  |  Gold: SFML graphic" : "     Cyan: intended  |  Amber: blocked");
        }

        private void LateUpdate()
        {
            if (root == null || !root.activeSelf) return;
            if ((lastSize - frame.rect.size).sqrMagnitude > .1f) ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (frame == null) return;
            lastSize = frame.rect.size;
            float width = frame.rect.width, height = frame.rect.height;
            Dock(titleBar, 0, 0, 0, 52);
            Box(heading.rectTransform, 24, 7, Mathf.Max(180, width - 510), 38);
            Box(stageLabel.rectTransform, Mathf.Max(220, width - 440), 8, 365, 36);
            Dock(objective.rectTransform, 22, 60, 22, 48);
            Dock(toolbar, 18, 116, 18, 46);
            Dock(inspector, 18, 174, 18, 178);
            Dock(viewport, 18, inspectorOpen ? 364 : 174, 18, Mathf.Max(160, height - (inspectorOpen ? 364 : 174) - 126));
            float availableHeight = Mathf.Max(100, viewport.rect.height - 8);
            float fitWidth = Mathf.Min(Mathf.Max(200, viewport.rect.width - 12), availableHeight * 1.25f);
            board.anchorMin = board.anchorMax = board.pivot = new Vector2(.5f, .5f);
            board.anchoredPosition = Vector2.zero;
            board.sizeDelta = new Vector2(fitWidth, fitWidth / 1.25f);
            // Controls stay at their fixed readable size; only the board scales to its viewport.
            Box(inspect.GetComponent<RectTransform>(), Mathf.Max(480, width - 258), 0, 220, 44);
            Box(editPatch.GetComponent<RectTransform>(), 184, 4, 175, 34);
            var back = inspector.Find("Back to board") as RectTransform;
            if (back != null) Box(back, Mathf.Max(370, width - 244), 4, 180, 34);
            if (inspectorOpen) RefreshCode();
            float panelWidth = Mathf.Min(1220, width - 56);
            choicePanel.sizeDelta = new Vector2(panelWidth, 424);
        }

        private void Build()
        {
            art = new WorkshopUIAssets();
            if (font == null) font = TMP_Settings.defaultFontAsset;
            root = new GameObject("Wednesday Board Workspace", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2400;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000); scaler.matchWidthOrHeight = 1f;
            var shade = Panel("Desktop shade", root.transform, Background); Stretch(shade);
            frame = Panel("Board IDE", root.transform, PanelColor, "window-frame"); Stretch(frame, 20);
            titleBar = Panel("Title bar", frame, PanelColor, "title-bar");
            heading = Label(titleBar, "", 27); heading.fontStyle = FontStyles.Bold;
            stageLabel = Label(titleBar, "", 19); stageLabel.alignment = TextAlignmentOptions.MidlineRight; stageLabel.color = Muted;
            var close = MakeButton(titleBar, "Close", "", Hide); AnchorRight(close.GetComponent<RectTransform>(), 8, 7, 38, 38);
            AddIcon(close.transform, "close-icon", 22);
            objective = Label(frame, "", 24);
            toolbar = Panel("Placement controls", frame, Background);
            lengthButton = MakeButton(toolbar, "Ship length", "", ChangeLength); Box((RectTransform)lengthButton.transform, 0, 0, 174, 44);
            lengthLabel = lengthButton.GetComponentInChildren<TMP_Text>();
            rotate = MakeButton(toolbar, "Rotate", "", Rotate); Box((RectTransform)rotate.transform, 186, 0, 272, 44);
            orientationLabel = rotate.GetComponentInChildren<TMP_Text>();
            orientationLabel.rectTransform.offsetMin = new Vector2(38,8);
            var rotationIcon = Panel("Rotation icon", rotate.transform, Color.white, "rotate-icon");
            Box(rotationIcon, 10, 11, 22, 22); rotationIcon.GetComponent<Image>().raycastTarget = false;
            inspect = MakeButton(toolbar, "Inspect Code", "Inspect Code", Inspect);
            viewport = Panel("Board viewport", frame, new Color(.025f, .065f, .105f), "window-frame");
            viewport.gameObject.AddComponent<RectMask2D>();
            board = Panel("Ocean grid", viewport, new Color(0,0,0,0)); board.GetComponent<Image>().raycastTarget = false;
            for (int r = 0; r < BoardSimulation.Rows; r++)
                for (int c = 0; c < BoardSimulation.Columns; c++)
                {
                    int rr = r, cc = c;
                    var tile = Panel("Cell " + r + "," + c, board, new Color(.05f,.15f,.23f), "ocean-tile");
                    CellRect(tile, r, c, 1);
                    var button = tile.gameObject.AddComponent<Button>(); button.targetGraphic = tile.GetComponent<Image>();
                    var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.7f,.9f,1f); colors.pressedColor = new Color(.4f,.7f,.85f); button.colors = colors;
                    button.onClick.AddListener(() => Pick(rr, cc));
                    tile.gameObject.AddComponent<WorkshopCellPointer>().Bind(this, r, c);
                    var edge = Panel("Grid border", tile, LineColor, "grid-cell"); Stretch(edge); edge.GetComponent<Image>().raycastTarget = false;
                }
            var boundary = Panel("Board boundary", board, Cyan, "hover-cell");
            Place(boundary, .1f, .125f, .9f, .875f); boundary.GetComponent<Image>().raycastTarget = false;
            for (int c = 0; c < 8; c++)
            { var t = Label(board, c.ToString(), 22); Place(t.rectTransform, (c+1f)/10, .89f, (c+2f)/10, .98f); t.alignment = TextAlignmentOptions.Center; }
            for (int r = 0; r < 6; r++)
            { var t = Label(board, r.ToString(), 22); Place(t.rectTransform, .01f, 1f-(r+2f)/8, .09f, 1f-(r+1f)/8); t.alignment = TextAlignmentOptions.Center; }
            baseLayer = Layer("Board state", board); previewLayer = Layer("Placement ghost", board);
            BuildInspector(); BuildFooter(); BuildPatchMenu();
            root.SetActive(false);
        }

        private void BuildInspector()
        {
            inspector = Panel("Code inspector", frame, PanelColor, "inspector-frame");
            var tab = Panel("Board.cpp tab", inspector, new Color(.08f,.2f,.29f)); Box(tab, 0, 0, 174, 40);
            var filename = Label(tab, "Board.cpp", 22); Box(filename.rectTransform, 14, 5, 148, 30);
            editPatch = MakeButton(inspector, "Edit patch", "Edit patch", OpenChoices);
            var back = MakeButton(inspector, "Back to board", "Back to board", () => SetInspector(false));
            Box((RectTransform)back.transform, 1100, 4, 180, 34);
            lineNumbers = Label(inspector, "", 20); lineNumbers.alignment = TextAlignmentOptions.TopRight; lineNumbers.color = Muted;
            Box(lineNumbers.rectTransform, 12, 48, 44, 112);
            var scroll = Panel("Code viewport", inspector, new Color(0,0,0,0)); Dock(scroll, 70, 46, 12, 116);
            scroll.gameObject.AddComponent<RectMask2D>();
            codeScroll = scroll.gameObject.AddComponent<ScrollRect>(); codeScroll.viewport = scroll;
            codeScroll.horizontal = true; codeScroll.vertical = false; codeScroll.movementType = ScrollRect.MovementType.Clamped;
            codeContent = Layer("Code content", scroll); codeContent.anchorMin = codeContent.anchorMax = new Vector2(0,1); codeContent.pivot = new Vector2(0,1); codeContent.anchoredPosition = Vector2.zero;
            codeScroll.content = codeContent;
            codeHighlight = Panel("Editable line highlight", codeContent, Amber, "code-line-highlight"); codeHighlight.GetComponent<Image>().raycastTarget = false;
            code = Label(codeContent, "", 20); Stretch(code.rectTransform, 3); code.alignment = TextAlignmentOptions.TopLeft;
            code.textWrappingMode = TextWrappingModes.NoWrap; code.overflowMode = TextOverflowModes.Overflow; code.raycastTarget = true;
            var click = code.gameObject.AddComponent<Button>(); click.targetGraphic = code; click.onClick.AddListener(OpenChoices);
            codeScrollHint = Label(inspector, "Drag code to scroll", 14); codeScrollHint.color = Muted;
            AnchorRight(codeScrollHint.rectTransform, 15, 158, 220, 16); codeScrollHint.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void BuildFooter()
        {
            var footer = Panel("Test result strip", frame, PanelColor, "terminal-frame");
            footer.anchorMin = new Vector2(0,0); footer.anchorMax = new Vector2(1,0); footer.pivot = new Vector2(.5f,0);
            footer.offsetMin = new Vector2(18,36); footer.offsetMax = new Vector2(-18,118);
            badge = Panel("Result badge", footer, Cyan).GetComponent<Image>(); Box((RectTransform)badge.transform, 12, 12, 100, 50);
            badgeLabel = Label(badge.transform, "READY", 23); Stretch(badgeLabel.rectTransform, 4); badgeLabel.alignment = TextAlignmentOptions.Center; badgeLabel.fontStyle = FontStyles.Bold;
            statusIcon = Panel("Result icon", footer, Color.white).GetComponent<Image>(); Box((RectTransform)statusIcon.transform, 124, 19, 24, 24); statusIcon.raycastTarget = false;
            status = Label(footer, "", 20); Dock(status.rectTransform, 158, 7, 565, 36); status.overflowMode = TextOverflowModes.Ellipsis;
            detail = Label(footer, "", 17); Dock(detail.rectTransform, 124, 43, 565, 31); detail.overflowMode = TextOverflowModes.Ellipsis;
            dataButton = MakeButton(footer, "Show cell data", "", ToggleData); AnchorRight((RectTransform)dataButton.transform, 348, 18, 200, 44);
            dataLabel = dataButton.GetComponentInChildren<TMP_Text>(); dataLabel.fontSize = 18;
            dataLabel.rectTransform.offsetMin = new Vector2(32,8);
            dataToggle = Panel("Data checkbox", dataButton.transform, Color.white, "toggle-off").GetComponent<Image>();
            Box((RectTransform)dataToggle.transform, 8, 12, 20, 20); dataToggle.raycastTarget = false;
            run = MakeButton(footer, "Run Test", "Run Test", RunTest); AnchorRight((RectTransform)run.transform, 178, 18, 154, 44);
            submit = MakeButton(footer, "Submit", "Submit", Submit); AnchorRight((RectTransform)submit.transform, 12, 18, 154, 44);
            var playIcon = Panel("Run icon", run.transform, Color.white, "run-icon"); Box(playIcon, 12, 13, 18, 18); playIcon.GetComponent<Image>().raycastTarget = false;
            run.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new Vector2(33,8);
            submitIcon = Panel("Submit icon", submit.transform, Color.white, "pass-icon").GetComponent<Image>();
            Box((RectTransform)submitIcon.transform, 12, 13, 18, 18); submitIcon.raycastTarget = false;
            submit.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new Vector2(33,8);
            coordinates = Label(frame, "", 16); coordinates.color = Muted;
            coordinates.rectTransform.anchorMin = new Vector2(0,0); coordinates.rectTransform.anchorMax = new Vector2(1,0);
            coordinates.rectTransform.offsetMin = new Vector2(28,7); coordinates.rectTransform.offsetMax = new Vector2(-28,31);
        }

        private void BuildPatchMenu()
        {
            choiceOverlay = new GameObject("Patch selection overlay", typeof(RectTransform), typeof(Image), typeof(Button));
            choiceOverlay.transform.SetParent(root.transform, false); Stretch((RectTransform)choiceOverlay.transform);
            choiceOverlay.GetComponent<Image>().color = new Color(.015f,.03f,.05f,.9f);
            var outside = choiceOverlay.GetComponent<Button>(); outside.targetGraphic = choiceOverlay.GetComponent<Image>(); outside.onClick.AddListener(CloseChoices);
            choicePanel = Panel("Patch choices", choiceOverlay.transform, PanelColor, "patch-menu-frame");
            choicePanel.anchorMin = choicePanel.anchorMax = choicePanel.pivot = new Vector2(.5f,.5f);
            choicePanel.anchoredPosition = Vector2.zero; choicePanel.sizeDelta = new Vector2(1220,424);
            var title = Label(choicePanel, "Board.cpp / choose a replacement", 25); Dock(title.rectTransform, 22, 14, 80, 42);
            var close = MakeButton(choicePanel, "Close patches", "X", CloseChoices); AnchorRight((RectTransform)close.transform, 16, 14, 42, 38);
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                patchButtons[i] = MakeButton(choicePanel, "Patch " + (i+1), "", () => Choose(displayOrder[slot]));
                Dock((RectTransform)patchButtons[i].transform, 22, 76+i*96, 22, 82);
                patchLabels[i] = patchButtons[i].GetComponentInChildren<TMP_Text>();
                patchLabels[i].alignment = TextAlignmentOptions.MidlineLeft; patchLabels[i].fontSize = 22;
            }
            var note = Label(choicePanel, "Choose a patch, then Run Test to observe its behavior.", 17); Dock(note.rectTransform, 24, 374, 24, 28); note.color = Muted;
        }

        private RectTransform Panel(string name, Transform parent, Color color, string sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color;
            if (sprite != null) art.Skin(image, sprite, true);
            return (RectTransform)go.transform;
        }
        private TMP_Text Label(Transform parent, string value, float size)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = size; label.color = Ink;
            label.text = value; label.enableAutoSizing = false; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) label.fontSharedMaterial = font.material;
            return label;
        }
        private Button MakeButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(name, parent, new Color(.06f,.15f,.21f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); art.SkinButton(button);
            button.onClick.AddListener(action);
            var label = Label(rect, caption, 21); Stretch(label.rectTransform, 8); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private void AddIcon(Transform parent, string name, float size)
        {
            var icon = Panel(name, parent, Color.white, name); icon.GetComponent<Image>().raycastTarget = false;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f,.5f); icon.anchoredPosition = Vector2.zero; icon.sizeDelta = new Vector2(size,size);
        }
        private static RectTransform Layer(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; Stretch(rect); return rect;
        }
        private static void CellRect(RectTransform rect, int r, int c, float inset)
        {
            Place(rect, (c+1f)/10, 1f-(r+2f)/8, (c+2f)/10, 1f-(r+1f)/8);
            rect.offsetMin = new Vector2(inset,inset); rect.offsetMax = new Vector2(-inset,-inset);
        }
        private static void Stretch(RectTransform rect, float inset = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(inset,inset); rect.offsetMax = new Vector2(-inset,-inset); }
        private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        { rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1); rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Dock(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0,1); rect.anchorMax = new Vector2(1,1); rect.pivot = new Vector2(.5f,1);
            rect.offsetMin = new Vector2(left,-top-height); rect.offsetMax = new Vector2(-right,-top);
        }
        private static void Box(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(left,-top); rect.sizeDelta = new Vector2(width,height);
        }
        private static void AnchorRight(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1,1); rect.pivot = new Vector2(1,1);
            rect.anchoredPosition = new Vector2(-right,-top); rect.sizeDelta = new Vector2(width,height);
        }
        private void OnDisable() { Hide(); }
        private void OnDestroy() { Hide(); if (root != null) Destroy(root); }
    }
}
