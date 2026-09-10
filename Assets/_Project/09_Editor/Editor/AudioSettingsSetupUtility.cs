using System.Collections.Generic;
using System.IO;
using GameProject.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires every scene that plays sound into the shared volume system: drops an
/// AudioSettingsBootstrap object in (so PlayerPrefs' saved volume gets applied the moment that
/// scene loads, even when it's opened directly rather than reached via Title -> Start), and
/// routes that scene's "BackgroundMusic" AudioSource (if it has one) through GameAudioMixer's BGM
/// group.
///
/// Run this once after GameAudioMixer.mixer exists (see GameAudioSettings.cs for the one-time manual
/// setup steps) and again any time a new scene gets added that plays sound - same "every synced
/// scene, not just the one you tested in" lesson as the monster PrefabApplyUtility scripts and
/// WirePlayerSfxClips (WorkLog 2026-08-08).
/// </summary>
public static class AudioSettingsSetupUtility
{
    // 고정 목록(항상 존재하는 씬들) + BackgroundTest/Stage2/3.../미래 스테이지는
    // PlayerStatsSyncTool.FindTargetScenePaths()로 매번 다시 스캔 - 2026-08-18 이전엔 여기 이
    // 목록이 전부 하드코딩이라 Stage2_BackgroundTest/Stage3_BackgroundTest가 빠져있었고(둘 다
    // 나중에 추가된 씬이라 이 목록을 갱신하는 걸 잊음), 그 두 씬은 설정 화면에서 조절한 BGM/효과음
    // 볼륨이 직접 진입 시 적용 안 될 수 있는 상태로 방치돼 있었음. Title/IntroCutscene/
    // EndingCutscene은 각자 씬 빌더가 만들 때 이미 부트스트랩을 직접 넣어주므로 여기 목록에는 없어도
    // 됨(이 도구는 그 세 씬을 위한 게 아니라 프리팹이 없어서 수동으로 챙겨야 하는 나머지를 위한 것).
    private static readonly string[] FixedAudioScenePaths =
    {
        "Assets/_Project/00_Scenes/Flow/Title.unity",
        "Assets/_Project/00_Scenes/Stages/ActionTest.unity",
        "Assets/_Project/00_Scenes/Stages/MonsterA_ActionTest.unity",
        "Assets/_Project/00_Scenes/Stages/MonsterB_ActionTest.unity",
        "Assets/_Project/00_Scenes/Stages/MonsterC_ActionTest.unity",
    };

    [MenuItem("Tools/Class Template/Add Audio Settings Bootstrap To All Scenes")]
    public static void AddBootstrapToAllScenes()
    {
        var originalScene = SceneManager.GetActiveScene();
        var originalScenePath = originalScene.path;
        if (originalScene.isDirty)
        {
            EditorSceneManager.SaveScene(originalScene);
        }

        var allAudioScenePaths = new List<string>(FixedAudioScenePaths);
        allAudioScenePaths.AddRange(PlayerStatsSyncTool.FindTargetScenePaths());

        var touchedScenes = new List<string>();
        foreach (var scenePath in allAudioScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                continue;
            }

            // Single mode, one scene at a time - see MonsterAPrefabApplyUtility.RevertOverridesEverywhere
            // for why (Unity throws when two loaded scenes both reference the same asset context).
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = false;

            if (GameObject.Find("AudioSettingsBootstrap") == null)
            {
                new GameObject("AudioSettingsBootstrap", typeof(AudioSettingsBootstrap));
                changed = true;
            }

            var bgmObject = GameObject.Find("BackgroundMusic");
            if (bgmObject != null)
            {
                var source = bgmObject.GetComponent<AudioSource>();
                if (source != null && source.outputAudioMixerGroup != GameAudioSettings.BgmGroup)
                {
                    source.outputAudioMixerGroup = GameAudioSettings.BgmGroup;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                touchedScenes.Add(scenePath);
            }
        }

        if (!string.IsNullOrEmpty(originalScenePath) && SceneManager.GetActiveScene().path != originalScenePath)
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(touchedScenes.Count > 0
            ? $"Audio settings bootstrap added/updated in: {string.Join(", ", touchedScenes)}"
            : "No scenes needed changes (bootstrap already present everywhere, BGM already routed).");
    }
}
