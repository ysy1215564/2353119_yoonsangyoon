using System.IO;
using System.Linq;
using GameProject.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StageClearController.nextSceneName을 텍스트 직접입력 대신 Build Settings에 등록된 씬 목록
/// 드롭다운으로 고르게 한다 - 오타로 씬 전환이 조용히 실패하는 사고를 막기 위한 편의 기능
/// (2026-08-17 스테이지 확장 작업 중 남겨둔 task #4).
///
/// 각 Scene Builder 도구(예: StageSceneBuilder, IntroCutsceneSceneBuilder, ClassTemplateTitleSceneBuilder)의
/// Create/Refresh가 자기가 만든 씬을 EditorBuildSettings.scenes에 자동 등록해두는 기존 관례가 있어서,
/// 그 목록이 곧 "실제로 SceneManager.LoadScene으로 이동 가능한 씬" 목록과 정확히 일치한다 - 따로 폴더를
/// 스캔하거나 새 목록을 관리할 필요가 없다.
///
/// 아직 Build Settings에 없는 값(예: 이 도구가 생기기 전에 손으로 입력해둔 값, 또는 스테이지 씬을 만들기
/// 전에 미리 지정해둔 이름)은 드롭다운을 여는 순간 조용히 지워지면 안 되므로, 목록에 없는 현재 값은
/// "(Build Settings에 없음)" 표시를 붙여 선택지에 그대로 포함시킨다 - 아무것도 안 건드리면 값이 유지된다.
/// </summary>
[CustomEditor(typeof(StageClearController))]
public sealed class StageClearControllerEditor : Editor
{
    private const string NextSceneNamePropertyPath = "nextSceneName";
    private const string MissingSceneSuffix = " (Build Settings에 없음)";
    private const string UnsetLabel = "(선택 안 됨)";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                if (property.propertyPath == NextSceneNamePropertyPath)
                {
                    DrawNextSceneNamePopup(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            enterChildren = false;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawNextSceneNamePopup(SerializedProperty property)
    {
        var sceneNames = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        if (sceneNames.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Build Settings에 등록된 씬이 없습니다. Scene Builder 도구(Create/Refresh)를 먼저 실행하면 자동 등록됩니다.",
                MessageType.Warning);
            EditorGUILayout.PropertyField(property, new GUIContent("Next Scene Name"));
            return;
        }

        string currentValue = property.stringValue;
        int currentIndex = sceneNames.IndexOf(currentValue);

        var displayOptions = sceneNames.ToArray();
        bool prepended = currentIndex < 0;
        if (prepended)
        {
            // 현재 값이 목록에 없음(빈 값 포함) - 지우지 않고 맨 앞에 표시용 항목만 추가.
            string label = string.IsNullOrEmpty(currentValue) ? UnsetLabel : currentValue + MissingSceneSuffix;
            displayOptions = new[] { label }.Concat(displayOptions).ToArray();
            currentIndex = 0;
        }

        int newIndex = EditorGUILayout.Popup(
            new GUIContent("Next Scene Name", "클리어 사운드 재생이 끝나면 자동으로 이동할 씬 - Build Settings에 등록된 씬 중에서 선택(오타 방지)"),
            currentIndex,
            displayOptions);

        if (newIndex == currentIndex)
        {
            return;
        }

        int sceneNamesIndex = prepended ? newIndex - 1 : newIndex;
        if (sceneNamesIndex >= 0 && sceneNamesIndex < sceneNames.Count)
        {
            property.stringValue = sceneNames[sceneNamesIndex];
        }
    }
}
