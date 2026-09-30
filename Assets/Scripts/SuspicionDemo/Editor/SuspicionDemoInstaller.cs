using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SuspicionDemoInstaller
{
    private static Font font;
    [MenuItem("Tools/Null Programmer/Install Suspicion and Reply Demo")]
    public static void Install()
    {
        if (Application.isPlaying) { Notice("Stop Play Mode before installing."); return; }
        Scene scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var existing = roots.SelectMany(r => r.GetComponentsInChildren<SuspicionDemoUI>(true)).FirstOrDefault();
        if (existing != null) { Selection.activeGameObject = existing.gameObject; Notice("Already installed. Your layout has been kept."); return; }
        var inbox = roots.SelectMany(r => r.GetComponentsInChildren<IntegratedMailInbox>(true)).FirstOrDefault();
        if (scene.name != "DesktopUI" || inbox == null || inbox.WindowObject == null)
        { Notice("Open DesktopUI with the integrated email system installed first."); return; }
        if (!roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Any())
        { Notice("This scene needs its existing EventSystem for UI buttons. Restore it first."); return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install suspicion and reply demo");
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!roots.SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).Any())
            {
                var manager = new GameObject("GameManager");
                SceneManager.MoveGameObjectToScene(manager, scene);
                Undo.RegisterCreatedObjectUndo(manager, "Create GameManager");
                manager.AddComponent<GameManager>();
            }
            var gate = inbox.GetComponent<EmailReplyGate>();
            if (gate == null) gate = Undo.AddComponent<EmailReplyGate>(inbox.gameObject);
            Undo.RecordObject(gate, "Bind reply gate");
            gate.Configure(inbox);
            EditorUtility.SetDirty(gate);
            var root = new GameObject("Suspicion and Reply UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create suspicion UI");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var ui = root.AddComponent<SuspicionDemoUI>();

            RectTransform hud = Panel("Suspicion HUD", root.transform, new Color32(22, 32, 47, 245), false);
            At(hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(365, 98));
            Text label = Label("Suspicion value", hud, "Steve's suspicion: 0/100", 24);
            Stretch(label.rectTransform, new Vector2(14, 53), new Vector2(-14, -10));
            RectTransform track = Panel("Meter background", hud, new Color32(55, 64, 78, 255), false);
            Stretch(track, new Vector2(14, 31), new Vector2(-14, -48));
            RectTransform fill = Panel("Meter fill", track, new Color32(43, 166, 119, 255), false);
            Image fillImage = fill.GetComponent<Image>();
            // Built-in UI sprite allows Image.Type.Filled to produce a horizontal bar.
            fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; fillImage.fillAmount = 0;
            Text warning = Label("Crisis warning", hud, "Steve is suspicious! (Test crisis)", 18);
            warning.color = new Color32(255, 156, 156, 255);
            Stretch(warning.rectTransform, new Vector2(14, 3), new Vector2(-14, -71));
            warning.gameObject.SetActive(false);
            Button reply = Button("Reply now", root.transform, "Reply now - Steve is waiting", new Color32(172, 35, 46, 255), 23);
            At((RectTransform)reply.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(360, 58));
            reply.gameObject.SetActive(false);

            RectTransform tests = Panel("Test controls", root.transform, new Color32(22, 32, 47, 245), false);
            At(tests, Vector2.zero, Vector2.zero, new Vector2(18, 18), new Vector2(580, 104));
            Text testLabel = Label("Test instructions", tests, "DEV TEST: read and close the email to unlock Reply now.", 18);
            Stretch(testLabel.rectTransform, new Vector2(12, 57), new Vector2(-12, -5));
            Button plus = Button("Add suspicion", tests, "+10 suspicion", new Color32(116, 44, 53, 255), 20);
            Button minus = Button("Reduce suspicion", tests, "-10 suspicion", new Color32(40, 94, 82, 255), 20);
            Button reset = Button("Reset reply test", tests, "Reset test", new Color32(66, 79, 98, 255), 20);
            At((RectTransform)plus.transform, Vector2.zero, Vector2.zero, new Vector2(12, 10), new Vector2(175, 42));
            At((RectTransform)minus.transform, Vector2.zero, Vector2.zero, new Vector2(200, 10), new Vector2(175, 42));
            At((RectTransform)reset.transform, Vector2.zero, Vector2.zero, new Vector2(388, 10), new Vector2(175, 42));

            RectTransform modal = Panel("Reply dialogue", root.transform, new Color(0, 0, 0, .72f), true);
            RectTransform card = Panel("Conversation", modal, new Color32(26, 38, 54, 255), true);
            At(card, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 530));
            Text title = Label("Title", card, "REPLY TO STEVE - TEST CONVERSATION", 22);
            Stretch(title.rectTransform, new Vector2(28, 477), new Vector2(-28, -18));
            Text feedback = Label("Previous response", card, "", 23);
            feedback.color = new Color32(250, 205, 130, 255);
            Stretch(feedback.rectTransform, new Vector2(28, 397), new Vector2(-28, -65));
            Text dialogue = Label("Dialogue", card, "", 28);
            Stretch(dialogue.rectTransform, new Vector2(28, 238), new Vector2(-28, -145));
            Button a = Button("Choice A", card, "Choice A", new Color32(49, 79, 110, 255), 23);
            Button b = Button("Choice B", card, "Choice B", new Color32(49, 79, 110, 255), 23);
            At((RectTransform)a.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(844, 70));
            At((RectTransform)b.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 70), new Vector2(844, 70));
            Button later = Button("Reply later", card, "Reply later", new Color32(70, 81, 98, 255), 20);
            At((RectTransform)later.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 15), new Vector2(180, 42));
            var data = new SerializedObject(ui);
            Bind(data, "meterLabel", label); Bind(data, "meterFill", fillImage); Bind(data, "warningLabel", warning);
            Bind(data, "replyButton", reply); Bind(data, "dialoguePanel", modal.gameObject); Bind(data, "dialogueText", dialogue);
            Bind(data, "feedbackText", feedback); Bind(data, "choiceA", a); Bind(data, "choiceB", b);
            Bind(data, "choiceAText", a.GetComponentInChildren<Text>()); Bind(data, "choiceBText", b.GetComponentInChildren<Text>());
            Bind(data, "laterButton", later); Bind(data, "laterText", later.GetComponentInChildren<Text>());
            Bind(data, "testControls", tests.gameObject); Bind(data, "addTest", plus); Bind(data, "subtractTest", minus);
            Bind(data, "resetTest", reset); Bind(data, "testFeedback", testLabel);
            data.ApplyModifiedPropertiesWithoutUndo();
            modal.gameObject.SetActive(false);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Notice("Installed. Save with Ctrl+S. In Play Mode: read the email, close it, then click Reply now. Test buttons are visible in Editor/development builds only.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(ex);
            Notice("Installation rolled back. Copy the first Console error for troubleshooting.");
        }
    }
    private static void Notice(string text) { EditorUtility.DisplayDialog("Suspicion and reply demo", text, "OK"); }
    private static void Bind(SerializedObject data, string name, UnityEngine.Object value) { data.FindProperty(name).objectReferenceValue = value; }
    private static RectTransform Panel(string name, Transform parent, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Stretch(rect, Vector2.zero, Vector2.zero);
        go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = raycast;
        return rect;
    }
    private static Text Label(string name, Transform parent, string value, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font; label.text = value; label.fontSize = size; label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
        Stretch(label.rectTransform, new Vector2(10, 4), new Vector2(-10, -4));
        return label;
    }
    private static Button Button(string name, Transform parent, string text, Color color, int size)
    {
        var r = Panel(name, parent, color, true);
        var button = r.gameObject.AddComponent<Button>();
        button.targetGraphic = r.GetComponent<Image>();
        var label = Label("Label", r, text, size); label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void At(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    { r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = position; r.sizeDelta = size; }
    private static void Stretch(RectTransform r, Vector2 min, Vector2 max)
    { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = min; r.offsetMax = max; }
}
