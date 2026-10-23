using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DialogueBubbleChecks
{
    [MenuItem("Tools/Null Programmer/Validate Dialogue Bubble Layout")]
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Dialogue Manager/Thought Bubble.prefab");
        var day = AssetDatabase.LoadAssetAtPath<DaySequenceData>("Assets/Scripts/Day Scripts/Day2_Data.asset");
        Require(prefab != null && day != null, "Missing bubble prefab or Day 2 data.");
        var screenSizes = new[]
        {
            new Vector2(360, 800), new Vector2(768, 576), new Vector2(944, 496),
            new Vector2(1280, 720), new Vector2(1920, 1080), new Vector2(2560, 1080)
        };
        var objects = new List<GameObject>();
        var canvasObject = new GameObject("Dialogue Layout Validation", typeof(RectTransform), typeof(Canvas));
        canvasObject.hideFlags = HideFlags.HideAndDontSave;
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        objects.Add(canvasObject);
        int measured = 0;
        try
        {
            foreach (var step in day.steps)
            {
                if (step.choices == null || step.choices.Count == 0) continue;
                // Exercise every authored rank variant, not just the currently unlocked three.
                foreach (var choice in step.choices)
                {
                    var obj = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
                    obj.hideFlags = HideFlags.HideAndDontSave;
                    objects.Add(obj);
                    var bubble = obj.GetComponent<DialogueBubble>();
                    bubble.Setup(new DialogueChoiceOption { text = choice.choiceText, personality = choice.personality },
                        0, Color.blue, null);
                    foreach (Vector2 screen in screenSizes)
                    {
                        float scale = screen.x / 1920f; // Project CanvasScaler: Match Width.
                        Vector2 viewport = new Vector2(screen.x * 0.92f, screen.y * 0.68f) / scale;
                        var plan = DialogueChoiceLayout.Calculate(viewport, 3, scale,
                            (i, width, font) => bubble.MeasureHeight(width, font));
                        bubble.FitToText(plan.width, plan.fontSize, plan.heights[0]);
                        var text = bubble.MessageText;
                        text.ForceMeshUpdate(true, true);
                        Require(!text.isTextOverflowing, "Text clipped: " + choice.choiceText);
                        Require(plan.fontSize * scale >= 13.99f, "Unreadable text size.");
                        Require(text.rectTransform.offsetMin.x >= 0f && text.rectTransform.offsetMax.x <= 0f,
                            "Text extends past the card horizontally.");
                        Require(-text.rectTransform.anchoredPosition.y + text.rectTransform.rect.height <=
                            bubble.LayoutSize.y, "Text extends below its card.");
                        CheckBounds(plan, viewport);
                        measured++;
                    }
                    UnityEngine.Object.DestroyImmediate(obj);
                    objects.RemoveAt(objects.Count - 1);
                }
            }
            // Unequal messages and larger choice sets must also have separated rows.
            foreach (Vector2 screen in screenSizes)
            for (int count = 1; count <= 8; count++)
            {
                float scale = screen.x / 1920f;
                Vector2 viewport = new Vector2(screen.x * 0.92f, screen.y * 0.68f) / scale;
                var plan = DialogueChoiceLayout.Calculate(viewport, count, scale,
                    (i, width, font) => i % 2 == 0 ? 180f : 900f);
                CheckBounds(plan, viewport);
            }
            Require(measured > 0, "No authored choices were tested.");
            Debug.Log("Dialogue bubble layout passed: " + measured +
                " authored text/screen combinations plus unequal-height layouts for 1–8 choices.");
        }
        finally
        {
            foreach (var obj in objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void CheckBounds(DialogueChoiceLayout.Plan plan, Vector2 viewport)
    {
        for (int i = 0; i < plan.positions.Length; i++)
        {
            Vector2 p = plan.positions[i];
            Require(p.x - plan.width * 0.5f >= -viewport.x * 0.5f &&
                p.x + plan.width * 0.5f <= viewport.x * 0.5f, "Card outside viewport width.");
            Require(-p.y >= 0f && -p.y + plan.heights[i] <= plan.contentHeight + 0.01f,
                "Card outside scrollable content.");
            for (int j = 0; j < i; j++)
            {
                bool separatedX = Mathf.Abs(p.x - plan.positions[j].x) >= plan.width;
                bool separatedY = p.y <= plan.positions[j].y - plan.heights[j] ||
                    plan.positions[j].y <= p.y - plan.heights[i];
                Require(separatedX || separatedY, "Overlapping choice cards.");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
