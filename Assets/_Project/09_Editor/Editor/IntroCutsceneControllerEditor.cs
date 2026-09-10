using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds a "장 미리보기" (slide preview) field to IntroCutsceneController's Inspector - type a slide
/// number and click 이동 to see it immediately in the Scene/Game view, no Play Mode needed (and no
/// risk of Play Mode discarding the change - see the Title Preview tools for the same reasoning).
/// Works in Play Mode too since JumpToSlide is a plain method with no editor-only branching.
/// </summary>
[CustomEditor(typeof(IntroCutsceneController))]
public class IntroCutsceneControllerEditor : Editor
{
    private int slideNumber = 1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미리보기 (Play 모드 아니어도 동작)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        slideNumber = EditorGUILayout.IntField("이동할 장 번호 (1부터)", slideNumber);
        if (GUILayout.Button("이동", GUILayout.Width(60)))
        {
            var controller = (IntroCutsceneController)target;
            controller.JumpToSlide(slideNumber - 1);

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }

            // 2026-08-12 버그 수정: JumpToSlide()가 slideImage.sprite만 바꾸는 경우(Prev 버튼의
            // 활성 상태가 그대로 유지되는 장 사이 이동, 예: 2→3, 3→4)엔 Unity가 화면을 자동으로
            // 다시 그려주지 않아서 - Prev 버튼이 꺼짐↔켜짐으로 실제 전환될 때만(예: 1→2) 우연히
            // 같이 갱신됐었음. Game/Scene 뷰를 명시적으로 다시 그리도록 강제해서 모든 장 번호에서
            // 똑같이 바로 보이게 한다.
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        EditorGUILayout.EndHorizontal();
    }
}
