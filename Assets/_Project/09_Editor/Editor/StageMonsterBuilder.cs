using System.IO;
using GameProject.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds Stage 2+'s 3 monster prefabs by CLONING Stage 1's own live MonsterA/B/C.prefab and
/// swapping only the art/SFX (2026-08-17 확정: "몬스터 행동 패턴은 유지, 외형 이미지만 교체") -
/// slot 1 clones MonsterA.prefab, slot 2 clones MonsterB.prefab, slot 3 clones MonsterC.prefab,
/// per StageIdentity's permanent slot->mold mapping.
///
/// 2026-08-17 rewrite: the first version built each monster from scratch, re-declaring every
/// balance field (attack box size/offset, projectile speed, collider size, controlMode, ...) with
/// the values from ActionTestSceneBuilder's ORIGINAL builder code. That broke real combat in
/// Stage2 - Stage1's actual MonsterA/B/C.prefab assets have accumulated real hand-tuning since
/// they were first created (e.g. MonsterA.prefab's attackBoxOffset is {-0.62, 0.63}, nothing like
/// the original code's {1.0, 0.65}) that the from-scratch approach had no way to know about, so
/// several monster attacks silently stopped landing on the player. Cloning the live prefab
/// instead means every balance value (HP, ranges, attack boxes, projectile speed, controlMode,
/// effect offsets, ...) is inherited byte-for-byte and automatically stays correct even as
/// Stage1 gets tuned further later - only sprites/SFX (the actual "외형") get overridden here.
///
/// Deliberately does NOT touch ActionTestSceneBuilder.cs's own monster-building menu items -
/// Stage 1's MonsterA/B/C prefabs are built/updated exactly as before, and this only READS them.
///
/// SFX filenames use plain role names (attack.wav / hit.wav / death.wav, or attack1/attack2 for
/// the 2-attack molds) rather than Stage1's "monstera_"-style prefix - the folder itself
/// (SFX/Stage{N}Monster{slot}/) is already what namespaces it.
/// </summary>
public static class StageMonsterBuilder
{
    private const string MonsterPrefabRoot = "Assets/_Project/02_Prefabs/Monsters";

    /// <summary>Builds (or rebuilds) all 3 of a stage's monster prefabs from their current art/SFX
    /// folders. Safe to rerun any time art changes - always re-imports and re-clones from Stage1's
    /// current prefab, so re-running after Stage1 gets balance-tuned further also picks that up.</summary>
    public static void BuildAllMonsterPrefabs(int stageNumber)
    {
        if (EditorPlayModeGuard.BlockIfPlaying("몬스터 프리팹 만들기"))
        {
            return;
        }

        if (stageNumber <= 1)
        {
            Debug.LogWarning("스테이지1은 이 도구 대상이 아닙니다 - 기존 'Create Or Update MonsterX Prefab' 메뉴를 쓰세요.");
            return;
        }

        var stage = StageIdentity.For(stageNumber);
        BuildMonsterPrefab(stage, 1);
        BuildMonsterPrefab(stage, 2);
        BuildMonsterPrefab(stage, 3);

        AssetDatabase.SaveAssets();
        Debug.Log($"{stage.DisplayName}의 몬스터 프리팹 3개(Monster1/2/3)를 만들었습니다/새로 반영했습니다 (스테이지1 밸런스 값 그대로 상속, 그림/사운드만 교체). 'Add Monster To Open Scene'으로 Stage{stageNumber}_BackgroundTest 씬에 배치하세요.");
    }

    private static void BuildMonsterPrefab(StageIdentity stage, int slot)
    {
        var identity = stage.Monster(slot);
        var templatePath = $"{MonsterPrefabRoot}/Monster{identity.Mold}.prefab";
        var template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
        if (template == null)
        {
            Debug.LogWarning($"{templatePath}가 없습니다 - 스테이지1의 'Create Or Update Monster{identity.Mold} Prefab'을 먼저 실행해서 그 프리팹부터 만들어야 합니다.");
            return;
        }

        ImportSlotFrameFolders(identity);

        // 스테이지1의 실제(튜닝된) 프리팹을 그대로 복제 - HP/공격판정/투사체 속도 등 모든 밸런스 값이
        // 이 한 줄로 자동 상속됨. 이어서 그림/사운드만 덮어쓴다.
        // Object.Instantiate (PrefabUtility.InstantiatePrefab이 아님)로 만든 복제본은 애초에
        // 프리팹과 "연결된 인스턴스"가 아니라 완전히 독립된 복사본이라, 별도로 연결을 끊을
        // 필요가 없다 - 바로 SaveAsPrefabAsset해도 Variant가 아닌 독립된 새 프리팹으로 저장된다.
        // (UnpackPrefabInstance를 여기 넣었다가 "must be called with a Prefab instance"
        // ArgumentException으로 터진 걸 2026-08-17 실기기 테스트에서 확인 - 불필요+ 잘못된 호출이었음.)
        var monster = (GameObject)Object.Instantiate(template);
        monster.name = "Monster";

        switch (slot)
        {
            case 1:
                OverrideMold1ArtAndAudio(monster, identity);
                break;
            case 2:
                OverrideMold2ArtAndAudio(monster, identity);
                break;
            case 3:
                OverrideMold3ArtAndAudio(monster, identity);
                break;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(slot), slot, "slot must be 1, 2, or 3.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(identity.PrefabPath) ?? string.Empty);
        PrefabUtility.SaveAsPrefabAsset(monster, identity.PrefabPath);
        Object.DestroyImmediate(monster);
    }

    private static void ImportSlotFrameFolders(StageIdentity.MonsterSlotIdentity identity)
    {
        var pivot = new Vector2(0.5f, 0f); // bottom-center, same convention Stage1's monsters/player use
        foreach (var folder in FolderNamesForMold(identity.Mold))
        {
            ActionTestSceneBuilder.ImportFrameFolder($"{identity.ArtFolder}/{folder}", true, pivot);
        }
    }

    private static string[] FolderNamesForMold(string mold)
    {
        switch (mold)
        {
            case "A":
                return new[] { "idle_frames", "walk_frames", "attack1_frames", "hit_frames", "death_frames" };
            case "B":
                return new[]
                {
                    "idle_frames", "walk_frames", "attack1_frames", "attack2_frames",
                    "attack1_effect_frames", "attack2_effect_frames", "hit_frames", "death_frames"
                };
            case "C":
                return new[]
                {
                    "idle_frames", "fly_frames", "attack1_frames", "attack2_charge_frames",
                    "attack2_dash_frames", "hit_frames", "death_frames", "Effects/Projectile"
                };
            default:
                return new string[0];
        }
    }

    // ---- Mold 1 (MonsterA's script) - only sprites/SFX are overridden, everything else (collider,
    // scale, layer, attackBox, groundMask, HP, ...) comes from the cloned MonsterA.prefab as-is ----

    private static void OverrideMold1ArtAndAudio(GameObject monster, StageIdentity.MonsterSlotIdentity identity)
    {
        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = ActionTestSceneBuilder.LoadFramesFromFolder($"{identity.ArtFolder}/idle_frames");
        var walkFrames = ActionTestSceneBuilder.LoadFramesFromFolder($"{identity.ArtFolder}/walk_frames");
        var attackFrames = ActionTestSceneBuilder.LoadFramesFromFolder($"{identity.ArtFolder}/attack1_frames");
        var previewFrames = idleFrames.Length > 0 ? idleFrames : walkFrames.Length > 0 ? walkFrames : attackFrames;
        if (previewFrames.Length > 0)
        {
            renderer.sprite = previewFrames[0];
        }

        var controller = monster.GetComponent<MonsterAActionTestController>();
        var so = new SerializedObject(controller);
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "idleFrames", $"{identity.ArtFolder}/idle_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "walkFrames", $"{identity.ArtFolder}/walk_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attackFrames", $"{identity.ArtFolder}/attack1_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "hitFrames", $"{identity.ArtFolder}/hit_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "deathFrames", $"{identity.ArtFolder}/death_frames");
        ActionTestSceneBuilder.SetAudioClip(so, "attackSfx.clip", $"{identity.SfxFolder}/attack");
        ActionTestSceneBuilder.SetAudioClip(so, "hitSfx.clip", $"{identity.SfxFolder}/hit");
        ActionTestSceneBuilder.SetAudioClip(so, "deathSfx.clip", $"{identity.SfxFolder}/death");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- Mold 2 (MonsterB's script) - the effect child objects (attack1/2 effect renderers) are
    // already present on the cloned template with correct internal wiring, so only their FRAME
    // arrays and the main animation frames/SFX need overriding here ------------------------------

    private static void OverrideMold2ArtAndAudio(GameObject monster, StageIdentity.MonsterSlotIdentity identity)
    {
        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = ActionTestSceneBuilder.LoadFramesFromFolder($"{identity.ArtFolder}/idle_frames");
        if (idleFrames.Length > 0)
        {
            renderer.sprite = idleFrames[0];
        }

        var controller = monster.GetComponent<MonsterBActionTestController>();
        var so = new SerializedObject(controller);
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "idleFrames", $"{identity.ArtFolder}/idle_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "walkFrames", $"{identity.ArtFolder}/walk_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack1Frames", $"{identity.ArtFolder}/attack1_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack2Frames", $"{identity.ArtFolder}/attack2_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "hitFrames", $"{identity.ArtFolder}/hit_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "deathFrames", $"{identity.ArtFolder}/death_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack1EffectFrames", $"{identity.ArtFolder}/attack1_effect_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack2EffectFrames", $"{identity.ArtFolder}/attack2_effect_frames");
        ActionTestSceneBuilder.SetAudioClip(so, "attack1Sfx.clip", $"{identity.SfxFolder}/attack1");
        ActionTestSceneBuilder.SetAudioClip(so, "attack2Sfx.clip", $"{identity.SfxFolder}/attack2");
        ActionTestSceneBuilder.SetAudioClip(so, "hitSfx.clip", $"{identity.SfxFolder}/hit");
        ActionTestSceneBuilder.SetAudioClip(so, "deathSfx.clip", $"{identity.SfxFolder}/death");
        so.ApplyModifiedPropertiesWithoutUndo();

        // 2026-08-17 안전장치(task #3): "몇 번째 공격 프레임부터 이펙트 시작"(attack1EffectStartFrame,
        // 스테이지1 값 그대로 상속됨)이 새로 넣은 attack1Frames/attack2Frames 길이를 벗어나면 경고 -
        // 프레임 수가 원본과 달라질 수 있는 시나리오(다른 그림으로 교체)에서만 실제로 걸림.
        MonsterFrameIndexValidator.WarnIfOutOfRange(so, "attack1Frames", "attack1EffectStartFrame", "Mold2/attack1EffectStartFrame");
        MonsterFrameIndexValidator.WarnIfOutOfRange(so, "attack2Frames", "attack2EffectStartFrame", "Mold2/attack2EffectStartFrame");
    }

    // ---- Mold 3 (MonsterC's script) - controlMode/projectileSpeed/attack ranges/etc. all come
    // from the cloned template; only sprites/SFX are overridden here --------------------------

    private static void OverrideMold3ArtAndAudio(GameObject monster, StageIdentity.MonsterSlotIdentity identity)
    {
        var renderer = monster.GetComponent<SpriteRenderer>();
        var idleFrames = ActionTestSceneBuilder.LoadFramesFromFolder($"{identity.ArtFolder}/idle_frames");
        if (idleFrames.Length > 0)
        {
            renderer.sprite = idleFrames[0];
        }

        var controller = monster.GetComponent<MonsterCActionTestController>();
        var so = new SerializedObject(controller);
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "idleFrames", $"{identity.ArtFolder}/idle_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "flyFrames", $"{identity.ArtFolder}/fly_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack1Frames", $"{identity.ArtFolder}/attack1_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack2ChargeFrames", $"{identity.ArtFolder}/attack2_charge_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "attack2DashFrames", $"{identity.ArtFolder}/attack2_dash_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "hitFrames", $"{identity.ArtFolder}/hit_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "deathFrames", $"{identity.ArtFolder}/death_frames");
        ActionTestSceneBuilder.SetSpritesFromFolder(so, "projectileFrames", $"{identity.ArtFolder}/Effects/Projectile");
        // 프레임 수가 원본과 달라질 수 있으니 판정 타이밍 배열 길이를 새 스프라이트 배열 길이에 맞춤
        // (스테이지1 값 자체는 유지, 배열 길이만 동기화 - 늘어난 칸은 0으로 채워지고 사용자가 채우면 됨).
        ActionTestSceneBuilder.MatchFrameTimeCountToSpriteCount(so, "attack1FrameTimes", "attack1Frames");
        ActionTestSceneBuilder.MatchFrameTimeCountToSpriteCount(so, "attack2ChargeFrameTimes", "attack2ChargeFrames");
        ActionTestSceneBuilder.MatchFrameTimeCountToSpriteCount(so, "attack2DashFrameTimes", "attack2DashFrames");
        ActionTestSceneBuilder.SetAudioClip(so, "attack1Sfx.clip", $"{identity.SfxFolder}/attack1");
        ActionTestSceneBuilder.SetAudioClip(so, "attack2Sfx.clip", $"{identity.SfxFolder}/attack2");
        ActionTestSceneBuilder.SetAudioClip(so, "hitSfx.clip", $"{identity.SfxFolder}/hit");
        ActionTestSceneBuilder.SetAudioClip(so, "deathSfx.clip", $"{identity.SfxFolder}/death");
        so.ApplyModifiedPropertiesWithoutUndo();

        // 2026-08-17 안전장치(task #3): attack1ProjectileFrame(스테이지1 값 그대로 상속됨)이 새
        // attack1Frames 범위를 벗어나면 경고.
        MonsterFrameIndexValidator.WarnIfOutOfRange(so, "attack1Frames", "attack1ProjectileFrame", "Mold3/attack1ProjectileFrame");
    }

    // ---- Placing a built prefab into whatever stage scene is currently open ------------------

    /// <summary>Drops one instance of a stage's slot-N monster prefab into whatever scene is
    /// currently open, at the camera's current X - same "just a starting point, drag it in the
    /// Scene view to actually place it" convention as Stage1's Add MonsterX To Scene.</summary>
    public static void AddMonsterToOpenScene(int stageNumber, int slot)
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add Monster To Scene"))
        {
            return;
        }

        var stage = StageIdentity.For(stageNumber);
        var identity = stage.Monster(slot);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(identity.PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"{identity.PrefabPath}에 프리팹이 없습니다 - 먼저 'Build All Monster Prefabs'를 실행하세요.");
            return;
        }

        var cameraObject = GameObject.FindWithTag("MainCamera");
        var spawnX = cameraObject != null ? cameraObject.transform.position.x : 0f;
        var spawnY = identity.Mold == "C" ? -0.8f : -3.4f; // Mold3(비행형)는 공중 스폰, 나머지는 바닥(GroundTopY)

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = $"Stage{stageNumber}Monster{slot}_Instance";
        instance.transform.position = new Vector3(spawnX, spawnY, 0f);

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"{identity.PrefabPath}를 씬에 추가했습니다. Scene 뷰에서 드래그해서 배치하고 저장하세요(Ctrl+S).");
    }
}
