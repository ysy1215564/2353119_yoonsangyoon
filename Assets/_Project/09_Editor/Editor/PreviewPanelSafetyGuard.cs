using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-safety-net for the "미리보기 토글을 켜놓은 채로 저장/Play해버리는" 사고 (Title 팝업 토글,
/// Toggle Pause Menu Preview, Toggle Stage Clear Panel Preview - 전부 Edit Mode에서 실제 아트를
/// 보면서 배치하기 위해 평소엔 꺼져있는 패널을 강제로 켜두는 도구들). 2026-08-18 추가.
///
/// 배경: 이 패널들을 "눈으로 바로 확인"하는 워크플로우 자체는 계속 필요함(리소스 배치 후 Play 없이
/// 바로 확인) - 그래서 각 Toggle 도구는 손대지 않는다. 문제는 딱 하나, 켜놓은 채로 끄는 걸 잊는
/// 것뿐이었다(Stage3 클리어 패널 - WorkLog 2026-08-17에서 실제로 걸림, StageClearController가
/// "패널이 켜져있는지"가 아니라 별도의 "이미 발동했는지" 플래그로만 재발동을 막아서 Play 시작하자마자
/// 클리어 화면이 떠 있는 채로 시작함). Title Preview는 "모두 닫기" 버튼이 있지만 Pause Menu/
/// Stage Clear Panel Preview는 로그 경고문에만 기대고 있었음 - 사람이 놓치면 그대로 사고.
///
/// 그래서 사람이 뭔가를 더 기억해야 하는 방식(버튼 추가) 대신, Play 진입 직전 + 씬 저장 직전
/// 두 시점에 이 패널들이 켜져 있으면 자동으로 꺼서 콘솔에 로그만 남기는 안전망을 추가한다 - 토글
/// 도구는 지금 쓰는 그대로, 실수 자체가 결과물에 남을 수 없게만 만든다.
/// </summary>
[InitializeOnLoad]
public static class PreviewPanelSafetyGuard
{
    // Title.unity의 팝업 3종 + ModalBlocker(TitleScenePreviewTools), Pause Menu의 PausePanel
    // (PauseMenuSceneTools), Stage Clear의 StageClearPanel(StageStartClearSceneTools /
    // StageStartClearBuilder) - 전부 "평소엔 꺼져있는 게 정상, 미리보기 도구가 임시로 켜두는" 패널.
    private static readonly string[] GuardedPanelNames =
    {
        "ControlsPopup",
        "SettingsPopup",
        "CreditsPopup",
        "ModalBlocker",
        "PausePanel",
        "StageClearPanel",
    };

    static PreviewPanelSafetyGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ForceOffGuardedPanels(SceneManager.GetActiveScene(), "Play 시작");
        }
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        ForceOffGuardedPanels(scene, "씬 저장");
    }

    private static void ForceOffGuardedPanels(Scene scene, string reason)
    {
        if (!scene.IsValid())
        {
            return;
        }

        foreach (var name in GuardedPanelNames)
        {
            var panel = FindByNameIncludingInactive(scene, name);
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
                Debug.Log($"PreviewPanelSafetyGuard: '{name}' 미리보기가 켜져 있어서 {reason} 전에 자동으로 껐습니다.");
            }
        }
    }

    private static GameObject FindByNameIncludingInactive(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindInChildren(root.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var found = FindInChildren(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
