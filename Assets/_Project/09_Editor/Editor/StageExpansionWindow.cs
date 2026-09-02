using UnityEditor;
using UnityEngine;

/// <summary>
/// Entry point for the multi-stage expansion tools (심화/선택 - 스테이지1 커리큘럼과는 별개의
/// 확장 기능, 2026-08-17 설계). Deliberately a separate submenu from the rest of
/// "Tools/Class Template" so it reads as optional/bonus, not part of the required weekly
/// curriculum tools.
///
/// [MenuItem] methods can't take a runtime parameter, so unlike Stage 1's fixed-name menu
/// items ("Create Background Test Scene"), stage-number-parameterized actions need a small
/// window to type the number into. "Create" needs that number (no scene is open yet to infer
/// it from); the Refresh/Add-style buttons instead infer the stage from whichever scene is
/// currently open (StageSceneBuilder.InferStageFromOpenScene) - same convention as every other
/// Refresh tool in this project - so the number field only matters for Create.
/// </summary>
public class StageExpansionWindow : EditorWindow
{
    private int stageNumber = 2;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Class Template/심화 - 스테이지 확장/스테이지 확장 도구")]
    public static void Open()
    {
        var window = GetWindow<StageExpansionWindow>("스테이지 확장");
        window.minSize = new Vector2(360, 260);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("심화 기능 - 스테이지 확장", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "스테이지1은 원래 커리큘럼 그대로이고 이 도구와 무관합니다.\n" +
            "여기서는 스테이지2 이상을 새로 만들거나(폴더 구조는 미리 04_Art/StudentReplace/Stage{번호}/... 에 준비되어 있어야 함), " +
            "이미 만든 스테이지 씬을 열어둔 상태에서 아트/BGM/날씨 등을 다시 적용합니다.",
            MessageType.Info);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("1) 새 스테이지 만들기", EditorStyles.boldLabel);
        stageNumber = EditorGUILayout.IntField("스테이지 번호 (2 이상)", stageNumber);

        using (new EditorGUI.DisabledScope(stageNumber < 2))
        {
            if (GUILayout.Button($"Stage{Mathf.Max(stageNumber, 2)}_BackgroundTest 씬 만들기"))
            {
                StageSceneBuilder.CreateBackgroundTestScene(stageNumber);
            }
        }

        if (stageNumber < 2)
        {
            EditorGUILayout.HelpBox("스테이지1은 기존 'Create Background Test Scene' 메뉴를 쓰세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("2) 필수 HUD/시스템 한번에 추가", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "HP표시 + 스페셜게이지 + 사망화면 + 일시정지메뉴 + 모바일컨트롤 + 시작배너/클리어화면을 " +
            "한 번에 추가·갱신합니다(현재 열려있는 Stage{번호}_BackgroundTest 기준). 각각은 원래 " +
            "Tools/Class Template 최상위 메뉴에 따로 있던 것들이라 빠뜨리기 쉬워서 여기 하나로 " +
            "묶었습니다 - 하나만 다시 손보고 싶으면 그 최상위 메뉴들을 개별로 써도 됩니다.",
            MessageType.None);

        if (GUILayout.Button("HUD/시스템 한번에 추가"))
        {
            StageHudBundleTool.AddAllHudSystemsToOpenScene();
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("3) 몬스터 프리팹 만들기 (위 스테이지 번호 기준)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Monster1(A 행동)/Monster2(B 행동)/Monster3(C 행동) 3마리를 한 번에 만듭니다 - " +
            "아트/SFX를 바꾼 뒤 다시 눌러도 안전합니다(항상 새로 반영).",
            MessageType.None);

        using (new EditorGUI.DisabledScope(stageNumber < 2))
        {
            if (GUILayout.Button("몬스터 프리팹 3개 만들기/다시 반영"))
            {
                StageMonsterBuilder.BuildAllMonsterPrefabs(stageNumber);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Monster1 씬에 추가"))
            {
                StageMonsterBuilder.AddMonsterToOpenScene(stageNumber, 1);
            }
            if (GUILayout.Button("Monster2 씬에 추가"))
            {
                StageMonsterBuilder.AddMonsterToOpenScene(stageNumber, 2);
            }
            if (GUILayout.Button("Monster3 씬에 추가"))
            {
                StageMonsterBuilder.AddMonsterToOpenScene(stageNumber, 3);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.HelpBox("씬에 추가 버튼은 반드시 해당 스테이지 씬(Stage{번호}_BackgroundTest)을 열어둔 상태에서 누르세요.", MessageType.None);

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("4) 현재 열려있는 스테이지 씬에 적용 (씬 이름으로 스테이지 자동 인식)", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Real Stage Art (바닥/배경/날씨 아트 반영)"))
        {
            StageSceneBuilder.ApplyRealStageArtToOpenScene();
        }

        if (GUILayout.Button("Add Weather Effect To Scene"))
        {
            StageSceneBuilder.AddWeatherEffectToOpenScene();
        }

        if (GUILayout.Button("Add Background Music To Scene"))
        {
            StageSceneBuilder.AddBackgroundMusicToOpenScene();
        }

        if (GUILayout.Button("Wire Auto Level Bounds"))
        {
            StageSceneBuilder.WireAutoLevelBoundsOnOpenScene();
        }

        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("5) 시작 배너 / 클리어 화면만 따로 (현재 열려있는 스테이지 씬 기준)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "게임 시작 배너 + 레벨클리어 오브젝트(맞으면 클리어) + 클리어 화면을 한 번에 추가/갱신합니다 " +
            "(위 2번 버튼에도 포함되어 있음 - 이건 그것만 따로 다시 실행하고 싶을 때용). " +
            "클리어 후 다음 씬은 인스펙터의 StageClearController > Next Scene Name에서 직접 설정하세요 " +
            "(기본값 EndingCutscene - 설정 안 하면 안전하게 엔딩으로 감).",
            MessageType.None);

        if (GUILayout.Button("Add Stage Start & Clear To Scene"))
        {
            StageStartClearBuilder.AddStageStartAndClearToOpenScene();
        }

        if (GUILayout.Button("Toggle Stage Clear Panel Preview (위치 맞춤용, 끄고 저장할 것)"))
        {
            StageStartClearBuilder.ToggleStageClearPanelPreviewOnOpenScene();
        }

        EditorGUILayout.EndScrollView();
    }
}
