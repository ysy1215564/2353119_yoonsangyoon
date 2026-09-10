using GameProject.Monsters;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterBActionTestController))]
public sealed class MonsterBActionTestControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        MonsterActionTestInspectorUtility.DrawInspectorWithOneBasedFrameTimes(serializedObject);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply Monster B Settings To Prefab", GUILayout.Height(32f)))
        {
            MonsterBPrefabApplyUtility.ApplySettingsToPrefab((MonsterBActionTestController)target);
        }

        EditorGUILayout.HelpBox(
            "Edit values in this Inspector while not playing, then click the button above. The gameplay " +
            "ActionTest scene is synced automatically - and so is every OTHER scene using this monster, " +
            "EXCEPT Max Hp / Respawn Delay / Detect Range / Max Respawn Count, which always stay local to this " +
            "one placed instance and are never touched by Apply.",
            MessageType.Info);
    }
}
