using System;
using System.Collections.Generic;
using System.IO;
using GameProject.Monsters;
using GameProject.Player;
using GameProject.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ActionTestSceneBuilder
{
    private const string ScenePath = "Assets/_Project/00_Scenes/Stages/ActionTest.unity";
    // The player has no prefab (unlike the monsters), so every scene with its own
    // Player_ActionTest object needs to be listed here for Wire Player SFX Clips to reach it -
    // see WirePlayerSfxClips().
    private const string BackgroundTestScenePath = "Assets/_Project/00_Scenes/Stages/BackgroundTest.unity";
    private const string MonsterBMotionTestScenePath = "Assets/_Project/00_Scenes/Stages/MonsterB_ActionTest.unity";
    private const string MonsterCMotionTestScenePath = "Assets/_Project/00_Scenes/Stages/MonsterC_ActionTest.unity";
    private const string ArtRoot = "Assets/_Project/04_Art/StudentReplace";
    private const string SfxRoot = "Assets/_Project/06_Audio/SFX";
    private const string PrefabRoot = "Assets/_Project/02_Prefabs";
    private const string MonsterAPrefabPath = PrefabRoot + "/Monsters/MonsterA.prefab";
    private const string MonsterBPrefabPath = PrefabRoot + "/Monsters/MonsterB.prefab";
    private const string MonsterCPrefabPath = PrefabRoot + "/Monsters/MonsterC.prefab";
    private const float PlayerPixelsPerUnit = 120f;
    private const float EffectPixelsPerUnit = 120f;

    [MenuItem("Tools/Class Template/Create Action Test Scene")]
    public static void CreateActionTestScene()
    {
        // This rebuilds ActionTest.unity from scratch and overwrites whatever is
        // currently saved there - no undo. It's the main scene used for every play
        // test, so re-running it out of habit would wipe any hand-tuning done directly
        // in the scene (not just prefab-synced values). Warn every time the file
        // already exists.
        if (File.Exists(ScenePath) && !ConfirmSceneRebuild(ScenePath))
        {
            return;
        }

        ImportFrameFolder($"{ArtRoot}/Player/idle_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/walk_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/jump_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/jump_attack_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/dash_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/attack1_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/attack2_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/special_charge_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/special_fire_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/hit_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/knockdown_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Player/death_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterA/idle_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterA/walk_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterA/attack1_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterA/hit_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterA/death_frames", true, new Vector2(0.5f, 0f));
        ImportMonsterBFrameFolders();
        ImportMonsterCFrameFolders();
        ImportFrameFolder($"{ArtRoot}/Effects/AttackEffect01_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Effects/AttackEffect02_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Effects/JumpAttackEffect_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Effects/SpecialChargeEffect_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Effects/SpecialLaser_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Effects/SpecialGroundBurst_frames", false, new Vector2(0.5f, 0.5f));
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ActionTest";

        CreateCamera();
        CreateFloor();
        CreatePlayer();
        CreateMonsterA();
        CreateMonsterB();
        CreateMonsterC();
        CreateInstructionCanvas();
        CreateMonsterSelector();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created action test scene: {ScenePath}");
    }

    [MenuItem("Tools/Class Template/Create Or Update MonsterA Prefab")]
    public static void CreateOrUpdateMonsterAPrefabAndActionTestInstance()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var existingMonster = GameObject.Find("MonsterA_ActionTest");
        if (existingMonster == null)
        {
            existingMonster = CreateMonsterAGameObject();
        }

        // Applied unconditionally (not just on the fresh-build branch above) so a newly
        // added serialized field like groundMask actually reaches an already-existing scene
        // instance too, not just a from-scratch one - otherwise re-running this on a scene
        // that already has "MonsterA_ActionTest" silently carries the field's C# default
        // (0 / Nothing) into the prefab instead of the intended value.
        var existingController = existingMonster.GetComponent<MonsterAActionTestController>();
        var existingSo = new SerializedObject(existingController);
        existingSo.FindProperty("groundMask").intValue = LayerMask.GetMask("Default");
        existingSo.ApplyModifiedPropertiesWithoutUndo();
        existingMonster.layer = LayerMask.NameToLayer("Monster"); // same reason - must not share the Ground colliders' layer

        Directory.CreateDirectory(PrefabRoot + "/Monsters");
        PrefabUtility.SaveAsPrefabAsset(existingMonster, MonsterAPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterAPrefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException("MonsterA prefab was not created.", MonsterAPrefabPath);
        }

        if (PrefabUtility.GetCorrespondingObjectFromSource(existingMonster) == null)
        {
            var position = existingMonster.transform.position;
            var rotation = existingMonster.transform.rotation;
            var scale = existingMonster.transform.localScale;
            var siblingIndex = existingMonster.transform.GetSiblingIndex();

            UnityEngine.Object.DestroyImmediate(existingMonster);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "MonsterA_ActionTest";
            instance.transform.SetSiblingIndex(siblingIndex);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created or updated MonsterA prefab: {MonsterAPrefabPath}");
    }

    // ---- Placing monsters freely in whatever scene is open (e.g. BackgroundTest.unity) ---

    [MenuItem("Tools/Class Template/Add MonsterA To Scene")]
    public static void AddMonsterAToScene()
    {
        AddMonsterPrefabToScene(MonsterAPrefabPath, "MonsterA");
    }

    [MenuItem("Tools/Class Template/Add MonsterB To Scene")]
    public static void AddMonsterBToScene()
    {
        AddMonsterPrefabToScene(MonsterBPrefabPath, "MonsterB");
    }

    [MenuItem("Tools/Class Template/Add MonsterC To Scene")]
    public static void AddMonsterCToScene()
    {
        AddMonsterPrefabToScene(MonsterCPrefabPath, "MonsterC");
    }

    /// <summary>
    /// Drops one instance of a monster prefab into whatever scene is currently open, at the
    /// camera's current X (ground-level Y) - just a starting point, not a placement decision.
    /// Drag it in the Scene view (or Ctrl+D to duplicate for more of the same monster) to do
    /// the actual balance/placement work by hand - this command only saves the trip to the
    /// Project window to find the prefab. Safe to run repeatedly; never overwrites anything.
    /// </summary>
    private static void AddMonsterPrefabToScene(string prefabPath, string baseName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"{baseName} prefab not found at {prefabPath}. Build it first (its 'Create Or Update ...' command).");
            return;
        }

        var cameraObject = GameObject.FindWithTag("MainCamera");
        var spawnX = cameraObject != null ? cameraObject.transform.position.x : 0f;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = $"{baseName}_Instance";
        instance.transform.position = new Vector3(spawnX, -3.4f, 0f); // -3.4 = ground top Y, matches every other monster/player spawn in this file

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"{baseName} added to the scene. Drag it in the Scene view (or Ctrl+D to duplicate) to place it, then save the scene (Ctrl+S).");
    }

    // Every scene that has its own Player_ActionTest object (the player isn't a prefab, so
    // there's no single shared source these could all inherit from automatically like the
    // monsters do). Add a new scene's path here if a Player object gets placed in it.
    private static readonly string[] PlayerSfxScenePaths = { ScenePath, BackgroundTestScenePath };

    [MenuItem("Tools/Class Template/Wire Player SFX Clips")]
    public static void WirePlayerSfxClips()
    {
        var updatedScenes = new List<string>();

        foreach (var scenePath in PlayerSfxScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find by component, not by name - BackgroundTestSceneBuilder renames its player
            // GameObject to "Player_BackgroundTest" right after creating it (see CreatePlayer()
            // call there), so a fixed "Player_ActionTest" name lookup only ever finds it in
            // ActionTest.unity.
            var controller = UnityEngine.Object.FindObjectOfType<PlayerActionTestController>();
            if (controller == null)
            {
                Debug.LogWarning($"No PlayerActionTestController found in {scenePath}.");
                continue;
            }

            var so = new SerializedObject(controller);
            SetPlayerSfxClips(so);
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureAudioListenerOnMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            updatedScenes.Add(scenePath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(updatedScenes.Count > 0
            ? $"Wired Player SFX clips into: {string.Join(", ", updatedScenes)}."
            : "Wired Player SFX clips into 0 scenes - no Player_ActionTest object was found anywhere in PlayerSfxScenePaths.");
    }

    [MenuItem("Tools/Class Template/Wire MonsterA SFX Clips")]
    public static void WireMonsterASfxClips()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var monster = GameObject.Find("MonsterA_ActionTest");
        if (monster == null)
        {
            Debug.LogWarning("MonsterA_ActionTest was not found in ActionTest.unity.");
            return;
        }

        var controller = monster.GetComponent<MonsterAActionTestController>();
        if (controller == null)
        {
            Debug.LogWarning("MonsterA_ActionTest has no MonsterAActionTestController.");
            return;
        }

        var so = new SerializedObject(controller);
        SetMonsterASfxClips(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureAudioListenerOnMainCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Wired Monster A SFX clips into ActionTest.unity. Now select MonsterA_ActionTest and click \"Apply Monster A Settings To Prefab\" to push it onto MonsterA.prefab.");
    }

    [MenuItem("Tools/Class Template/Wire MonsterB SFX Clips")]
    public static void WireMonsterBSfxClips()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var monster = GameObject.Find("MonsterB_ActionTest");
        if (monster == null)
        {
            Debug.LogWarning("MonsterB_ActionTest was not found in ActionTest.unity.");
            return;
        }

        var controller = monster.GetComponent<MonsterBActionTestController>();
        if (controller == null)
        {
            Debug.LogWarning("MonsterB_ActionTest has no MonsterBActionTestController.");
            return;
        }

        var so = new SerializedObject(controller);
        SetMonsterBSfxClips(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureAudioListenerOnMainCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Wired Monster B SFX clips into ActionTest.unity. Now select MonsterB_ActionTest and click \"Apply Monster B Settings To Prefab\" to push it onto MonsterB.prefab.");
    }

    private static void SetMonsterBSfxClips(SerializedObject so)
    {
        SetAudioClip(so, "attack1Sfx.clip", $"{SfxRoot}/MonsterB/monsterb_attack1");
        SetAudioClip(so, "attack2Sfx.clip", $"{SfxRoot}/MonsterB/monsterb_attack2");
        SetAudioClip(so, "hitSfx.clip", $"{SfxRoot}/MonsterB/monsterb_hit");
        SetAudioClip(so, "deathSfx.clip", $"{SfxRoot}/MonsterB/monsterb_death");
    }

    [MenuItem("Tools/Class Template/Wire MonsterC SFX Clips")]
    public static void WireMonsterCSfxClips()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var monster = GameObject.Find("MonsterC_ActionTest");
        if (monster == null)
        {
            Debug.LogWarning("MonsterC_ActionTest was not found in ActionTest.unity.");
            return;
        }

        var controller = monster.GetComponent<MonsterCActionTestController>();
        if (controller == null)
        {
            Debug.LogWarning("MonsterC_ActionTest has no MonsterCActionTestController.");
            return;
        }

        var so = new SerializedObject(controller);
        SetMonsterCSfxClips(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureAudioListenerOnMainCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Wired Monster C SFX clips into ActionTest.unity. Now select MonsterC_ActionTest and click \"Apply Monster C Settings To Prefab\" to push it onto MonsterC.prefab.");
    }

    // internal (not private): also called from MonsterA/B/CPrefabApplyUtility.cs while they
    // sweep every synced scene on Apply, so a scene that's missing a listener (e.g. an older
    // motion test scene predating this fix) gets healed automatically instead of staying silent
    // forever - see WorkLog 2026-08-08.
    internal static void EnsureAudioListenerOnMainCamera()
    {
        var cameraObject = GameObject.FindWithTag("MainCamera");
        if (cameraObject == null)
        {
            Debug.LogWarning("No MainCamera-tagged object found; could not add an AudioListener.");
            return;
        }

        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
            Debug.Log("Added missing AudioListener to Main Camera.");
        }
    }

    [MenuItem("Tools/Class Template/Create Or Update MonsterC Prefab")]
    public static void CreateOrUpdateMonsterCPrefabAndActionTestInstance()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ImportMonsterCFrameFolders();
        AssetDatabase.Refresh();

        var existingMonster = GameObject.Find("MonsterC_ActionTest");
        if (existingMonster == null)
        {
            CreateMonsterC();
            existingMonster = GameObject.Find("MonsterC_ActionTest");
        }

        Directory.CreateDirectory(PrefabRoot + "/Monsters");
        PrefabUtility.SaveAsPrefabAsset(existingMonster, MonsterCPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterCPrefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException("MonsterC prefab was not created.", MonsterCPrefabPath);
        }

        if (PrefabUtility.GetCorrespondingObjectFromSource(existingMonster) == null)
        {
            var position = existingMonster.transform.position;
            var rotation = existingMonster.transform.rotation;
            var scale = existingMonster.transform.localScale;
            var siblingIndex = existingMonster.transform.GetSiblingIndex();

            UnityEngine.Object.DestroyImmediate(existingMonster);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "MonsterC_ActionTest";
            instance.transform.SetSiblingIndex(siblingIndex);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
        }

        CreateMonsterSelector();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created or updated MonsterC prefab: {MonsterCPrefabPath}");
    }

    [MenuItem("Tools/Class Template/Create Monster B Motion Test Scene")]
    public static void CreateMonsterBMotionTestScene()
    {
        if (File.Exists(MonsterBMotionTestScenePath) && !ConfirmSceneRebuild(MonsterBMotionTestScenePath))
        {
            return;
        }

        ImportMonsterBFrameFolders();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MonsterB_ActionTest";
        CreateMonsterMotionTestCamera("MonsterB Motion Test Camera");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterBPrefabPath);
        if (prefab == null)
            throw new FileNotFoundException("MonsterB prefab was not found.", MonsterBPrefabPath);

        var monster = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        monster.name = "MonsterB_MotionTest";
        monster.transform.position = new Vector3(0f, -3.4f, 0f);
        monster.AddComponent<MonsterBMotionTestSceneController>();

        var preview = monster.GetComponent<MonsterBMotionTestSceneController>();
        var previewSo = new SerializedObject(preview);
        previewSo.FindProperty("monsterController").objectReferenceValue = monster.GetComponent<MonsterBActionTestController>();
        previewSo.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(MonsterBMotionTestScenePath));
        EditorSceneManager.SaveScene(scene, MonsterBMotionTestScenePath);
        AddSceneToBuildSettings(MonsterBMotionTestScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created Monster B motion test scene: {MonsterBMotionTestScenePath}");
    }

    [MenuItem("Tools/Class Template/Create Monster C Motion Test Scene")]
    public static void CreateMonsterCMotionTestScene()
    {
        if (File.Exists(MonsterCMotionTestScenePath) && !ConfirmSceneRebuild(MonsterCMotionTestScenePath))
        {
            return;
        }

        ImportMonsterCFrameFolders();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MonsterC_ActionTest";
        CreateMonsterMotionTestCamera("MonsterC Motion Test Camera");

        // Instantiate from the shared prefab (like MonsterB's motion test scene already does) -
        // NOT CreateMonsterCGameObject(), which builds a disconnected one-off object with no
        // prefab link at all. MonsterC only became a real prefab after this method was first
        // written, and it never got updated to match; a monster built that way can never receive
        // sprite/SFX/etc. changes from "Apply Monster C Settings To Prefab" (see WorkLog 2026-08-08).
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterCPrefabPath);
        if (prefab == null)
            throw new FileNotFoundException("MonsterC prefab was not found.", MonsterCPrefabPath);

        var monster = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        monster.name = "MonsterC_MotionTest";
        monster.transform.position = new Vector3(0f, -2.6f, 0f);
        monster.AddComponent<MonsterCMotionTestSceneController>();

        var preview = monster.GetComponent<MonsterCMotionTestSceneController>();
        var previewSo = new SerializedObject(preview);
        previewSo.FindProperty("monsterController").objectReferenceValue = monster.GetComponent<MonsterCActionTestController>();
        previewSo.FindProperty("targetIdleSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Player/idle_frames/player_idle_01.png");
        previewSo.ApplyModifiedPropertiesWithoutUndo();

        // The prefab's controlMode is Gameplay (that's what ActionTest.unity/BackgroundTest.unity
        // need) - this scene needs MotionPreview instead, and controlMode is excluded from Apply/
        // Revert (see MonsterCPrefabApplyUtility) precisely so this local override sticks.
        var controllerSo = new SerializedObject(monster.GetComponent<MonsterCActionTestController>());
        controllerSo.FindProperty("controlMode").enumValueIndex = 0; // ControlMode.MotionPreview
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(MonsterCMotionTestScenePath));
        EditorSceneManager.SaveScene(scene, MonsterCMotionTestScenePath);
        AddSceneToBuildSettings(MonsterCMotionTestScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created Monster C motion test scene: {MonsterCMotionTestScenePath}");
    }

    [MenuItem("Tools/Class Template/Create Or Update MonsterC Action Test")]
    public static void CreateOrUpdateMonsterCActionTestInstance()
    {
        if (!ConfirmMonsterCActionTestRebuild())
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ImportMonsterCFrameFolders();
        AssetDatabase.Refresh();

        CreateMonsterC();
        CreateMonsterSelector();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created or updated MonsterC action test instance and selector.");
    }

    private static void CreateMonsterMotionTestCamera(string cameraName)
    {
        var cameraObject = new GameObject(cameraName, typeof(Camera), typeof(AudioListener));
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.82f, 0.86f, 0.9f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.tag = "MainCamera";
    }

    private static void ImportMonsterBFrameFolders()
    {
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/idle_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/walk_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/attack1_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/attack2_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/hit_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/death_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/attack1_effect_frames", false, new Vector2(0.5f, 0.5f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterB/attack2_effect_frames", false, new Vector2(0.5f, 0.5f));
    }

    private static void ImportMonsterCFrameFolders()
    {
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/idle_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/fly_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/attack1_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/attack2_charge_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/attack2_dash_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/hit_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/death_frames", true, new Vector2(0.5f, 0f));
        ImportFrameFolder($"{ArtRoot}/Monsters/MonsterC/Effects/Projectile", false, new Vector2(0.5f, 0.5f));
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.82f, 0.82f, 0.82f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateFloor()
    {
        var floor = new GameObject("Flat Test Floor", typeof(SpriteRenderer), typeof(BoxCollider2D));
        floor.transform.position = new Vector3(0f, -4.4f, 0f);
        floor.transform.localScale = new Vector3(32f, 2f, 1f);
        floor.layer = LayerMask.NameToLayer("Default");

        var renderer = floor.GetComponent<SpriteRenderer>();
        renderer.sprite = CreateRuntimeSquareSprite();
        renderer.color = new Color(0.45f, 0.45f, 0.45f);
        renderer.sortingOrder = -5;

        var collider = floor.GetComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static Sprite CreateRuntimeSquareSprite()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "RuntimeSquare";
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    /// <summary>
    /// Builds a fully-wired player object (sprites, physics, SFX) at the standard
    /// ActionTest scale. Internal so other scene builders (e.g. BackgroundTestSceneBuilder)
    /// can reuse the exact same setup instead of duplicating it.
    /// </summary>
    internal static GameObject CreatePlayer()
    {
        var player = new GameObject("Player_ActionTest", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(PlayerActionTestController));
        player.transform.position = new Vector3(-3f, -3.4f, 0f);

        var renderer = player.GetComponent<SpriteRenderer>();
        var idleFrames = LoadFramesFromFolder($"{ArtRoot}/Player/idle_frames");
        renderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;
        renderer.sortingOrder = 10;

        var body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 2.2f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        var collider = player.GetComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.95f, 2.5f);
        collider.offset = new Vector2(0f, 1.25f);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        var attackEffect = CreateEffectObject("AttackEffect", player.transform, 20);
        var specialCharge = CreateEffectObject("SpecialChargeEffect", player.transform, 21);
        var specialLaser = CreateEffectObject("SpecialLaserEffect", player.transform, 18);
        var specialGroundBurst = CreateEffectObject("SpecialGroundBurstEffect", player.transform, 19);

        var controller = player.GetComponent<PlayerActionTestController>();
        var so = new SerializedObject(controller);
        SetObject(so, "groundCheck", groundCheck.transform);
        so.FindProperty("groundMask").intValue = LayerMask.GetMask("Default");

        SetSpritesFromFolder(so, "idleFrames", $"{ArtRoot}/Player/idle_frames");
        SetSpritesFromFolder(so, "walkFrames", $"{ArtRoot}/Player/walk_frames");
        SetSpritesFromFolder(so, "jumpFrames", $"{ArtRoot}/Player/jump_frames");
        SetSpritesFromFolder(so, "jumpAttackFrames", $"{ArtRoot}/Player/jump_attack_frames");
        SetSpritesFromFolder(so, "dashFrames", $"{ArtRoot}/Player/dash_frames");
        SetSpritesFromFolder(so, "attack1Frames", $"{ArtRoot}/Player/attack1_frames");
        SetSpritesFromFiles(
            so,
            "attack2Frames",
            $"{ArtRoot}/Player/attack2_frames",
            "player_attack2_01.png",
            "player_attack2_02.png",
            "player_attack2_03.png",
            "player_attack2_04.png");
        SetSpritesFromFolder(so, "specialChargeFrames", $"{ArtRoot}/Player/special_charge_frames");
        SetSpritesFromFolder(so, "specialFireFrames", $"{ArtRoot}/Player/special_fire_frames");
        SetSpritesFromFiles(
            so,
            "hitFrames",
            $"{ArtRoot}/Player/hit_frames",
            "player_hit_01.png");
        SetSpritesFromFolder(so, "knockdownFrames", $"{ArtRoot}/Player/knockdown_frames");
        SetSpritesFromFolder(so, "deathFrames", $"{ArtRoot}/Player/death_frames");

        SetObject(so, "attackEffect", attackEffect);
        SetObject(so, "specialLaserEffect", specialLaser);
        SetObject(so, "specialChargeEffect", specialCharge);
        SetObject(so, "specialGroundBurstEffect", specialGroundBurst);
        SetSpritesFromFolder(so, "attackEffect1Frames", $"{ArtRoot}/Effects/AttackEffect01_frames");
        SetSpritesFromFolder(so, "attackEffect2Frames", $"{ArtRoot}/Effects/AttackEffect02_frames");
        SetSpritesFromFolder(so, "jumpAttackEffectFrames", $"{ArtRoot}/Effects/JumpAttackEffect_frames");
        SetSpritesFromFolder(so, "specialChargeEffectFrames", $"{ArtRoot}/Effects/SpecialChargeEffect_frames");
        SetSpritesFromFolder(so, "specialLaserFrames", $"{ArtRoot}/Effects/SpecialLaser_frames");
        SetSpritesFromFolder(so, "specialGroundBurstFrames", $"{ArtRoot}/Effects/SpecialGroundBurst_frames");
        so.FindProperty("attack1DamageFrame").intValue = 3;
        so.FindProperty("attack2DamageFrame").intValue = 2;
        so.FindProperty("specialDamageFrame").intValue = 3;
        so.FindProperty("specialLaserStartFrame").intValue = 4;
        so.FindProperty("specialGroundBurstStartFrame").intValue = 4;

        SetPlayerSfxClips(so);

        so.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static void SetPlayerSfxClips(SerializedObject so)
    {
        SetAudioClip(so, "attack1SwingSfx.clip", $"{SfxRoot}/Player/player_attack1_swing");
        SetAudioClip(so, "attack2SwingSfx.clip", $"{SfxRoot}/Player/player_attack2_swing");
        SetAudioClip(so, "jumpSfx.clip", $"{SfxRoot}/Player/player_jump");
        SetAudioClip(so, "dashSfx.clip", $"{SfxRoot}/Player/player_dash");
        SetAudioClip(so, "hitSfx.clip", $"{SfxRoot}/Player/player_hit");
        SetAudioClip(so, "heavyHitSfx.clip", $"{SfxRoot}/Player/player_heavy_hit");
        SetAudioClip(so, "deathSfx.clip", $"{SfxRoot}/Player/player_death");
        SetAudioClip(so, "specialChargeLoopSfx.clip", $"{SfxRoot}/Player/player_special_charge_loop");
        SetAudioClip(so, "specialFireSfx.clip", $"{SfxRoot}/Player/player_special_fire");
        SetAudioClip(so, "jumpAttackSfx.clip", $"{SfxRoot}/Player/player_jump_attack");
    }

    private static readonly string[] AudioFileExtensions = { ".wav", ".ogg", ".mp3", ".aiff", ".aif" };

    // internal (not private): reused by StageMonsterBuilder.cs for Stage 2+'s monster prefab
    // pipeline (2026-08-17) - pure utility, no Stage1-specific behavior, so widening visibility
    // carries zero risk to Stage1's own tools.
    internal static void SetAudioClip(SerializedObject so, string propertyName, string basePathWithoutExtension)
    {
        var clip = FindAudioClipByBaseName(basePathWithoutExtension);
        var property = so.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"SFX property not found: {propertyName}");
            return;
        }

        property.objectReferenceValue = clip;
    }

    /// <summary>
    /// Resolves a clip by base filename regardless of extension (wav/ogg/mp3/...), the same way
    /// FindBgmClip() scans by folder instead of one fixed name - so swapping
    /// "player_attack1_swing.wav" for a re-exported "player_attack1_swing.ogg" (a different
    /// asset GUID, not an in-place overwrite) still gets picked up by Wire/Create without
    /// editing this file. Warns (doesn't fail) if more than one extension exists for the same
    /// base name and picks the first alphabetically - delete the stale one to avoid ambiguity.
    /// </summary>
    /// <param name="warnIfMissing">
    /// false면 파일이 없을 때 조용히 null을 돌려준다. 효과음이 선택 사항인 곳(HP 상자)에서 쓴다 -
    /// 아직 안 넣은 것이 정상인 파일에 매번 경고를 띄우면 진짜 경고가 묻힌다.
    /// </param>
    internal static AudioClip FindAudioClipByBaseName(string basePathWithoutExtension, bool warnIfMissing = true)
    {
        var directory = Path.GetDirectoryName(basePathWithoutExtension)?.Replace('\\', '/');
        var baseName = Path.GetFileName(basePathWithoutExtension);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var file in Directory.GetFiles(directory))
        {
            if (Path.GetFileNameWithoutExtension(file) != baseName)
            {
                continue;
            }

            if (Array.IndexOf(AudioFileExtensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
            {
                continue;
            }

            candidates.Add(file.Replace('\\', '/'));
        }

        if (candidates.Count == 0)
        {
            if (warnIfMissing)
            {
                Debug.LogWarning($"No audio file found for \"{baseName}\" in {directory}.");
            }

            return null;
        }

        candidates.Sort();
        if (candidates.Count > 1)
        {
            var names = string.Join(", ", candidates.ConvertAll(Path.GetFileName));
            Debug.LogWarning($"Multiple audio files found for \"{baseName}\" ({names}); using {Path.GetFileName(candidates[0])}. Delete the others to avoid ambiguity.");
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>(candidates[0]);
    }

    private static TestSpriteEffect CreateEffectObject(string name, Transform parent, int sortingOrder)
    {
        var effect = new GameObject(name, typeof(SpriteRenderer), typeof(TestSpriteEffect));
        effect.transform.SetParent(parent, false);

        var renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        return effect.GetComponent<TestSpriteEffect>();
    }

    private static void CreateMonsterA()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterAPrefabPath);
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "MonsterA_ActionTest";
            instance.transform.position = new Vector3(2.8f, -3.4f, 0f);
            instance.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            return;
        }

        CreateMonsterAGameObject();
    }

    private static GameObject CreateMonsterAGameObject()
    {
        var monster = new GameObject("MonsterA_ActionTest", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(MonsterAActionTestController));
        monster.transform.position = new Vector3(2.8f, -3.4f, 0f);
        monster.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        // Own body/hurtbox collider must NOT share the Ground colliders' "Default" layer, or
        // MonsterGroundGuard's downward ground-check raycast (which also targets "Default")
        // can hit the monster's own collider and always report "ground found" - silently
        // defeating the never-fall-into-a-gap guard with no error.
        monster.layer = LayerMask.NameToLayer("Monster");

        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = LoadFramesFromFolder($"{ArtRoot}/Monsters/MonsterA/idle_frames");
        var attackFrames = LoadFramesFromFolder($"{ArtRoot}/Monsters/MonsterA/attack1_frames");
        var walkFrames = LoadFramesFromFolder($"{ArtRoot}/Monsters/MonsterA/walk_frames");
        var previewFrames = idleFrames.Length > 0 ? idleFrames : walkFrames.Length > 0 ? walkFrames : attackFrames;
        renderer.sprite = previewFrames.Length > 0 ? previewFrames[0] : null;
        renderer.sortingOrder = 9;

        var collider = monster.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.35f, 1.0f);
        collider.offset = new Vector2(0f, 0.55f);
        collider.isTrigger = false;

        var controller = monster.GetComponent<MonsterAActionTestController>();
        var so = new SerializedObject(controller);
        SetSpritesFromFolder(so, "idleFrames", $"{ArtRoot}/Monsters/MonsterA/idle_frames");
        SetSpritesFromFolder(so, "walkFrames", $"{ArtRoot}/Monsters/MonsterA/walk_frames");
        SetSpritesFromFolder(so, "attackFrames", $"{ArtRoot}/Monsters/MonsterA/attack1_frames");
        SetSpritesFromFolder(so, "hitFrames", $"{ArtRoot}/Monsters/MonsterA/hit_frames");
        SetSpritesFromFolder(so, "deathFrames", $"{ArtRoot}/Monsters/MonsterA/death_frames");
        SetMonsterASfxClips(so);
        so.FindProperty("groundMask").intValue = LayerMask.GetMask("Default"); // matches the Ground prefabs' Collision layer - see MonsterGroundGuard
        so.ApplyModifiedPropertiesWithoutUndo();

        return monster;
    }

    private static void SetMonsterASfxClips(SerializedObject so)
    {
        SetAudioClip(so, "attackSfx.clip", $"{SfxRoot}/MonsterA/monstera_attack");
        SetAudioClip(so, "hitSfx.clip", $"{SfxRoot}/MonsterA/monstera_hit");
        SetAudioClip(so, "deathSfx.clip", $"{SfxRoot}/MonsterA/monstera_death");
    }


    private static void CreateMonsterB()
    {
        var existing = GameObject.Find("MonsterB_ActionTest");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterBPrefabPath);
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "MonsterB_ActionTest";
            instance.transform.position = new Vector3(5.5f, -3.4f, 0f);
            instance.transform.localScale = new Vector3(0.99f, 0.99f, 1f);
            return;
        }

        CreateMonsterBGameObject();
    }

    private static GameObject CreateMonsterBGameObject()
    {
        var monster = new GameObject(
            "MonsterB_ActionTest",
            typeof(SpriteRenderer),
            typeof(BoxCollider2D),
            typeof(MonsterBActionTestController));
        monster.transform.position = new Vector3(5.5f, -3.4f, 0f);
        monster.transform.localScale = new Vector3(0.99f, 0.99f, 1f);
        // See MonsterA's CreateMonsterAGameObject() for why this can't stay on "Default".
        monster.layer = LayerMask.NameToLayer("Monster");

        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = LoadFramesFromFolder($"{ArtRoot}/Monsters/MonsterB/idle_frames");
        renderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;
        renderer.sortingOrder = 8;

        var collider = monster.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(2.8f, 3.0f);
        collider.offset = new Vector2(0f, 1.5f);
        collider.isTrigger = false;

        var attack1EffectObject = new GameObject("MonsterB_Attack1_GroundImpactEffect", typeof(SpriteRenderer));
        attack1EffectObject.transform.SetParent(monster.transform, false);
        var attack1EffectRenderer = attack1EffectObject.GetComponent<SpriteRenderer>();
        attack1EffectRenderer.sortingOrder = 12;
        attack1EffectRenderer.enabled = false;

        var attack2EffectObject = new GameObject("MonsterB_Attack2_LaserEffect", typeof(SpriteRenderer));
        attack2EffectObject.transform.SetParent(monster.transform, false);
        var attack2EffectRenderer = attack2EffectObject.GetComponent<SpriteRenderer>();
        attack2EffectRenderer.sortingOrder = 12;
        attack2EffectRenderer.enabled = false;

        var controller = monster.GetComponent<MonsterBActionTestController>();
        var so = new SerializedObject(controller);
        SetSpritesFromFolder(so, "idleFrames", $"{ArtRoot}/Monsters/MonsterB/idle_frames");
        SetSpritesFromFolder(so, "walkFrames", $"{ArtRoot}/Monsters/MonsterB/walk_frames");
        SetSpritesFromFolder(so, "attack1Frames", $"{ArtRoot}/Monsters/MonsterB/attack1_frames");
        SetSpritesFromFolder(so, "attack2Frames", $"{ArtRoot}/Monsters/MonsterB/attack2_frames");
        SetSpritesFromFolder(so, "hitFrames", $"{ArtRoot}/Monsters/MonsterB/hit_frames");
        SetSpritesFromFolder(so, "deathFrames", $"{ArtRoot}/Monsters/MonsterB/death_frames");
        SetObject(so, "attack1EffectRenderer", attack1EffectRenderer);
        SetObject(so, "attack2EffectRenderer", attack2EffectRenderer);
        SetSpritesFromFolder(so, "attack1EffectFrames", $"{ArtRoot}/Monsters/MonsterB/attack1_effect_frames");
        SetSpritesFromFolder(so, "attack2EffectFrames", $"{ArtRoot}/Monsters/MonsterB/attack2_effect_frames");
        so.FindProperty("attack1EffectStartFrame").intValue = 15;
        so.FindProperty("attack2EffectStartFrame").intValue = 15;
        so.FindProperty("attack1EffectOffset").vector3Value = new Vector3(-2.6f, 0.75f, 0f);
        so.FindProperty("attack2EffectOffset").vector3Value = new Vector3(-2.5f, 2.2f, 0f);
        so.FindProperty("attack1EffectScale").vector3Value = new Vector3(0.9f, 0.9f, 1f);
        so.FindProperty("attack2EffectScale").vector3Value = new Vector3(0.9f, 0.9f, 1f);
        SetMonsterBSfxClips(so);
        so.FindProperty("groundMask").intValue = LayerMask.GetMask("Default"); // matches the Ground prefabs' Collision layer - see MonsterGroundGuard
        so.ApplyModifiedPropertiesWithoutUndo();

        return monster;
    }

    private static GameObject CreateMonsterCGameObject(string objectName, Vector3 position)
    {
        var monster = new GameObject(
            objectName,
            typeof(SpriteRenderer),
            typeof(BoxCollider2D),
            typeof(MonsterCActionTestController));
        monster.transform.position = position;
        monster.transform.localScale = Vector3.one;

        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = LoadFramesFromFolder($"{ArtRoot}/Monsters/MonsterC/idle_frames");
        renderer.sprite = idleFrames.Length > 0 ? idleFrames[0] : null;
        renderer.sortingOrder = 8;

        var collider = monster.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.6f, 1.6f);
        collider.offset = new Vector2(0f, 0.9f);
        collider.isTrigger = true;

        var controller = monster.GetComponent<MonsterCActionTestController>();
        var so = new SerializedObject(controller);
        SetObject(so, "spriteRenderer", renderer);
        SetSpritesFromFolder(so, "idleFrames", $"{ArtRoot}/Monsters/MonsterC/idle_frames");
        SetSpritesFromFolder(so, "flyFrames", $"{ArtRoot}/Monsters/MonsterC/fly_frames");
        SetSpritesFromFolder(so, "attack1Frames", $"{ArtRoot}/Monsters/MonsterC/attack1_frames");
        SetSpritesFromFolder(so, "attack2ChargeFrames", $"{ArtRoot}/Monsters/MonsterC/attack2_charge_frames");
        SetSpritesFromFolder(so, "attack2DashFrames", $"{ArtRoot}/Monsters/MonsterC/attack2_dash_frames");
        SetSpritesFromFolder(so, "hitFrames", $"{ArtRoot}/Monsters/MonsterC/hit_frames");
        SetSpritesFromFolder(so, "deathFrames", $"{ArtRoot}/Monsters/MonsterC/death_frames");
        MatchFrameTimeCountToSpriteCount(so, "attack1FrameTimes", "attack1Frames");
        MatchFrameTimeCountToSpriteCount(so, "attack2ChargeFrameTimes", "attack2ChargeFrames");
        MatchFrameTimeCountToSpriteCount(so, "attack2DashFrameTimes", "attack2DashFrames");
        SetSpritesFromFolder(so, "projectileFrames", $"{ArtRoot}/Monsters/MonsterC/Effects/Projectile");
        so.FindProperty("projectileSpawnOffset").vector3Value = new Vector3(-0.9f, 1.95f, 0f);
        so.FindProperty("projectileTargetAimOffset").vector3Value = new Vector3(0f, 1.25f, 0f);
        so.FindProperty("projectileScale").vector3Value = new Vector3(0.3f, 0.3f, 1f);
        so.FindProperty("projectileSpeed").floatValue = 14f;
        so.FindProperty("attack1ProjectileFrame").intValue = 3;
        so.FindProperty("projectileAnimationFramesPerSecond").floatValue = 12f;
        so.FindProperty("projectileFloorMask").intValue = LayerMask.GetMask("Default");
        so.FindProperty("projectileColliderRadius").floatValue = 0.28f;
        so.FindProperty("attack2TargetAimOffset").vector3Value = new Vector3(0f, 1.25f, 0f);
        so.FindProperty("attack2DashSpeed").floatValue = 12f;
        so.FindProperty("attack2ReturnSpeed").floatValue = 10f;
        so.FindProperty("attack2MaxDashSeconds").floatValue = 1.5f;
        SetMonsterCSfxClips(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        return monster;
    }

    private static void SetMonsterCSfxClips(SerializedObject so)
    {
        SetAudioClip(so, "attack1Sfx.clip", $"{SfxRoot}/MonsterC/monsterc_attack1");
        SetAudioClip(so, "attack2Sfx.clip", $"{SfxRoot}/MonsterC/monsterc_attack2");
        SetAudioClip(so, "hitSfx.clip", $"{SfxRoot}/MonsterC/monsterc_hit");
        SetAudioClip(so, "deathSfx.clip", $"{SfxRoot}/MonsterC/monsterc_death");
    }

    private static void CreateMonsterC()
    {
        var existing = GameObject.Find("MonsterC_ActionTest");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var monster = CreateMonsterCGameObject("MonsterC_ActionTest", new Vector3(4.8f, -0.8f, 0f));
        var controller = monster.GetComponent<MonsterCActionTestController>();
        var so = new SerializedObject(controller);
        so.FindProperty("controlMode").enumValueIndex = 1;
        so.FindProperty("maxHp").intValue = 3;
        so.FindProperty("detectRange").floatValue = 7f;
        so.FindProperty("attackRange").floatValue = 6f;
        so.FindProperty("attackCooldown").floatValue = 1.4f;
        so.FindProperty("roamArea").vector2Value = new Vector2(3.4f, 1.2f);
        so.FindProperty("roamSpeed").floatValue = 1.15f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateMonsterSelector()
    {
        var existing = GameObject.Find("ActionTestMonsterSelector");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var selectorObject = new GameObject(
            "ActionTestMonsterSelector",
            typeof(ActionTestMonsterSelector));
        var serializedSelector =
            new SerializedObject(selectorObject.GetComponent<ActionTestMonsterSelector>());

        SetObject(
            serializedSelector,
            "playerTemplate",
            GameObject.Find("Player_ActionTest"));
        SetObject(
            serializedSelector,
            "monsterATemplate",
            GameObject.Find("MonsterA_ActionTest"));
        SetObject(
            serializedSelector,
            "monsterBTemplate",
            GameObject.Find("MonsterB_ActionTest"));
        SetObject(
            serializedSelector,
            "monsterCTemplate",
            GameObject.Find("MonsterC_ActionTest"));
        serializedSelector.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateInstructionCanvas()
    {
        // 이 메서드가 idempotent가 아니었던 시절(2026-08-11 이전) 여러 군데서 호출되면서(scene 최초
        // 생성 + MonsterB/C Action Test 갱신 시마다) "Instruction Canvas"/"Instruction Text"가
        // 계속 중복 생성됐었다 - 이제 이미 있으면 그냥 넘어간다.
        //
        // 2026-08-24: 만드는 이름을 "HUD Canvas"로 통일. 예전엔 이 씬만 "Instruction Canvas"라
        // 불렀는데, HUD 도구들이 캔버스를 새로 만드는 대신 이걸 재사용하다 보니 ActionTest만 이름이
        // 다른 채로 굳었다("조작 설명"이라는 원래 용도보다 HUD가 훨씬 많이 들어있는데도). 수업 문서가
        // "HUD는 ActionTest에서 고쳐라"로 바뀌면서 학생이 없는 이름을 찾게 될 자리라 정리.
        // 기존 씬(옛 이름)도 그대로 인식해야 하므로 존재 확인은 두 이름 다 본다.
        if (GameObject.Find("HUD Canvas") != null || GameObject.Find("Instruction Canvas") != null)
        {
            return;
        }

        var canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // 2026-08-17: 명시적으로 0.5(가로/세로 절반씩) 지정 - 이전엔 안 정해줘서 유니티 기본값(0,
        // 가로 기준)으로 남아있었는데, 다른 HUD 도구들(SpecialGauge/PauseMenu/DeathScreen 등)이
        // 만드는 "HUD Canvas"는 전부 0.5로 맞춰져 있어서 둘이 달랐다 - 이 캔버스를 나중에 HP표시/
        // 게이지 등이 공유해서 쓰기 때문에, ActionTest에서 맞춘 위치가 다른 씬(HUD Canvas 사용)과
        // 다르게 보이는 원인이었음(Sync 도구로 좌표를 그대로 복사해도 계산식 자체가 달라서 시각적
        // 결과가 어긋났음). 다른 도구들과 통일.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("Instruction Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -28f);
        rect.sizeDelta = new Vector2(900f, 260f);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.black;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.text =
            "PLAYER ACTION TEST\n" +
            "A / D: Move    Space: Double Jump    Shift: Dash\n" +
            "J / Left Click: Attack Combo    Air J / Left Click: Jump Attack\n" +
            "Hold K / Right Click: Charge Special, release when ready\n" +
            "H: Hit    N: Heavy Hit    Ctrl+M: Death";
    }

    private static Sprite[] LoadFrames(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var frames = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                frames.Add(sprite);
            }
        }

        frames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return frames.ToArray();
    }

    internal static Sprite[] LoadFramesFromFolder(string folder)
    {
        var frames = new List<Sprite>();
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'));
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }
        }

        frames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return frames.ToArray();
    }


    // 2026-08-18: 메뉴 이름을 MonsterA/C의 "...Prefab" 표기에 맞춤(동작은 그대로 - 프리팹 생성과
    // ActionTest 배치를 한 번에 함). 예전 이름("...Action Test")과 A/C 쪽 이름이 서로 달라서
    // 셋이 같은 역할을 하는 버튼이라는 게 한눈에 안 들어오는 문제가 있었음.
    [MenuItem("Tools/Class Template/Create Or Update MonsterB Prefab")]
    public static void CreateOrUpdateMonsterBActionTestInstance()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ImportMonsterBFrameFolders();
        Directory.CreateDirectory(PrefabRoot + "/Monsters");

        var existing = GameObject.Find("MonsterB_ActionTest");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        var monster = CreateMonsterBGameObject();
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            monster,
            MonsterBPrefabPath,
            InteractionMode.AutomatedAction);

        CreateInstructionCanvas();
        CreateMonsterSelector();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created or updated MonsterB action test instance, prefab, and selector.");
    }
    internal static void ImportFrameFolder(string folder, bool isPlayer, Vector2 pivot)
    {
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning($"Missing frame folder: {folder}");
            return;
        }

        foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
        {
            var assetPath = path.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = isPlayer ? PlayerPixelsPerUnit : EffectPixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static void SetSprites(SerializedObject so, string propertyName, string path)
    {
        var frames = LoadFrames(path);
        var property = so.FindProperty(propertyName);
        property.arraySize = frames.Length;
        for (var i = 0; i < frames.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
    }

    internal static void SetSpritesFromFolder(SerializedObject so, string propertyName, string folder)
    {
        var frames = new List<Sprite>();
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'));
                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }
        }

        frames.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        var property = so.FindProperty(propertyName);
        property.arraySize = frames.Count;
        for (var i = 0; i < frames.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
    }

    internal static void MatchFrameTimeCountToSpriteCount(SerializedObject so, string frameTimePropertyName, string spritePropertyName)
    {
        var spriteProperty = so.FindProperty(spritePropertyName);
        var frameTimeProperty = so.FindProperty(frameTimePropertyName);
        if (spriteProperty == null || frameTimeProperty == null)
        {
            return;
        }

        var previousSize = frameTimeProperty.arraySize;
        frameTimeProperty.arraySize = spriteProperty.arraySize;
        for (var i = previousSize; i < frameTimeProperty.arraySize; i++)
        {
            frameTimeProperty.GetArrayElementAtIndex(i).floatValue = 0f;
        }
    }

    private static void SetSpritesFromFiles(SerializedObject so, string propertyName, string folder, params string[] fileNames)
    {
        var property = so.FindProperty(propertyName);
        property.arraySize = fileNames.Length;

        for (var i = 0; i < fileNames.Length; i++)
        {
            var path = $"{folder}/{fileNames[i]}";
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }

    internal static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        so.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existing = EditorBuildSettings.scenes;
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        foreach (var scene in existing)
        {
            if (scene.path != scenePath)
            {
                scenes.Add(scene);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>
    /// Shows a "this will overwrite/destroy the existing scene" confirmation before a
    /// full scene rebuild. Only call when the target scene file already exists - a fresh
    /// first-time build has nothing to lose and shouldn't be interrupted. Returns true
    /// only if the user explicitly chose to proceed.
    /// </summary>
    /// <summary>
    /// 되돌릴 수 없는 작업의 확인창. 파괴적인 도구 네 곳이 전부 이 함수를 쓴다.
    ///
    /// <para>2026-08-29: <c>EditorUtility.DisplayDialog</c>는 첫 번째(ok) 버튼에 포커스를 준다.
    /// 원래는 파괴적인 쪽이 ok 자리에 있어서, 창이 뜬 상태에서 Enter나 Space를 치면 그대로
    /// 실행됐다. <b>확인창을 띄우는 이유가 한 박자 멈추게 하는 것인데 기본값이 위험한 쪽이면
    /// 멈추지 않는다.</b></para>
    ///
    /// <para>그래서 안전한 쪽("취소")을 ok 자리에 넣고 반환값을 뒤집는다. 버튼 순서도 같이 바뀌어
    /// 취소가 왼쪽에 오는데, 네 곳이 전부 이 함수를 쓰므로 순서는 서로 일관된다.</para>
    /// </summary>
    /// <returns>사용자가 파괴적인 쪽(<paramref name="proceedLabel"/>)을 눌렀으면 true.</returns>
    internal static bool ConfirmDestructive(string title, string message, string proceedLabel)
    {
        // 안전한 쪽이 ok(기본 포커스), 파괴적인 쪽이 cancel - 그래서 결과를 뒤집는다.
        return !EditorUtility.DisplayDialog(title, message, "취소", proceedLabel);
    }

    private static bool ConfirmSceneRebuild(string scenePath)
    {
        var confirmed = ConfirmDestructive(
            "씬 다시 만들기",
            $"{scenePath}\n\n이 파일이 이미 있습니다. 계속하면 지금 씬의 모든 내용(손으로 수정한 값 포함)이 사라지고 기본 상태로 새로 만들어집니다.\n\n되돌릴 수 없습니다. 정말 새로 만드시겠습니까?",
            "그래도 새로 만들기");

        if (!confirmed)
        {
            Debug.Log($"씬 재생성 취소됨 - 기존 씬을 그대로 유지합니다: {scenePath}");
        }

        return confirmed;
    }

    /// <summary>
    /// 2026-08-29: 이 도구만 확인창이 없었다. 형제인 Create Monster B/C Motion Test Scene은
    /// ConfirmSceneRebuild를 거치는데 여기만 바로 실행됐다.
    ///
    /// <para>이름 때문에 더 위험했다. 학생 문서(04_Unity_적용_가이드)가 가르치는 규칙은 "이름이
    /// Create ... Scene 인 도구는 실행하지 말 것"인데 이 항목은 Scene으로 끝나지 않는다. 게다가
    /// Create Or Update 는 문서가 실행하라고 가르치는 안전한 프리팹 도구(Create Or Update
    /// MonsterA Prefab)의 접두어다. 규칙을 지킨 학생일수록 안전하다고 판단하게 되어 있었다.</para>
    ///
    /// <para>ConfirmSceneRebuild와 따로 둔 것은 하는 일이 달라서다 - 씬을 통째로 새로 만들지는
    /// 않고 오브젝트 둘만 지우고 다시 만든다. 문구가 실제와 다르면 확인창이 있으나 마나다.</para>
    /// </summary>
    private static bool ConfirmMonsterCActionTestRebuild()
    {
        var confirmed = ConfirmDestructive(
            "MonsterC 액션 테스트 다시 만들기",
            $"{ScenePath}\n\n이 씬을 열어서 MonsterC_ActionTest 와 ActionTestMonsterSelector 를 지우고 기본 상태로 새로 만든 뒤 바로 저장합니다. 그 둘에 손으로 맞춰둔 위치나 값이 있으면 사라집니다.\n\n지금 열려 있는 씬도 닫힙니다 - 저장하지 않은 변경이 있으면 취소하고 먼저 저장하세요(Ctrl+S).\n\n되돌릴 수 없습니다. 계속하시겠습니까?",
            "그래도 다시 만들기");

        if (!confirmed)
        {
            Debug.Log($"MonsterC 액션 테스트 재생성 취소됨 - 씬을 그대로 유지합니다: {ScenePath}");
        }

        return confirmed;
    }
}







