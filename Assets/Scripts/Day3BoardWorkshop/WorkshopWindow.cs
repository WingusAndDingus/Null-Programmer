using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NullProgrammer.Day3Workshop
{
    // Board-first overlay. The small repair panel appears only after a test exposes a bug.
    public sealed class WorkshopWindow : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private WorkshopSession session;
        private int exercise, row, column, length = 3;
        private int[] displayOrder = { 0, 1, 2 };
        private bool vertical = true, tested, running;
        private GameObject root, popup, choices, ownedEvents;
        private RectTransform board;
        private TMP_Text heading, objective, status, code, orientationLabel, lengthLabel;
        private Button submit, inspect, run;
        private readonly List<Collider2D> colliders2D = new List<Collider2D>();
        private readonly List<Collider> colliders3D = new List<Collider>();
        private readonly List<GraphicRaycaster> raycasters = new List<GraphicRaycaster>();
        private readonly List<GameObject> marks = new List<GameObject>();
        private readonly List<CanvasGroup> disabledGroups = new List<CanvasGroup>();
        private Coroutine animation;
        private readonly Color panel = new Color(.055f,.095f,.14f), ink = new Color(.9f,.95f,1f);
        private readonly Color teal = new Color(.12f,.68f,.77f), green = new Color(.22f,.8f,.5f), red = new Color(.95f,.4f,.3f);

        public void Show(WorkshopSession owner, int index)
        {
            if (root == null) Build();
            if (root.activeSelf) return;
            session = owner; exercise = index;
            displayOrder = new[] { new[] {0,2,1}, new[] {1,0,2}, new[] {2,1,0}, new[] {0,1,2}, new[] {0,1,2} }[index];
            row = index < 4 ? WorkshopLesson.DemoRow[index] : 1;
            column = index < 4 ? WorkshopLesson.DemoColumn[index] : 1;
            vertical = index < 4 ? WorkshopLesson.DemoVertical[index] : true;
            tested = false; running = false;
            heading.text = "BATTLESHIP / BOARD WORKSHOP       " + WorkshopLesson.Titles[index];
            objective.text = WorkshopLesson.Objectives[index];
            status.text = "Click a starting cell, then Run Test. Outlined cells show the intended footprint.";
            popup.SetActive(false); choices.SetActive(false);
            root.SetActive(true);
            if (EventSystem.current == null)
                ownedEvents = new GameObject("Workshop Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (DialogueManager.Instance != null) DialogueManager.Instance.HideDialogueUI();
            BlockDesktop();
            UpdateButtons(); Draw(null, null);
        }
        public void Hide()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null; running = false;
            if (root != null) root.SetActive(false);
            foreach (var c in colliders2D) if (c != null) c.enabled = true;
            foreach (var c in colliders3D) if (c != null) c.enabled = true;
            foreach (var c in raycasters) if (c != null) c.enabled = true;
            foreach (var group in disabledGroups) if (group != null) group.blocksRaycasts = true;
            colliders2D.Clear(); colliders3D.Clear(); raycasters.Clear(); disabledGroups.Clear();
            if (ownedEvents != null) Destroy(ownedEvents);
            ownedEvents = null;
        }
        private void BlockDesktop()
        {
            foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                if (c.enabled) { colliders2D.Add(c); c.enabled = false; }
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (c.enabled) { colliders3D.Add(c); c.enabled = false; }
            foreach (var c in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
                if (c.enabled && c.gameObject != root) { raycasters.Add(c); c.enabled = false; }
        }
        private void Pick(int r, int c)
        {
            if (running) return;
            row = r; column = c;
            Draw(null, null);
            status.text = "Selected row " + row + ", column " + column + ". Run Test to execute this build.";
        }
        private void Rotate()
        {
            if (running) return;
            vertical = !vertical; UpdateButtons(); Draw(null, null);
        }
        private void ChangeLength()
        {
            if (running) return;
            length = length == 4 ? 2 : length + 1; UpdateButtons(); Draw(null, null);
        }
        private void UpdateButtons()
        {
            orientationLabel.text = vertical ? "Orientation: vertical" : "Orientation: horizontal";
            lengthLabel.text = "Ship length: " + length;
            inspect.interactable = exercise < 4 && session.discovered[exercise];
            submit.interactable = tested && !running;
            run.interactable = !running;
        }
        private void Inspect()
        {
            if (exercise >= 4 || !session.discovered[exercise] || running) return;
            popup.SetActive(!popup.activeSelf); choices.SetActive(false);
            code.text = WorkshopLesson.Code(exercise, session.patches[exercise]);
        }
        private void Choose(int value)
        {
            if (running) return;
            session.patches[exercise] = value;
            tested = false;
            popup.SetActive(false); choices.SetActive(false);
            status.text = "Patch selected. Run Test checks it against multiple placements.";
            UpdateButtons(); Draw(null, null);
        }
        private void RunTest()
        {
            if (running) return;
            popup.SetActive(false); choices.SetActive(false);
            animation = StartCoroutine(AnimateTest());
        }
        private IEnumerator AnimateTest()
        {
            running = true; UpdateButtons();
            var simulation = new BoardSimulation(session.patches);
            if (exercise == 2) simulation.Seed();
            BoardSimulation.Run result = simulation.Place(row, column, length, vertical);
            var checks = BoardSimulation.CheckPatch(exercise, session.patches);
            Draw(simulation, result);
            // Reveal segment execution in order rather than hiding the board behind output text.
            int count = marks.Count;
            for (int i = 0; i < count; i++) if (marks[i] != null) marks[i].SetActive(false);
            for (int i = 0; i < count; i++)
            {
                if (marks[i] != null) marks[i].SetActive(true);
                yield return new WaitForSecondsRealtime(.065f);
            }
            if (exercise == 4)
            {
                foreach (var check in checks)
                {
                    status.text = (check.passed ? "PASS / " : "FAIL / ") + check.name;
                    status.color = check.passed ? green : red;
                    yield return new WaitForSecondsRealtime(.35f);
                }
            }
            bool passed = BoardSimulation.Passed(checks);
            tested = passed;
            status.color = passed ? green : red;
            if (!passed)
            {
                if (exercise < 4) session.discovered[exercise] = true;
                var failed = checks.Find(c => !c.passed);
                status.text = result.reason + "  /  Failed: " + failed.name + ". Inspect Code to repair.";
            }
            else status.text = result.reason + "  /  Regression checks passed. Try another placement or Submit.";
            running = false; animation = null; UpdateButtons();
            if (!passed && exercise < 4) Inspect();
        }
        private void Submit()
        {
            if (tested && !running) session.Submit(exercise);
        }
        private void ClearMarks()
        {
            foreach (var mark in marks) if (mark != null) Destroy(mark);
            marks.Clear();
        }
        private void Mark(int r, int c, Color color, bool outline, string name)
        {
            // The grid has a one-cell gutter, so an overrun remains visible.
            if (r < -1 || r > BoardSimulation.Rows || c < -1 || c > BoardSimulation.Columns) return;
            float x = (c + 1f) / 10f, y = 1f - (r + 2f) / 8f;
            var rect = Panel(name, board, color, x, y, x + .1f, y + .125f);
            rect.offsetMin = new Vector2(4,4); rect.offsetMax = new Vector2(-4,-4);
            rect.GetComponent<Image>().raycastTarget = false;
            if (outline)
            {
                rect.GetComponent<Image>().color = new Color(0,0,0,0);
                foreach (var edge in new[] { new Vector4(0,0,.06f,1), new Vector4(.94f,0,1,1), new Vector4(0,0,1,.06f), new Vector4(0,.94f,1,1) })
                { var strip = Panel("Outline",rect,color,edge.x,edge.y,edge.z,edge.w); strip.GetComponent<Image>().raycastTarget = false; }
            }
            marks.Add(rect.gameObject);
        }
        private void Draw(BoardSimulation model, BoardSimulation.Run result)
        {
            ClearMarks();
            if (exercise == 2)
            {
                Mark(2,3,new Color(.2f,.4f,.58f),false,"Existing ship");
                Mark(2,4,new Color(.2f,.4f,.58f),false,"Existing ship");
            }
            for (int i = 0; i < length; i++)
                Mark(row + (vertical ? i : 0), column + (vertical ? 0 : i), teal, true, "Expected footprint");
            if (model != null)
            {
                for (int r = 0; r < BoardSimulation.Rows; r++)
                    for (int c = 0; c < BoardSimulation.Columns; c++)
                        if (model.occupied[r,c] && !(exercise == 2 && r == 2 && (c == 3 || c == 4)))
                            Mark(r,c,result.accepted ? green : red,false,"Board data");
            }
            if (result == null) return;
            foreach (var cell in result.cells)
                if (!BoardSimulation.Inside(cell)) Mark(cell.row,cell.column,red,false,"Out of bounds");
            if (result.accepted && (exercise == 3 || exercise == 4))
                for (int r = 0; r < result.visualRows; r++) for (int c = 0; c < result.visualColumns; c++)
                    Mark(result.visualRow + r,result.visualColumn + c,new Color(1f,.75f,.25f),true,"SFML graphic outline");
        }
        private void Build()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            root = new GameObject("Wednesday Board Workspace",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2400;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,1000); scaler.matchWidthOrHeight = .5f;
            Panel("Desktop shade",root.transform,new Color(.01f,.025f,.045f,.97f),0,0,1,1);
            var frame = Panel("Board IDE",root.transform,panel,.04f,.035f,.96f,.965f);
            heading = Label(frame, "",26,.025f,.93f,.82f,.985f);
            Button(frame,"Close",.89f,.93f,.98f,.985f,Hide);
            objective = Label(frame,"",24,.025f,.855f,.98f,.925f);
            orientationLabel = Button(frame,"",.025f,.79f,.27f,.85f,Rotate).GetComponentInChildren<TMP_Text>();
            lengthLabel = Button(frame,"",.285f,.79f,.45f,.85f,ChangeLength).GetComponentInChildren<TMP_Text>();
            inspect = Button(frame,"Inspect Code",.73f,.79f,.975f,.85f,Inspect);
            // Constant grid dimensions; no resizing or stacked text panels on discovery.
            var host = Panel("Board Bounds",frame,new Color(.025f,.05f,.08f),.08f,.15f,.92f,.77f);
            var area = Panel("Board Area",host,new Color(.025f,.05f,.08f),0,0,1,1);
            var aspect = area.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 10f/8f;
            board = area;
            for(int r=0;r<6;r++) for(int c=0;c<8;c++)
            {
                int rr=r, cc=c;
                var cell = Button(board,"",(c+1f)/10f,1f-(r+2f)/8f,(c+2f)/10f,1f-(r+1f)/8f,()=>Pick(rr,cc));
                cell.image.color = new Color(.09f,.17f,.23f);
                var rect = (RectTransform)cell.transform; rect.offsetMin = new Vector2(2,2); rect.offsetMax = new Vector2(-2,-2);
            }
            for(int c=0;c<8;c++) Label(board,c.ToString(),20,(c+1f)/10f,.89f,(c+2f)/10f,.98f);
            for(int r=0;r<6;r++) Label(board,r.ToString(),20,.015f,1f-(r+2f)/8f,.09f,1f-(r+1f)/8f);
            status = Label(frame,"",21,.025f,.065f,.70f,.145f);
            run = Button(frame,"Run Test",.72f,.065f,.84f,.135f,RunTest);
            submit = Button(frame,"Submit",.85f,.065f,.975f,.135f,Submit);
            Label(frame,"Teal outline: intended cells     Green: board data     Gold outline: SFML graphic",18,.025f,.012f,.98f,.06f);
            popup = Panel("Code Inspector",frame,new Color(.065f,.115f,.17f),.025f,.78f,.975f,.93f).gameObject;
            code = Label(popup.transform,"",21,.015f,.27f,.985f,.97f); code.richText=false;
            code.color = new Color(.6f,.9f,1f); code.raycastTarget = true;
            var codeClick = code.gameObject.AddComponent<Button>(); codeClick.targetGraphic = code;
            codeClick.onClick.AddListener(()=>choices.SetActive(!choices.activeSelf));
            Button(popup.transform,"Change highlighted implementation",.015f,.02f,.66f,.27f,()=>choices.SetActive(!choices.activeSelf));
            Button(popup.transform,"Back to board",.77f,.02f,.985f,.27f,()=>{popup.SetActive(false);choices.SetActive(false);});
            choices = Panel("Patch Choices",frame,new Color(.075f,.13f,.19f),.08f,.48f,.92f,.775f).gameObject;
            for(int i=0;i<3;i++)
            {
                int choice=i;
                var button=Button(choices.transform,"",.015f,.68f-i*.325f,.985f,.98f-i*.325f,()=>Choose(displayOrder[choice]));
                var label=button.GetComponentInChildren<TMP_Text>();
                label.fontSize=21;
                // Filled on Inspect because the exercise changes between openings.
                button.gameObject.name="Patch "+i;
            }
            root.SetActive(false);
        }
        private void LateUpdate()
        {
            if (choices == null || !choices.activeSelf || exercise > 3) return;
            for(int i=0;i<3;i++) choices.transform.Find("Patch "+i).GetComponentInChildren<TMP_Text>().text = WorkshopLesson.Choices[exercise][displayOrder[i]];
        }
        private RectTransform Panel(string name,Transform parent,Color color,float x0,float y0,float x1,float y1)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            go.GetComponent<Image>().color=color;
            var rect=(RectTransform)go.transform; Place(rect,x0,y0,x1,y1);return rect;
        }
        private TMP_Text Label(Transform parent,string text,float size,float x0,float y0,float x1,float y1)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.color=ink;label.text=text;
            label.enableAutoSizing=false;label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode=TextWrappingModes.Normal;
            if(font!=null) label.fontSharedMaterial=font.material;
            Place((RectTransform)go.transform,x0,y0,x1,y1);return label;
        }
        private Button Button(Transform parent,string caption,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(caption,parent,new Color(.1f,.23f,.3f),x0,y0,x1,y1);
            var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=rect.GetComponent<Image>();b.onClick.AddListener(action);
            Label(rect,caption,22,.035f,.035f,.965f,.965f).alignment=TextAlignmentOptions.Center; return b;
        }
        private static void Place(RectTransform rect,float x0,float y0,float x1,float y1)
        {rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private void OnDestroy() { Hide(); if(root!=null) Destroy(root); }
    }
}
