using GameProject.Monsters;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterCActionTestController))]
public sealed class MonsterCActionTestControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        MonsterActionTestInspectorUtility.DrawInspectorWithOneBasedFrameTimes(serializedObject);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply Monster C Settings To Prefab", GUILayout.Height(32f)))
        {
            MonsterCPrefabApplyUtility.ApplySettingsToPrefab((MonsterCActionTestController)target);
        }

        EditorGUILayout.HelpBox(
            "Edit values in this Inspector while not playing, then click the button above. The gameplay " +
            "ActionTest scene is synced automatically - and so is every OTHER scene using this monster, " +
            "EXCEPT Max Hp / Respawn Delay / Detect Range / Max Respawn Count, which always stay local to this " +
            "one placed instance and are never touched by Apply.",
            MessageType.Info);
    }
}
