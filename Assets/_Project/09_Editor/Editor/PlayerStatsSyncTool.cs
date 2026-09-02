using System.Collections.Generic;
using System.IO;
using GameProject.Player;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies every tunable balance field on PlayerActionTestController from ActionTest.unity's
/// Player into every other scene's Player, so "ActionTest 기준으로 통일"(2026-08-17 확정) actually
/// holds: tune something in ActionTest, run this once, and BackgroundTest/Stage2_BackgroundTest/
/// every future StageN_BackgroundTest scene all match it - not just at scene-build time (like a
/// fresh default value would), but any time after, on demand.
///
/// Why this exists: unlike monsters, the player has NO prefab (deliberate, see
/// ActionTestSceneBuilder's own comment on PlayerSfxScenePaths) - every scene's Player is created
/// independently by CreatePlayer(), so a value hand-tuned in one scene's Inspector never reaches
/// any other scene on its own. This is the player-equivalent of the monsters' "Apply Settings To
/// Prefab" - same idea (전부 한 번에 반영), different mechanism (프리팹이 없으니 씬 간 직접 복사).
///
/// Copies EVERY serialized field except a short exclusion list of fields that point at objects
/// that only exist inside one specific scene (groundCheck, the 4 TestSpriteEffect children) -
/// copying those across scenes would silently null out or mis-wire them. Sprite arrays and
/// SfxCue/AudioClip fields are project ASSET references, not scene-local, so those sync safely.
///
/// 2026-08-17: no longer has its own [MenuItem] - folded into SyncFromActionTestTool's single
/// "Sync Everything From ActionTest" command (사용자 확정: 도구가 여러 개로 나뉘어 있어서 일부만
/// 실행하고 헷갈리는 문제가 있었음 - 학생이 기억해야 할 버튼을 하나로 줄임). This class's methods
/// are kept internal and reused from there.
/// </summary>
public static class PlayerStatsSyncTool
{
    private const string StagesFolder = "Assets/_Project/00_Scenes/Stages";

    // Scene-local object references - copying these across scenes would point at the WRONG
    // scene's objects (or go null). Everything else on PlayerActionTestController is either a
    // plain value (float/int/bool/Vector2/Vector3/enum/array-of-float) or a reference to a
    // project ASSET (Sprite/AudioClip via SfxCue) - both safe to copy as-is.
    private static readonly HashSet<string> SceneLocalFieldNames = new HashSet<string>
    {
        "m_Script",
        "groundCheck",
        "attackEffect",
        "specialLaserEffect",
        "specialChargeEffect",
        "specialGroundBurstEffect",
    };

    /// <summary>Applies every tunable field from <paramref name="source"/> (ActionTest's player,
    /// or a scene-independent snapshot clone of it) onto <paramref name="target"/> (some other
    /// scene's player), preserving the target's own scene-local references.</summary>
    internal static void CopyAllExceptSceneLocalFields(PlayerActionTestController source, PlayerActionTestController target)
    {
        var targetSo = new SerializedObject(target);
        var preserved = new Dictionary<string, Object>();
        foreach (var fieldName in SceneLocalFieldNames)
        {
            if (fieldName == "m_Script")
            {
                continue;
            }

            var property = targetSo.FindProperty(fieldName);
            if (property != null)
            {
                preserved[fieldName] = property.objectReferenceValue;
            }
        }

        EditorUtility.CopySerialized(source, target);

        targetSo.Update(); // pull in what CopySerialized just wrote directly onto the component
        foreach (var kvp in preserved)
        {
            var property = targetSo.FindProperty(kvp.Key);
            if (property != null)
            {
                property.objectReferenceValue = kvp.Value;
            }
        }

        targetSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>BackgroundTest.unity(스테이지1) + Stages 폴더의 모든 Stage{N}_BackgroundTest.unity -
    /// 새 스테이지가 추가돼도 이 목록에 코드 수정 없이 자동으로 포함됨.</summary>
    internal static List<string> FindTargetScenePaths()
    {
        var paths = new List<string>();

        var stage1Path = $"{StagesFolder}/BackgroundTest.unity";
        if (File.Exists(stage1Path))
        {
            paths.Add(stage1Path);
        }

        if (Directory.Exists(StagesFolder))
        {
            foreach (var file in Directory.GetFiles(StagesFolder, "Stage*_BackgroundTest.unity", SearchOption.TopDirectoryOnly))
            {
                paths.Add(file.Replace('\\', '/'));
            }
        }

        return paths;
    }
}
