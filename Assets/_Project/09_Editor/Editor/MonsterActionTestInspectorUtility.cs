using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MonsterActionTestInspectorUtility
{
    private static readonly HashSet<string> OneBasedFrameTimeProperties = new HashSet<string>
    {
        "idleFrameTimes",
        "walkFrameTimes",
        "attackFrameTimes",
        "attack1FrameTimes",
        "attack2FrameTimes",
        "attack2ChargeFrameTimes",
        "attack2DashFrameTimes",
        "flyFrameTimes",
        "hitFrameTimes",
        "deathFrameTimes"
    };

    public static void DrawInspectorWithOneBasedFrameTimes(SerializedObject serializedObject)
    {
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                if (OneBasedFrameTimeProperties.Contains(property.propertyPath))
                {
                    DrawOneBasedFrameTimeArray(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            enterChildren = false;
        }
    }

    private static void DrawOneBasedFrameTimeArray(SerializedProperty property)
    {
        property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName, true);
        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;
        int newSize = Mathf.Max(0, EditorGUILayout.DelayedIntField("Frame Count", property.arraySize));
        if (newSize != property.arraySize)
            property.arraySize = newSize;

        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element, new GUIContent($"Frame {i + 1}"));
        }

        EditorGUI.indentLevel--;
    }
}
