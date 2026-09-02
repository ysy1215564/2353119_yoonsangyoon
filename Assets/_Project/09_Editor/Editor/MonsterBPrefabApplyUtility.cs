using System.Collections.Generic;
using System.IO;
using GameProject.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MonsterBPrefabApplyUtility
{
    // Every scene that can have its own MonsterB instance(s). Apply always re-syncs every one
    // of them (except the per-instance balance fields in MonsterBalanceFieldNames.PerInstanceOnly),
    // so a change made in ANY of these scenes - ActionTest, BackgroundTest, or the standalone
    // Monster B motion test scene - ends up in all the others too, no matter which instance you
    // clicked Apply from. Add a scene's path here if MonsterB ever gets placed somewhere else.
    private static readonly string[] SyncedScenePaths =
    {
        "Assets/_Project/00_Scenes/Stages/ActionTest.unity",
        "Assets/_Project/00_Scenes/Stages/BackgroundTest.unity",
        "Assets/_Project/00_Scenes/Stages/MonsterB_ActionTest.unity",
    };

    public static void ApplySettingsToPrefab(MonsterBActionTestController instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("Monster B controller is not assigned.");
            return;
        }

        var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(instance.gameObject);
        if (instanceRoot == null)
        {
            Debug.LogWarning("Monster B must be a prefab instance before applying changes.");
            return;
        }

        var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
        var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("Could not locate the Monster B prefab asset.");
            return;
        }

        var serialized = new SerializedObject(instance);
        var property = serialized.GetIterator();
        var enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "m_Script" || property.name == "previewMotion" || MonsterBalanceFieldNames.PerInstanceOnly.Contains(property.name))
                continue;

            PrefabUtility.ApplyPropertyOverride(property, prefabPath, InteractionMode.UserAction);
        }

        AssetDatabase.SaveAssets();
        var syncedScenes = RevertOverridesEverywhere();
        Debug.Log($"Applied Monster B settings to prefab ({prefabPath}) and synced: {string.Join(", ", syncedScenes)}");
    }

    /// <summary>
    /// Forces every MonsterB instance in every scene in SyncedScenePaths back in line with the
    /// prefab (except the per-instance balance fields) - not just the one in ActionTest.unity.
    /// Without this, a placement that picked up its own stray override on some field (e.g. from
    /// testing a value directly in that scene's Inspector) silently keeps ignoring future prefab
    /// changes forever, in whichever scene it happens to live in - see WorkLog 2026-08-07/08 for
    /// two real cases of exactly this (an HP display offset and a Player SFX clip).
    /// </summary>
    private static List<string> RevertOverridesEverywhere()
    {
        var syncedScenes = new List<string>();
        var originalScene = SceneManager.GetActiveScene();
        var originalScenePath = originalScene.path;

        // The loop below reloads scenes with OpenScene(..., Single), including possibly the one
        // we're in right now - save it first so nothing unrelated to this Apply gets silently
        // discarded when it gets reloaded from disk.
        if (originalScene.isDirty)
        {
            EditorSceneManager.SaveScene(originalScene);
        }

        foreach (var scenePath in SyncedScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                continue;
            }

            // Always load in isolation (Single), even if it means reloading the scene we're
            // already in. Having two loaded scenes that each contain an instance of the same
            // prefab confuses PrefabUtility.RevertPropertyOverride - it throws "Cannot apply or
            // revert on any of the objects" (an internal Unity ArgumentException) instead of
            // reverting anything, and can leave the second scene stuck open with unsaved changes.
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Some older scenes (e.g. the standalone motion test scene) predate the fix that
            // makes every new camera get an AudioListener, so SFX would stay silent there even
            // with everything else wired correctly - heal that here too while we're in the scene.
            ActionTestSceneBuilder.EnsureAudioListenerOnMainCamera();
            EditorSceneManager.MarkSceneDirty(scene);

            var controllers = new List<MonsterBActionTestController>();
            foreach (var root in scene.GetRootGameObjects())
            {
                controllers.AddRange(root.GetComponentsInChildren<MonsterBActionTestController>(true));
            }

            foreach (var controller in controllers)
            {
                // A controller that isn't actually connected to any prefab can't be reverted at
                // all - RevertPropertyOverride throws a plain ArgumentException (NOT
                // UnityException) in that case, which would otherwise abort this whole loop. Skip
                // it with a warning instead of crashing.
                if (!PrefabUtility.IsPartOfPrefabInstance(controller.gameObject))
                {
                    Debug.LogWarning($"Skipped {controller.gameObject.name} in {scenePath} - it isn't a prefab instance, so it can't be synced from MonsterB.prefab.");
                    continue;
                }

                var serialized = new SerializedObject(controller);
                var instanceProperty = serialized.GetIterator();
                var enterInstanceChildren = true;
                while (instanceProperty.NextVisible(enterInstanceChildren))
                {
                    enterInstanceChildren = false;
                    if (instanceProperty.name == "m_Script" || instanceProperty.name == "previewMotion" || MonsterBalanceFieldNames.PerInstanceOnly.Contains(instanceProperty.name))
                        continue;

                    try
                    {
                        PrefabUtility.RevertPropertyOverride(instanceProperty, InteractionMode.UserAction);
                    }
                    catch (System.Exception)
                    {
                        // Properties without scene overrides (or otherwise not revertable) are
                        // already as good as they're going to get here - never let one bad
                        // property abort syncing the rest.
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
            if (controllers.Count > 0)
            {
                syncedScenes.Add(scenePath);
            }
        }

        // Restore whatever scene the user was actually looking at.
        if (!string.IsNullOrEmpty(originalScenePath) && SceneManager.GetActiveScene().path != originalScenePath)
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        return syncedScenes;
    }
}
