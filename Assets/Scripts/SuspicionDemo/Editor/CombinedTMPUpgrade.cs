using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Converts only the installed email and suspicion UI, in place.</summary>
public static class CombinedTMPUpgrade
{
    private sealed class Binding
    {
        public MonoBehaviour owner;
        public string field;
        public GameObject labelObject;
    }

    [MenuItem("Tools/Null Programmer/Upgrade Email and Suspicion to TextMeshPro")]
    public static void Upgrade()
    {
        if (Application.isPlaying) { Notice("Stop Play Mode before converting."); return; }
        Scene scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var mails = roots.SelectMany(r => r.GetComponentsInChildren<IntegratedMailInbox>(true)).ToArray();
        var demos = roots.SelectMany(r => r.GetComponentsInChildren<SuspicionDemoUI>(true)).ToArray();
        if (mails.Length != 1 || demos.Length != 1)
        { Notice("Open the scene containing exactly one installed email controller and one Suspicion Demo UI. Do not reinstall them."); return; }
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null || font.material == null)
        { Notice("Import TMP Essential Resources using Window > TextMeshPro, then try again."); return; }
        // Bitmap fonts cannot provide the distance-field rendering this upgrade needs.
        if (font.material.shader == null || font.material.shader.name.IndexOf("Distance Field", StringComparison.OrdinalIgnoreCase) < 0)
        {
            var sdf = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (sdf == null || sdf.material == null)
            { Notice("The default TMP font is not an SDF font. Import TMP Essential Resources for LiberationSans SDF, then retry."); return; }
            font = sdf;
        }
        var mailData = new SerializedObject(mails[0]);
        Canvas content = mailData.FindProperty("contentCanvas").objectReferenceValue as Canvas;
        Canvas notification = mailData.FindProperty("notificationCanvas").objectReferenceValue as Canvas;
        if (content == null || notification == null)
        { Notice("The email canvas references are missing. Restore them before converting."); return; }
        Transform[] targets = { content.transform, notification.transform, demos[0].transform };
        var bindings = new List<Binding>();
        try
        {
            Collect(mails[0], new[] { "inboxLabel", "rowLabel", "subjectLabel", "senderLabel", "bodyLabel" }, targets, bindings);
            Collect(demos[0], new[] { "meterLabel", "warningLabel", "dialogueText", "feedbackText", "choiceAText", "choiceBText", "laterText", "testFeedback" }, targets, bindings);
        }
        catch (Exception ex) { Notice(ex.Message + " No objects changed."); return; }
        Text[] labels = targets.SelectMany(t => t.GetComponentsInChildren<Text>(true)).Distinct().ToArray();
        if (labels.Length == 0) { Notice("Already converted. Your existing layout and font sizes were kept."); return; }
        // A separate component could reference a Text object this tool doesn't know how
        // to rebind. The supported UI's references are preflighted above.
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Upgrade email and suspicion to TMP");
        try
        {
            Undo.RecordObject(mails[0], "Reconnect mail labels");
            Undo.RecordObject(demos[0], "Reconnect suspicion labels");
            foreach (Text label in labels) Replace(label, font);
            foreach (var owner in bindings.Select(b => b.owner).Distinct())
            {
                var data = new SerializedObject(owner);
                foreach (Binding b in bindings.Where(b => b.owner == owner))
                {
                    TMP_Text converted = b.labelObject.GetComponent<TMP_Text>();
                    if (converted == null) throw new InvalidOperationException("No TMP component for " + b.field);
                    data.FindProperty(b.field).objectReferenceValue = null;
                    data.FindProperty(b.field + "TMP").objectReferenceValue = converted;
                }
                data.ApplyModifiedProperties();
                EditorUtility.SetDirty(owner);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = demos[0].gameObject;
            Notice("Converted " + labels.Length + " labels to TextMeshPro. Font sizes and RectTransforms were preserved. Save with Ctrl+S, then check email scrolling and the Reply now dialogue. TMP may wrap lines differently.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(ex);
            Notice("Conversion rolled back. Copy the first Console error for troubleshooting.");
        }
    }

    private static void Collect(MonoBehaviour owner, string[] fields, Transform[] targets, List<Binding> result)
    {
        var data = new SerializedObject(owner);
        foreach (string field in fields)
        {
            var legacyProperty = data.FindProperty(field);
            var tmpProperty = data.FindProperty(field + "TMP");
            if (legacyProperty == null || tmpProperty == null)
                throw new InvalidOperationException("Install both updated runtime scripts first: missing " + field + "TMP.");
            var legacy = legacyProperty.objectReferenceValue as Text;
            var tmp = tmpProperty.objectReferenceValue as TMP_Text;
            GameObject go = tmp != null ? tmp.gameObject : legacy != null ? legacy.gameObject : null;
            if (go == null) throw new InvalidOperationException("Restore missing label reference: " + field + ".");
            if (!targets.Any(t => go.transform == t || go.transform.IsChildOf(t)))
                throw new InvalidOperationException("Label " + field + " is outside the installed UI.");
            result.Add(new Binding { owner = owner, field = field, labelObject = go });
        }
    }
    private static void Notice(string message) { EditorUtility.DisplayDialog("TextMeshPro upgrade", message, "OK"); }
    private static void Replace(Text old, TMP_FontAsset font)
    {
        GameObject go = old.gameObject;
        RectTransform rect = old.rectTransform;
        // TMP initialization can modify a RectTransform; explicitly restore it.
        Vector2 min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta;
        Vector3 position = rect.anchoredPosition3D, scale = rect.localScale;
        Quaternion rotation = rect.localRotation;
        string text = old.text;
        float fontSize = old.fontSize;
        Color color = old.color;
        FontStyle style = old.fontStyle;
        TextAnchor anchor = old.alignment;
        bool raycast = old.raycastTarget, richText = old.supportRichText, active = old.enabled;
        bool wrap = old.horizontalOverflow == HorizontalWrapMode.Wrap;
        bool overflow = old.verticalOverflow == VerticalWrapMode.Overflow;
        Undo.RecordObject(rect, "Keep UI layout");
        Undo.DestroyObjectImmediate(old);
        var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = false;
        tmp.color = color;
        tmp.raycastTarget = raycast;
        tmp.richText = richText;
        tmp.enabled = active;
        tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold :
            style == FontStyle.Italic ? FontStyles.Italic :
            style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
        TextAlignmentOptions[] alignments = {
            TextAlignmentOptions.TopLeft, TextAlignmentOptions.Top, TextAlignmentOptions.TopRight,
            TextAlignmentOptions.Left, TextAlignmentOptions.Center, TextAlignmentOptions.Right,
            TextAlignmentOptions.BottomLeft, TextAlignmentOptions.Bottom, TextAlignmentOptions.BottomRight
        };
        tmp.alignment = alignments[(int)anchor];
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        // The existing body viewport supplies clipping and scrolling.
        tmp.overflowMode = overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        tmp.margin = Vector4.zero;
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
        rect.sizeDelta = size; rect.anchoredPosition3D = position;
        rect.localScale = scale; rect.localRotation = rotation;
        EditorUtility.SetDirty(tmp);
    }
}
