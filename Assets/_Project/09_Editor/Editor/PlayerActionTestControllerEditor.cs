using GameProject.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PlayerActionTestController))]
public sealed class PlayerActionTestControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        MonsterActionTestInspectorUtility.DrawInspectorWithOneBasedFrameTimes(serializedObject);
        var changed = serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply All Player Action Settings", GUILayout.Height(32f)))
        {
            ApplyAllSettings((PlayerActionTestController)target);
        }

        EditorGUILayout.HelpBox(
            "Edit all action settings in this Inspector, then click Apply All before testing the scene.",
            MessageType.Info);

        if (changed)
            EditorSceneManager.MarkSceneDirty(((PlayerActionTestController)target).gameObject.scene);
    }

    private static void ApplyAllSettings(PlayerActionTestController player)
    {
        if (player == null || !player.gameObject.scene.IsValid())
        {
            Debug.LogWarning("Player action settings can only be applied to a scene player.");
            return;
        }

        var scene = player.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied all player action settings to the ActionTest scene.");
    }
}
