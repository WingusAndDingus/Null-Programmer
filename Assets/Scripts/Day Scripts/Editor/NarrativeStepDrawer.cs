using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NarrativeStep))]
public class NarrativeStepDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Tell Unity to draw the foldout using the stepID string if available
        SerializedProperty stepIDProp = property.FindPropertyRelative("stepID");
        string title = label.text;

        if (stepIDProp != null && !string.IsNullOrEmpty(stepIDProp.stringValue))
        {
            title = $"{stepIDProp.stringValue}";
        }

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
