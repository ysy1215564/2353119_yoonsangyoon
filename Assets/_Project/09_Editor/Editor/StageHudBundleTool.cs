using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click bundle of every "always needed, order-safe, idempotent" HUD/system add-tool for a
/// Stage2+ scene (심화 - 스테이지 확장, 2026-08-17). Added after the user built Stage3 from
/// scratch and found that Player HP Display/Special Gauge/Death Screen/Pause Menu/Mobile Controls
/// live as generic top-level "Tools/Class Template" menu items (predating the multi-stage system)
/// while everything else needed to build a stage lives inside the "심화 - 스테이지 확장" window -
/// having to jump between the two menus made it easy to forget one. Same root cause, and same fix
/// direction, as why "Sync Player & HUD From ActionTest" replaced two separate sync commands
/// earlier the same day (SyncFromActionTestTool.cs) - "혼자만 실행해서 헷갈린다" solved by making
/// the always-together steps into one button instead of relying on remembering all of them.
///
/// Purely an orchestrator: calls each existing tool's own public entry point, in the order that
/// respects their dependencies (nothing here reads position from anything added later). None of
/// the underlying add logic is duplicated or modified, so every button this wraps is exactly as
/// safe to re-run standalone afterward as it always was (e.g. to fix just one piece).
///
/// Deliberately excludes:
///  - Apply Real Stage Art / Add Weather Effect / Add Background Music / Wire Auto Level Bounds -
///    these are per-stage CONTENT decisions (a stage might have no weather), not "every stage
///    always needs this" scaffolding. Bundling them here would risk silently overriding a
///    deliberate choice instead of just filling in boilerplate.
///  - Sync Player & HUD From ActionTest - its scope is "every stage scene that currently exists",
///    not "the scene open right now", so folding it into a single-scene bundle would blur that.
///  - Toggle Stage Clear Panel Preview - an inherently manual look-then-turn-off action, not
///    something that makes sense to fire automatically.
/// </summary>
public static class StageHudBundleTool
{
    public static void AddAllHudSystemsToOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add All HUD & Systems To Scene"))
        {
            return;
        }

        var stage = StageSceneBuilder.InferStageFromOpenScene();
        if (stage == null)
        {
            return;
        }

        if (stage.IsLegacyStage1)
        {
            Debug.LogWarning("스테이지1은 이 도구 대상이 아닙니다 - 기존 개별 'Add ... To Scene' 메뉴들을 쓰세요.");
            return;
        }

        PlayerHpDisplaySceneTools.AddPlayerHpDisplayToScene();
        SpecialGaugeSceneTools.AddSpecialGaugeToScene();
        DeathScreenSceneTools.AddDeathScreenToScene();
        PauseMenuSceneTools.AddPauseMenuToScene();
        MobileControlsSceneTools.AddMobileControlsToScene();
        StageStartClearBuilder.AddStageStartAndClearToOpenScene();

        Debug.Log($"{stage.DisplayName}: HP표시/스페셜게이지/사망화면/일시정지메뉴/모바일컨트롤/시작배너·클리어화면까지 한 번에 추가·갱신했습니다. " +
            "위치는 전부 대략값이니 Scene 뷰에서 확인하고, 필요하면 개별 'Add ... To Scene' 메뉴로 하나씩 다시 손보세요. 저장(Ctrl+S) 잊지 마세요.");
    }
}
