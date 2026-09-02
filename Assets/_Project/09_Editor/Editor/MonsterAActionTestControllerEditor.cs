using GameProject.Monsters;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterAActionTestController))]
public sealed class MonsterAActionTestControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        MonsterActionTestInspectorUtility.DrawInspectorWithOneBasedFrameTimes(serializedObject);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply Monster A Settings To Prefab", GUILayout.Height(32f)))
        {
            MonsterAPrefabApplyUtility.ApplySettingsToPrefab((MonsterAActionTestController)target);
        }

        EditorGUILayout.HelpBox(
            "Edit Monster A settings in this Inspector, then click the button above before testing. " +
            "This button applies to every scene that uses this monster - so does its own auto-sync " +
            "of ActionTest.unity - EXCEPT Max Hp / Respawn Delay / Detect Range / Max Respawn Count, which " +
            "always stay local to this one placed instance and are never touched by Apply.",
            MessageType.Info);
    }
}
