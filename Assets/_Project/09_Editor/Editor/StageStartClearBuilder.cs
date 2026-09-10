using System.IO;
using GameProject.Environment;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Stage-number-parameterized twin of StageStartClearSceneTools, for Stage 2 and up (심화 -
/// 스테이지 확장, 2026-08-17). Adds the "GAME START" banner, the level-clear object (드래곤/꽃
/// 등 - 맞으면 클리어), and the clear screen to whatever Stage{N}_BackgroundTest scene is
/// currently open - infers which stage from the scene name (StageSceneBuilder.
/// InferStageFromOpenScene), same convention as the other Stage-N tools.
///
/// Deliberately a separate file from StageStartClearSceneTools.cs - Stage 1's tool/behavior is
/// untouched. Reuses only its pure scene-search/import utility helpers (bumped private->internal,
/// zero behavior change) instead of duplicating that plumbing again.
///
/// StageClearController.nextSceneName is left at its own script default ("EndingCutscene") when
/// first created here - per the 2026-08-17 design decision, stage-to-stage chaining is always a
/// manual, explicit choice the student makes in the Inspector (or via the not-yet-built dropdown
/// picker), never auto-computed. Leaving it at "EndingCutscene" means an unconfigured additional
/// stage safely defaults to ending the game, matching how Stage1 already behaves out of the box.
/// </summary>
public static class StageStartClearBuilder
{
    private const float GroundTopY = -3.4f; // same world-scale rule as StageSceneBuilder/BackgroundTestSceneBuilder
    private const float DefaultObjectInsetFromEdge = 3f;
    private const string LevelClearBgmClipBaseName = "level_clear_bgm";

    public static void AddStageStartAndClearToOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add Stage Start & Clear To Scene"))
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
            Debug.LogWarning("스테이지1은 이 도구 대상이 아닙니다 - 기존 'Add Stage Start & Clear To Background Test Scene' 메뉴를 쓰세요.");
            return;
        }

        StageStartClearSceneTools.ImportSprites(stage.StageStartArtFolder);
        StageStartClearSceneTools.ImportSprites(stage.StageClearArtFolder);
        StageStartClearSceneTools.ImportSprites(stage.LevelClearObjectArtFolder);
        EnsureLevelClearObjectPrefab(stage);

        var scene = SceneManager.GetActiveScene();

        EnsureGameStartBanner(scene, stage);
        var levelClearObject = EnsureLevelClearObjectInstance(scene, stage);
        EnsureStageClearScreen(scene, stage, levelClearObject);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"{stage.DisplayName}에 게임 시작 배너 / 레벨클리어 오브젝트 / 클리어 화면을 추가(또는 갱신)했습니다. 위치는 대략값이니 Scene 뷰에서 직접 맞추고, 클리어 후 다음 씬(nextSceneName)도 인스펙터에서 확인/설정하세요. 저장(Ctrl+S)하세요.");
    }

    // ---- 1. 게임 시작 배너 ------------------------------------------------------------------

    private static void EnsureGameStartBanner(Scene scene, StageIdentity stage)
    {
        if (StageStartClearSceneTools.FindByNameIncludingInactive(scene, "GameStartBanner") != null)
        {
            return;
        }

        var canvasTransform = StageStartClearSceneTools.EnsureHudCanvas(scene);
        var sprite = StageStartClearSceneTools.LoadSprite(stage.StageStartArtFolder, "game_start");
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(580f, 330f);

        var obj = new GameObject("GameStartBanner", typeof(RectTransform), typeof(Image), typeof(GameStartBannerController));
        obj.transform.SetParent(canvasTransform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -60f);
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    // ---- 2. 레벨클리어 오브젝트 --------------------------------------------------------------

    private static void EnsureLevelClearObjectPrefab(StageIdentity stage)
    {
        var directory = Path.GetDirectoryName(stage.LevelClearObjectPrefabPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(stage.LevelClearObjectPrefabPath) != null)
        {
            return;
        }

        var sprite = StageStartClearSceneTools.LoadSprite(stage.LevelClearObjectArtFolder, "level_clear_object");
        var root = new GameObject("LevelClearObject", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(LevelClearObjectController));

        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        var collider = root.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        if (sprite != null)
        {
            collider.size = sprite.bounds.size;
        }

        PrefabUtility.SaveAsPrefabAsset(root, stage.LevelClearObjectPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static LevelClearObjectController EnsureLevelClearObjectInstance(Scene scene, StageIdentity stage)
    {
        var existing = StageStartClearSceneTools.FindByNameIncludingInactive(scene, "LevelClearObject");
        if (existing != null)
        {
            return existing.GetComponent<LevelClearObjectController>();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.LevelClearObjectPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"{stage.LevelClearObjectPrefabPath}를 찾을 수 없습니다.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = ComputeDefaultLevelClearObjectPosition(stage);
        return instance.GetComponent<LevelClearObjectController>();
    }

    private static Vector3 ComputeDefaultLevelClearObjectPosition(StageIdentity stage)
    {
        var sprite = StageStartClearSceneTools.LoadSprite(stage.LevelClearObjectArtFolder, "level_clear_object");
        var halfHeight = sprite != null ? sprite.bounds.extents.y : 2.5f;

        var x = 60f;
        var groundRoot = GameObject.Find("Ground");
        if (groundRoot != null && GroundBoundsUtility.TryComputeBounds(groundRoot.transform, out _, out var maxX))
        {
            x = maxX - DefaultObjectInsetFromEdge;
        }

        return new Vector3(x, GroundTopY + halfHeight, 0f);
    }

    // ---- 3. 클리어 화면 ---------------------------------------------------------------------

    private static void EnsureStageClearScreen(Scene scene, StageIdentity stage, LevelClearObjectController levelClearObject)
    {
        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerActionTestController(주인공)를 찾을 수 없어 클리어 화면을 만들지 못했습니다.");
            return;
        }

        var screenObject = StageStartClearSceneTools.FindByNameIncludingInactive(scene, "StageClearController");
        if (screenObject == null)
        {
            var canvasTransform = StageStartClearSceneTools.EnsureHudCanvas(scene);
            screenObject = CreateStageClearHierarchy(canvasTransform, stage);
        }

        WireReferences(screenObject, stage, playerController, levelClearObject);
    }

    private static GameObject CreateStageClearHierarchy(Transform parent, StageIdentity stage)
    {
        var controllerObject = new GameObject("StageClearController", typeof(RectTransform), typeof(AudioSource), typeof(StageClearController));
        controllerObject.transform.SetParent(parent, false);
        StageStartClearSceneTools.SetupStingerSource(controllerObject);
        var controllerRect = controllerObject.GetComponent<RectTransform>();
        controllerRect.anchorMin = Vector2.zero;
        controllerRect.anchorMax = Vector2.one;
        controllerRect.offsetMin = Vector2.zero;
        controllerRect.offsetMax = Vector2.zero;

        var panel = new GameObject("StageClearPanel", typeof(RectTransform));
        panel.transform.SetParent(controllerObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 900f);

        var sprite = StageStartClearSceneTools.LoadSprite(stage.StageClearArtFolder, "level_clear");
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(580f, 330f);
        var imageObject = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(panel.transform, false);
        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = size;

        var image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        panel.SetActive(false);
        return controllerObject;
    }

    private static void WireReferences(GameObject controllerObject, StageIdentity stage, PlayerActionTestController playerController, LevelClearObjectController levelClearObject)
    {
        var controller = controllerObject.GetComponent<StageClearController>();
        if (controller == null)
        {
            return;
        }

        StageStartClearSceneTools.SetupStingerSource(controllerObject);

        var so = new SerializedObject(controller);
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.FindProperty("levelClearObject").objectReferenceValue = levelClearObject;

        var panelProperty = so.FindProperty("panel");
        if (panelProperty.objectReferenceValue == null)
        {
            var panelTransform = controllerObject.transform.Find("StageClearPanel");
            if (panelTransform != null)
            {
                panelProperty.objectReferenceValue = panelTransform.gameObject;
            }
        }

        var stingerSourceProperty = so.FindProperty("stingerSource");
        if (stingerSourceProperty.objectReferenceValue == null)
        {
            stingerSourceProperty.objectReferenceValue = controllerObject.GetComponent<AudioSource>();
        }

        var clearStingerProperty = so.FindProperty("clearStinger");
        if (clearStingerProperty.objectReferenceValue == null)
        {
            var clip = StageStartClearSceneTools.FindAudioClipByBaseName(stage.LevelClearBgmFolder, LevelClearBgmClipBaseName);
            if (clip == null)
            {
                Debug.LogWarning($"클리어 사운드 클립을 못 찾음: {stage.LevelClearBgmFolder}/{LevelClearBgmClipBaseName}.*");
            }

            clearStingerProperty.objectReferenceValue = clip;
        }

        var worldBgmProperty = so.FindProperty("worldBgmSource");
        if (worldBgmProperty.objectReferenceValue == null)
        {
            var bgmObject = GameObject.Find("BackgroundMusic");
            if (bgmObject != null)
            {
                worldBgmProperty.objectReferenceValue = bgmObject.GetComponent<AudioSource>();
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- Edit-mode preview toggle (same trick as StageStartClearSceneTools.ToggleStageClearPanelPreview) ----

    /// <summary>클리어 일러스트 위치를 Play 없이 Edit 모드에서 맞춰볼 수 있게 패널을 토글 - 반드시
    /// 끄고 나서 저장할 것(켠 채로 저장하면 Play 시작하자마자 클리어 화면이 떠 있게 됨).</summary>
    public static void ToggleStageClearPanelPreviewOnOpenScene()
    {
        var scene = SceneManager.GetActiveScene();
        var panel = StageStartClearSceneTools.FindByNameIncludingInactive(scene, "StageClearPanel");
        if (panel == null)
        {
            Debug.LogWarning("'StageClearPanel'을 찾을 수 없습니다. 이 씬에서 'Add Stage Start & Clear To Scene'을 먼저 실행하세요.");
            return;
        }

        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(panel.activeSelf
            ? "클리어 화면을 켰습니다 - 위치를 맞춘 뒤 이 버튼을 한 번 더 눌러 끄고 나서 저장하세요."
            : "클리어 화면을 껐습니다. 저장(Ctrl+S)하세요.");
    }
}
