#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NarrativeMessageData))]
public sealed class NarrativeMessageDrawer : PropertyDrawer
{
    private static readonly string[] Fields = { "contactKey", "contactName", "portrait", "text", "replies", "waitForReply" };
    private bool Visible(SerializedProperty property)
    {
        int separator = property.propertyPath.LastIndexOf('.');
        if (separator < 0) return true;
        var type = property.serializedObject.FindProperty(property.propertyPath.Substring(0, separator) + ".stepType");
        return type == null || (Enum.IsDefined(typeof(NarrativeStepType), "ReceiveMessage") &&
            type.intValue == Convert.ToInt32(Enum.Parse(typeof(NarrativeStepType), "ReceiveMessage")));
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!Visible(property)) return 0;
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
            foreach (string field in Fields)
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(field), true) + EditorGUIUtility.standardVerticalSpacing;
        return height;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!Visible(property)) return;
        EditorGUI.BeginProperty(position, label, property);
        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (string field in Fields)
            {
                var child = property.FindPropertyRelative(field);
                line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                line.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(line, child, true);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
#endif
