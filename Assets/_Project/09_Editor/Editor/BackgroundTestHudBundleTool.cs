using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click bundle of every "always needed, order-safe, idempotent" HUD/system add-tool for
/// BackgroundTest.unity(스테이지1) - the Stage1 twin of StageHudBundleTool (2026-08-17, Stage2+
/// 전용). "Create Background Test Scene"은 바닥/배경/카메라/BGM만 만들고, HP표시/스페셜게이지/
/// 사망화면/일시정지메뉴/모바일컨트롤/시작배너·클리어화면까지는 전부 따로따로 "Add ... To Scene"
/// 메뉴를 하나씩 눌러야 했음(스테이지1이 스테이지2+ 시스템보다 먼저 만들어져서 이 번들링이 없었던
/// 것) - 2026-08-18, 도구 정리하면서 같은 패턴을 스테이지1에도 적용.
///
/// 순수 오케스트레이터 - 각 도구의 기존 공개 진입점을 의존 관계가 안 꼬이는 순서로 그대로 호출한다.
/// 이 번들이 감싼 개별 도구는 전과 똑같이 단독으로도 안전하게 재실행 가능(한 가지만 다시 손보고
/// 싶을 때).
///
/// StageHudBundleTool과 같은 이유로 제외: Add HP Item Box To Scene / Apply Real Stage Art / Add
/// Weather Effect / Add Background Music To Scene(스테이지1은 애초에 Create Background Test
/// Scene이 만들 때 날씨/BGM을 이미 자동으로 넣어줘서 빠진 게 아님) / Add Fall Death Zone To Scene
/// (역시 Create Background Test Scene이 이미 포함) / Toggle Stage Clear Panel Preview(수동으로
/// 보고 끄는 동작이라 자동화 대상 아님) / Sync Player & HUD From ActionTest(대상 범위가 "지금 열린
/// 씬"이 아니라 "존재하는 모든 스테이지 씬"이라 성격이 다름).
/// </summary>
public static class BackgroundTestHudBundleTool
{
    [MenuItem("Tools/Class Template/Add All HUD & Systems To Background Test Scene")]
    public static void AddAllHudSystemsToBackgroundTestScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add All HUD & Systems To Background Test Scene"))
        {
            return;
        }

        PlayerHpDisplaySceneTools.AddPlayerHpDisplayToScene();
        SpecialGaugeSceneTools.AddSpecialGaugeToScene();
        DeathScreenSceneTools.AddDeathScreenToScene();
        PauseMenuSceneTools.AddPauseMenuToScene();
        MobileControlsSceneTools.AddMobileControlsToScene();
        StageStartClearSceneTools.AddStageStartAndClearToScene();

        Debug.Log("BackgroundTest: HP표시/스페셜게이지/사망화면/일시정지메뉴/모바일컨트롤/시작배너·클리어화면까지 한 번에 추가·갱신했습니다. " +
            "위치는 전부 대략값이니 Scene 뷰에서 확인하고, 필요하면 개별 'Add ... To Scene' 메뉴로 하나씩 다시 손보세요. 저장(Ctrl+S) 잊지 마세요.");
    }

    [MenuItem("Tools/Class Template/Add All HUD & Systems To Background Test Scene", true)]
    private static bool ValidateAddAllHudSystemsToBackgroundTestScene()
    {
        // 6개 중 가장 좁은 조건(Add Stage Start & Clear)에 맞춤 - ActionTest에선 시작 배너/클리어
        // 오브젝트가 의미가 없어서 그 도구 자체가 BackgroundTest 전용으로 막혀있음.
        return SceneManager.GetActiveScene().name == "BackgroundTest";
    }
}
