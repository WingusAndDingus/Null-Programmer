using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class IntegratedMailInstaller
{
    private static Font font;
    private static readonly Color Ink = new Color32(29, 49, 64, 255);

    [MenuItem("Tools/Null Programmer/Install Integrated Email")]
    public static void Install()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Install the inbox outside Play Mode.", "OK");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "DesktopUI")
        {
            EditorUtility.DisplayDialog("Open DesktopUI", "Open DesktopUI and make it the active scene first.", "OK");
            return;
        }
        GameObject[] objects = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        IntegratedMailInbox existing = objects.Select(o => o.GetComponent<IntegratedMailInbox>()).FirstOrDefault(c => c != null);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Already installed", "The inbox is already installed. Your existing layout has been kept.", "OK");
            return;
        }
        GameObject icon = objects.FirstOrDefault(o => o.name == "pixel mail icon envelope ui_15470108_0")
            ?? objects.FirstOrDefault(o => o.name == "MailIcon");
        EventTrigger trigger = icon != null ? icon.GetComponent<EventTrigger>() : null;
        EventTrigger.Entry click = trigger != null ? trigger.triggers.FirstOrDefault(e => e.eventID == EventTriggerType.PointerClick) : null;
        GameObject window = null;
        if (click != null)
        {
            for (int i = 0; i < click.callback.GetPersistentEventCount(); i++)
            {
                if (click.callback.GetPersistentMethodName(i) == "SetActive")
                    window = click.callback.GetPersistentTarget(i) as GameObject;
            }
        }
        // Supports a scene still pointing to the old OpenInbox callback too.
        if (window == null) window = objects.FirstOrDefault(o => o.name == "Window" && o.GetComponent<MaximizeWindow>() != null);
        SpriteRenderer body = window != null ? window.GetComponent<SpriteRenderer>() : null;
        DraggableWindow drag = window != null ? window.GetComponentInChildren<DraggableWindow>(true) : null;
        SpriteRenderer bar = drag != null ? drag.GetComponent<SpriteRenderer>() : null;
        if (icon == null || trigger == null || click == null || body == null || body.sprite == null ||
            drag == null || drag.WindowRoot != window.transform || bar == null ||
            drag.DesktopBottomLeft == null || drag.DesktopTopRight == null ||
            click.callback.GetPersistentEventCount() != 1)
        {
            EditorUtility.DisplayDialog("Scene setup differs", "Expected the current main-branch mail icon with one Pointer Click callback and a sprite Window with DraggableWindow/desktop bounds. Nothing was changed.", "OK");
            return;
        }
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Integrate mail inbox");
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Remove the old controller so its Awake cannot rebuild an overlay.
            MailInbox legacy = icon.GetComponent<MailInbox>();
            if (legacy != null)
            {
                var serialized = new SerializedObject(legacy);
                var oldCanvas = serialized.FindProperty("canvas")?.objectReferenceValue as Canvas;
                if (oldCanvas != null)
                {
                    Undo.RecordObject(oldCanvas.gameObject, "Hide legacy mail overlay");
                    oldCanvas.gameObject.SetActive(false);
                }
                Undo.DestroyObjectImmediate(legacy);
            }
            IntegratedMailInbox controller = Undo.AddComponent<IntegratedMailInbox>(icon);
            var root = new GameObject("Email Contents", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Add email contents");
            root.transform.SetParent(window.transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            root.AddComponent<MailContentRaycaster>().SetOwner(controller);
            root.AddComponent<MailBringToFront>().SetWindow(drag);
            root.AddComponent<RectMask2D>();

            RectTransform background = Panel("Email background", root.transform, new Color32(238, 243, 246, 255));
            RectTransform list = Panel("Inbox list", background, new Color32(220, 230, 237, 255));
            Layout(list, Vector2.zero, new Vector2(.30f, 1), new Vector2(10, 10), new Vector2(-5, -10));
            Text heading = Label("Inbox heading", list, "Inbox (1 unread)", 25, TextAnchor.MiddleLeft);
            Layout(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(12, -52), new Vector2(-12, -4));
            RectTransform row = Panel("Sample email", list, Color.white);
            Layout(row, new Vector2(0, 1), Vector2.one, new Vector2(8, -252), new Vector2(-8, -64));
            Button emailButton = row.gameObject.AddComponent<Button>();
            emailButton.targetGraphic = row.GetComponent<Image>();
            Text rowText = Label("Summary", row, "", 20, TextAnchor.UpperLeft);
            Layout(rowText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            var colors = emailButton.colors;
            colors.highlightedColor = new Color32(193, 220, 233, 255);
            colors.selectedColor = colors.highlightedColor;
            emailButton.colors = colors;

            RectTransform area = Panel("Message area", background, Color.white);
            Layout(area, new Vector2(.30f, 0), Vector2.one, new Vector2(5, 10), new Vector2(-10, -10));
            Text prompt = Label("Select a message", area, "Select an email to read it.", 24, TextAnchor.MiddleCenter);
            RectTransform pane = Rect("Reading pane", area);
            Layout(pane, Vector2.zero, Vector2.one, new Vector2(18, 15), new Vector2(-18, -15));
            Text subject = Label("Subject", pane, "", 24, TextAnchor.UpperLeft);
            subject.fontStyle = FontStyle.Bold;
            Layout(subject.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -72), Vector2.zero);
            Text from = Label("Sender", pane, "", 18, TextAnchor.MiddleLeft);
            Layout(from.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -108), new Vector2(0, -72));
            RectTransform viewport = Panel("Body viewport", pane, Color.white);
            Layout(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -120));
            viewport.gameObject.AddComponent<RectMask2D>();
            Text message = Label("Email body", viewport, "", 20, TextAnchor.UpperLeft);
            message.rectTransform.pivot = new Vector2(.5f, 1);
            Layout(message.rectTransform, new Vector2(0, 1), Vector2.one, Vector2.zero, new Vector2(-12, 0));
            message.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = message.rectTransform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25;

            var notificationRoot = new GameObject("Mail Notification", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(notificationRoot, scene);
            Undo.RegisterCreatedObjectUndo(notificationRoot, "Add mail notification");
            Canvas notificationCanvas = notificationRoot.GetComponent<Canvas>();
            notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            notificationCanvas.sortingOrder = 500;
            CanvasScaler scaler = notificationRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform badge = Panel("Unread badge", notificationRoot.transform, new Color32(186, 42, 58, 255));
            badge.anchorMin = badge.anchorMax = new Vector2(.5f, .5f);
            badge.sizeDelta = new Vector2(26, 26);
            badge.GetComponent<Image>().raycastTarget = false;
            Text badgeText = Label("Count", badge, "1", 18, TextAnchor.MiddleCenter);
            badgeText.color = Color.white;

            controller.Configure(window, body, bar, drag, canvas, notificationCanvas, badge,
                heading, rowText, subject, from, message, pane.gameObject, prompt.gameObject, emailButton, scroll);
            pane.gameObject.SetActive(false);
            Undo.RecordObject(trigger, "Connect mail icon");
            click.callback = new EventTrigger.TriggerEvent();
            UnityEventTools.AddVoidPersistentListener(click.callback, controller.OpenInbox);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(controller);
            // Existing close/maximize/drag callbacks are deliberately retained.
            Undo.RecordObject(window, "Start with mail closed");
            window.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorUtility.DisplayDialog("Email integrated", "Save the scene (Ctrl+S), then press Play and click the mail icon. Expand Window > Email Contents to edit the UI. Read state survives closing/reopening within this scene session.", "OK");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Installation rolled back", "No partial installation was kept. Copy the first Console error for troubleshooting.", "OK");
        }
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        Layout(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return rect;
    }
    private static RectTransform Panel(string name, Transform parent, Color color)
    {
        RectTransform rect = Rect(name, parent);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }
    private static Text Label(string name, Transform parent, string text, int size, TextAnchor alignment)
    {
        RectTransform rect = Rect(name, parent);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = font; label.text = text; label.color = Ink; label.fontSize = size;
        label.alignment = alignment; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    private static void Layout(RectTransform r, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
    {
        r.anchorMin = min; r.anchorMax = max; r.offsetMin = low; r.offsetMax = high;
    }
}
