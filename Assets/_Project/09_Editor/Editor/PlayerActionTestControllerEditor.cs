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

        // 2026-09-08: 문구를 "Edit all action settings ... click Apply All before testing"에서 바꿨다.
        // 버튼 이름이 "Apply All"이라 "모든 씬에 적용"으로 읽히는데, 실제로 하는 일은 이 씬 저장뿐이다.
        // 수업 준비 중 실제로 겪었다 - ActionTest에서 이펙트 위치/크기를 고치고 이 버튼을 누른 뒤
        // BackgroundTest를 열어보고 "왜 다르지?" 가 나왔다. 씬 파일은 손도 닿지 않은 상태였고,
        // 빠진 것은 Sync 실행이었다. 문서(W02 본문 5절 / 인스팩터 10절)에는 정확히 적혀 있었지만
        // 버튼 이름이 만드는 기대를 문서가 이기지 못한다. 몬스터 쪽 같은 자리의 버튼은 반대로
        // 전 씬에 적용되는 물건이라(프리팹이 있음) 학생이 그것부터 배우면 더 헷갈린다.
        // 이름을 바꾸는 쪽은 문서 여러 곳에 박혀 있어 안내문만 고쳤다.
        EditorGUILayout.HelpBox(
            "이 버튼은 「지금 씬 저장」입니다 (Ctrl+S와 같음). 값을 바꿨으면 Play 전에 누르세요.\n" +
            "다른 씬에는 반영되지 않습니다. 배경 테스트 씬 등에도 옮기려면:\n" +
            "Tools > Class Template > Sync Player & HUD From ActionTest",
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
        Debug.Log($"[{scene.name}] 씬을 저장했습니다. 다른 씬에는 반영되지 않습니다 - 옮기려면 Tools > Class Template > Sync Player & HUD From ActionTest 를 실행하세요.");
    }
}
