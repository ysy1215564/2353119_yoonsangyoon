using System.Collections.Generic;
using System.IO;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single "tune it in ActionTest, then click this" command that reaches every scene-instance
/// value that has no prefab to fall back on - player stats/hitboxes AND the HUD/mobile-control UI
/// layout (2026-08-17, replaces the earlier separate "Sync Player Stats From ActionTest" +
/// "Sync HUD & Mobile Controls Layout From ActionTest" commands after the user found the split
/// confusing - only ran one of the two by mistake and some UI elements silently stayed stale).
/// One button, applied to every scene in one pass (BackgroundTest + every Stage{N}_BackgroundTest).
///
/// UI layout is captured by walking the FULL subtree under each named root (not a hand-maintained
/// flat list of leaf names) - matched between ActionTest and each target scene by relative
/// name-path, so any nested element (a bar's inner fill graphic, a button's own child icon, ...)
/// is covered automatically instead of needing to be remembered and added by name one at a time
/// (exactly what went stale last time: PauseGearButton wasn't in the old flat list at all, and
/// PlayerHpDisplay's own nested "Background" bar wasn't captured even though the root was).
/// </summary>
public static class SyncFromActionTestTool
{
    private const string ActionTestScenePath = "Assets/_Project/00_Scenes/Stages/ActionTest.unity";

    // 이 이름들의 오브젝트 밑에 있는 모든 RectTransform을 통째로(하위 몇 단계든) 동기화한다.
    // MobileControlsCanvas 하나로 조이스틱/점프/공격/대쉬/스페셜차지 버튼이 전부 커버됨.
    // DeathScreenController는 2026-08-18 추가 - 예전에 있던 "Sync Death Screen To BackgroundTest"
    // (ActionTest->BackgroundTest 하드코딩, Stage2/3엔 아예 안 미치던 도구)를 대체.
    // PauseMenuController는 2026-08-24 추가 - 빠져 있던 것을 W06 문서용 스샷을 정리하다 발견했다.
    // 톱니 버튼(PauseGearButton)만 목록에 있고 정작 그 버튼이 여는 메뉴는 없어서, 학생이 ActionTest
    // 에서 일시정지 버튼 6개를 배치해도 BackgroundTest/Stage2/3엔 아무것도 안 갔다("HUD는 ActionTest
    // 에서만 고치세요"라는 수업 규칙이 일시정지에 대해서만 거짓이었음). 사망 화면과 성격이 완전히
    // 같다 - 평소 꺼져 있는 패널, 씬마다 하나씩, 학생이 버튼 위치를 옮김. 꺼져 있어도
    // FindByNameIncludingInactive로 찾고, 복사하는 것은 위치/크기/스케일뿐이라 패널이 켜지지도 않는다.
    private static readonly string[] LayoutRootNames =
    {
        "PlayerHpDisplay",
        "SpecialGauge",
        "PauseGearButton",
        "MobileControlsCanvas",
        "DeathScreenController",
        "PauseMenuController",
    };

    private struct RectLayout
    {
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector3 LocalScale;
    }

    [MenuItem("Tools/Class Template/Sync Player & HUD From ActionTest")]
    public static void SyncPlayerAndHudFromActionTest()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Sync Player & HUD From ActionTest"))
        {
            return;
        }

        var originalScene = SceneManager.GetActiveScene();
        var originalScenePath = originalScene.path;
        if (originalScene.isDirty)
        {
            EditorSceneManager.SaveScene(originalScene);
        }

        if (!File.Exists(ActionTestScenePath))
        {
            Debug.LogWarning("ActionTest.unity를 찾을 수 없습니다.");
            return;
        }

        EditorSceneManager.OpenScene(ActionTestScenePath, OpenSceneMode.Single);
        var sourceScene = SceneManager.GetActiveScene();

        // ---- Capture (ActionTest가 열려있는 지금 시점에만 접근 가능한 것들) --------------------
        var sourceController = Object.FindObjectOfType<PlayerActionTestController>();
        GameObject snapshotObject = null;
        PlayerActionTestController snapshotController = null;
        if (sourceController != null)
        {
            // EditorSceneManager.OpenScene(..., Single)이 곧 이전 씬 오브젝트를 전부 파괴하므로,
            // 원본 참조를 여러 씬에 걸쳐 재사용하면 죽은 참조가 된다(2026-08-17 실기기에서
            // MissingReferenceException으로 확인) - 씬에 안 속한 복제본을 만들어 대신 사용.
            snapshotObject = Object.Instantiate(sourceController.gameObject);
            snapshotObject.hideFlags = HideFlags.HideAndDontSave;
            snapshotController = snapshotObject.GetComponent<PlayerActionTestController>();
        }
        else
        {
            Debug.LogWarning("ActionTest.unity에서 PlayerActionTestController(주인공)를 못 찾았습니다 - 플레이어 스탯은 건너뜁니다.");
        }

        var capturedLayouts = new Dictionary<string, Dictionary<string, RectLayout>>();
        foreach (var rootName in LayoutRootNames)
        {
            var rootObject = FindByNameIncludingInactive(sourceScene, rootName);
            if (rootObject == null)
            {
                continue; // ActionTest에 아직 없는 요소 - 조용히 건너뜀
            }

            capturedLayouts[rootName] = CaptureSubtree(rootObject.transform);
        }

        if (snapshotController == null && capturedLayouts.Count == 0)
        {
            Debug.LogWarning("ActionTest.unity에서 동기화할 게 하나도 없습니다.");
            RestoreOriginalScene(originalScenePath);
            return;
        }

        // ---- Apply --------------------------------------------------------------------------
        var updatedScenes = new List<string>();
        foreach (var targetScenePath in PlayerStatsSyncTool.FindTargetScenePaths())
        {
            var targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            var appliedAny = false;

            if (snapshotController != null)
            {
                var targetController = Object.FindObjectOfType<PlayerActionTestController>();
                if (targetController != null)
                {
                    PlayerStatsSyncTool.CopyAllExceptSceneLocalFields(snapshotController, targetController);

                    // showDebugHpText/showDebugSpecialGauge만 예외: 이 필드들은 "ActionTest 기준으로
                    // 통일"의 대상이 아니라 "에디터 전용 좌하단 디버그 표시는 테스트 씬에서만" 이라는
                    // 별개 규칙이라(2026-08-11 확정, 예전 Sync Player HP Display/Special Gauge To
                    // BackgroundTest가 각각 하던 일) - 여기서 강제로 꺼준다. targetScenePath는
                    // FindTargetScenePaths()가 반환하는 값이라 ActionTest 자신은 절대 포함되지
                    // 않으므로 항상 꺼도 안전함.
                    var targetSo = new SerializedObject(targetController);
                    targetSo.FindProperty("showDebugHpText").boolValue = false;
                    targetSo.FindProperty("showDebugSpecialGauge").boolValue = false;
                    targetSo.ApplyModifiedPropertiesWithoutUndo();

                    appliedAny = true;
                }
            }

            foreach (var kvp in capturedLayouts)
            {
                var targetRootObject = FindByNameIncludingInactive(targetScene, kvp.Key);
                if (targetRootObject == null)
                {
                    continue; // 이 씬엔 아직 이 요소가 없음(Add ... To Scene 안 눌렀을 수 있음) - 건너뜀
                }

                if (ApplySubtree(targetRootObject.transform, kvp.Value))
                {
                    appliedAny = true;
                }

                // PlayerHpDisplay는 MaxHp(플레이어 스탯, 위에서 이미 복사됨)에 따라 칸 수가 바뀌는데,
                // Update()가 다음 에디터 틱에나 도는 것을 기다리지 않고 지금 바로 반영해서 저장되는
                // 씬 파일에 곧장 맞는 칸 수가 들어가게 한다(예전 Sync Player HP Display 도구와 동일).
                if (kvp.Key == "PlayerHpDisplay")
                {
                    targetRootObject.GetComponent<PlayerHpDisplay>()?.ForceRefresh();
                }
            }

            if (appliedAny)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene, targetScenePath);
                updatedScenes.Add(targetScenePath);
            }
        }

        if (snapshotObject != null)
        {
            Object.DestroyImmediate(snapshotObject);
        }

        AssetDatabase.SaveAssets();
        RestoreOriginalScene(originalScenePath);

        Debug.Log(updatedScenes.Count > 0
            ? $"ActionTest 기준으로 반영한 씬: {string.Join(", ", updatedScenes)} (플레이어 스탯 + {capturedLayouts.Count}개 UI 레이아웃 - {string.Join(", ", capturedLayouts.Keys)})."
            : "반영할 대상 씬을 찾지 못했습니다.");
    }

    // ---- Subtree capture / apply ------------------------------------------------------------

    private static Dictionary<string, RectLayout> CaptureSubtree(Transform root)
    {
        var result = new Dictionary<string, RectLayout>();
        CaptureRecursive(root, "", result);
        return result;
    }

    private static void CaptureRecursive(Transform node, string path, Dictionary<string, RectLayout> result)
    {
        var rect = node as RectTransform;
        if (rect != null)
        {
            result[path] = new RectLayout
            {
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                LocalScale = rect.localScale,
            };
        }

        for (var i = 0; i < node.childCount; i++)
        {
            var child = node.GetChild(i);
            CaptureRecursive(child, path + "/" + child.name, result);
        }
    }

    /// <summary>Returns true if at least one node's layout was applied.</summary>
    private static bool ApplySubtree(Transform root, Dictionary<string, RectLayout> captured)
    {
        var applied = false;
        ApplyRecursive(root, "", captured, ref applied);
        return applied;
    }

    private static void ApplyRecursive(Transform node, string path, Dictionary<string, RectLayout> captured, ref bool applied)
    {
        RectLayout layout;
        var rect = node as RectTransform;
        if (rect != null && captured.TryGetValue(path, out layout))
        {
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
            rect.localScale = layout.LocalScale;
            applied = true;
        }

        for (var i = 0; i < node.childCount; i++)
        {
            var child = node.GetChild(i);
            ApplyRecursive(child, path + "/" + child.name, captured, ref applied);
        }
    }

    // ---- Scene search / restore ------------------------------------------------------------

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

    private static void RestoreOriginalScene(string originalScenePath)
    {
        if (!string.IsNullOrEmpty(originalScenePath) && SceneManager.GetActiveScene().path != originalScenePath)
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }
}
