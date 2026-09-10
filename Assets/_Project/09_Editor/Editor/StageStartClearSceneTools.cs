using System.IO;
using GameProject.Audio;
using GameProject.Environment;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BackgroundTest 씬에 (1) "GAME START" 배너, (2) 레벨클리어 오브젝트(스테이지에 배치되는 프리팹),
/// (3) 클리어 화면(StageClearController)을 추가/갱신한다. DeathScreenSceneTools와 같은
/// "이미 있으면 위치는 안 건드리고 레퍼런스만 다시 연결" 패턴 - 몇 번을 다시 실행해도 안전함.
/// </summary>
public static class StageStartClearSceneTools
{
    private const string StageStartArtPath = "Assets/_Project/04_Art/StudentReplace/UI/StageStart";
    private const string StageClearArtPath = "Assets/_Project/04_Art/StudentReplace/UI/StageClear";
    private const string LevelClearObjectArtPath = "Assets/_Project/04_Art/StudentReplace/Environment/LevelClearObject";
    private const string LevelClearBgmFolder = "Assets/_Project/06_Audio/BGM/LevelClear";
    private const string LevelClearBgmClipBaseName = "level_clear_bgm";
    private const string PrefabPath = "Assets/_Project/02_Prefabs/Environment/LevelClearObject.prefab";

    private const float GroundTopY = -3.4f; // BackgroundTestSceneBuilder와 동일한 world-scale 규칙(바닥 윗면 Y)
    private const float DefaultObjectInsetFromEdge = 3f; // 바닥 맨 끝에서 살짝 안쪽에 기본 배치(대략값)

    [MenuItem("Tools/Class Template/Add Stage Start & Clear To Background Test Scene")]
    public static void AddStageStartAndClearToScene()
    {
        ImportSprites(StageStartArtPath);
        ImportSprites(StageClearArtPath);
        ImportSprites(LevelClearObjectArtPath);
        EnsureLevelClearObjectPrefab();

        var scene = SceneManager.GetActiveScene();

        EnsureGameStartBanner(scene);
        var levelClearObject = EnsureLevelClearObjectInstance(scene);
        EnsureStageClearScreen(scene, levelClearObject);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("게임 시작 배너 / 레벨클리어 오브젝트 / 클리어 화면을 추가(또는 갱신)했습니다. 위치는 대략값이니 Scene 뷰에서 직접 맞추고 저장(Ctrl+S)하세요. 클리어는 L키로 즉시 테스트 가능(레벨클리어 오브젝트를 실제로 맞은 것과 동일하게 처리).");
    }

    [MenuItem("Tools/Class Template/Add Stage Start & Clear To Background Test Scene", true)]
    private static bool ValidateAddStageStartAndClearToScene()
    {
        return SceneManager.GetActiveScene().name == "BackgroundTest";
    }

    /// <summary>
    /// StageClearPanel은 평소 꺼져있는 게 정상(클리어됐을 때만 켜짐)이라, GameStartBanner와 달리
    /// Scene/Game 뷰 어디서도 안 보임 - Play 모드로 들어가서 실제로 클리어를 띄우기 전까지는
    /// 일러스트 위치를 맞출 방법이 없었다. 타이틀 팝업 미리보기 도구(TitleScenePreviewTools)와
    /// 같은 이유로 만든 Edit 모드 전용 토글 - Play 모드에서 옮긴 값은 정지하면 버려지지만, 이걸로
    /// 켜서 Edit 모드에서 옮기면 평범한 씬 변경이라 저장(Ctrl+S)하면 그대로 남는다.
    /// **끄는 걸 잊지 말 것**: 켜진 채로 저장하면 Play 진입 즉시 클리어 화면이 떠 있는 상태로
    /// 시작해버림(StageClearController가 패널의 활성 여부가 아니라 별도의 "이미 발동했는지" 플래그로만
    /// 동작해서, 패널이 켜져 있어도 재발동을 막아주지 않음).
    /// </summary>
    [MenuItem("Tools/Class Template/Toggle Stage Clear Panel Preview")]
    public static void ToggleStageClearPanelPreview()
    {
        var scene = SceneManager.GetActiveScene();
        var panel = FindByNameIncludingInactive(scene, "StageClearPanel");
        if (panel == null)
        {
            Debug.LogWarning("'StageClearPanel'을 찾을 수 없습니다. BackgroundTest.unity에서 'Add Stage Start & Clear To Background Test Scene'을 먼저 실행하세요.");
            return;
        }

        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(panel.activeSelf
            ? "클리어 화면을 켰습니다 - Scene/Game 뷰에서 위치를 맞춘 뒤, 이 메뉴를 한 번 더 눌러 끄고 나서 저장하세요(켠 채로 저장하면 Play 시작하자마자 클리어 화면이 떠 있게 됨)."
            : "클리어 화면을 껐습니다. 저장(Ctrl+S)하세요.");
    }

    [MenuItem("Tools/Class Template/Toggle Stage Clear Panel Preview", true)]
    private static bool ValidateToggleStageClearPanelPreview()
    {
        return SceneManager.GetActiveScene().name == "BackgroundTest";
    }

    // ---- 1. 게임 시작 배너 ------------------------------------------------------------------

    private static void EnsureGameStartBanner(Scene scene)
    {
        if (FindByNameIncludingInactive(scene, "GameStartBanner") != null)
        {
            return; // 이미 있으면 위치/크기는 안 건드림
        }

        var canvasTransform = EnsureHudCanvas(scene);
        var sprite = LoadSprite(StageStartArtPath, "game_start");
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(580f, 330f);

        var obj = new GameObject("GameStartBanner", typeof(RectTransform), typeof(Image), typeof(GameStartBannerController));
        obj.transform.SetParent(canvasTransform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -60f); // 화면 상단 중앙에서 살짝 아래 - 대략값
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    // ---- 2. 레벨클리어 오브젝트 --------------------------------------------------------------

    /// <summary>
    /// 학생이 나중에 프리팹 자체(이미지 교체)나 씬에 놓인 인스턴스(위치/개수)를 자유롭게 손댈 수
    /// 있도록 Ground/Background와 같은 프리팹 방식으로 만든다. 이미 프리팹이 있으면 덮어쓰지 않음.
    /// </summary>
    private static void EnsureLevelClearObjectPrefab()
    {
        var directory = Path.GetDirectoryName(PrefabPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        var sprite = LoadSprite(LevelClearObjectArtPath, "level_clear_object");
        var root = new GameObject("LevelClearObject", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(LevelClearObjectController));

        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10; // 플레이어와 같은 깊이 - 바닥(20)보다 뒤, 눈 이펙트(15)보다 뒤

        var collider = root.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        if (sprite != null)
        {
            collider.size = sprite.bounds.size;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    private static LevelClearObjectController EnsureLevelClearObjectInstance(Scene scene)
    {
        var existing = FindByNameIncludingInactive(scene, "LevelClearObject");
        if (existing != null)
        {
            return existing.GetComponent<LevelClearObjectController>();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"{PrefabPath}를 찾을 수 없습니다.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = ComputeDefaultLevelClearObjectPosition();
        return instance.GetComponent<LevelClearObjectController>();
    }

    /// <summary>스테이지 끝(드래곤 입구 자리) 근처에 자동 배치 - "Ground"의 실제 자식들을 스캔해서
    /// 오른쪽 끝 X를 구하고(GroundBoundsUtility, 몇 개가 있든 상관없이 항상 동작), 그보다 살짝
    /// 안쪽에 놓는다. Ground를 못 찾으면 대략적인 기본값으로 대체. 어느 쪽이든 대략값이라 Scene
    /// 뷰에서 직접 맞추면 됨.</summary>
    private static Vector3 ComputeDefaultLevelClearObjectPosition()
    {
        var sprite = LoadSprite(LevelClearObjectArtPath, "level_clear_object");
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

    private static void EnsureStageClearScreen(Scene scene, LevelClearObjectController levelClearObject)
    {
        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerActionTestController(주인공)를 찾을 수 없어 클리어 화면을 만들지 못했습니다.");
            return;
        }

        var screenObject = FindByNameIncludingInactive(scene, "StageClearController");
        if (screenObject == null)
        {
            var canvasTransform = EnsureHudCanvas(scene);
            screenObject = CreateStageClearHierarchy(canvasTransform);
        }

        WireReferences(screenObject, playerController, levelClearObject);
    }

    private static GameObject CreateStageClearHierarchy(Transform parent)
    {
        var controllerObject = new GameObject("StageClearController", typeof(RectTransform), typeof(AudioSource), typeof(StageClearController));
        controllerObject.transform.SetParent(parent, false);
        SetupStingerSource(controllerObject);
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
        panelRect.sizeDelta = new Vector2(900f, 900f); // 자식 배치용 논리적 크기 - 실제로는 자식(일러스트)만 보임

        var sprite = LoadSprite(StageClearArtPath, "level_clear");
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

        panel.SetActive(false); // 클리어 전까지는 숨김
        return controllerObject;
    }

    private static void WireReferences(GameObject controllerObject, PlayerActionTestController playerController, LevelClearObjectController levelClearObject)
    {
        var controller = controllerObject.GetComponent<StageClearController>();
        if (controller == null)
        {
            return;
        }

        SetupStingerSource(controllerObject);

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
            var clip = FindAudioClipByBaseName(LevelClearBgmFolder, LevelClearBgmClipBaseName);
            if (clip == null)
            {
                Debug.LogWarning($"클리어 사운드 클립을 못 찾음: {LevelClearBgmFolder}/{LevelClearBgmClipBaseName}.*");
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

    /// <summary>클리어 스팅어 전용 AudioSource - 1번만 재생되는 사운드라 playOnAwake/loop 둘 다 끄고
    /// 스크립트가 PlayOneShot으로 직접 재생한다. BGM 믹서 그룹 경유(사용자 확정) - 설정의 BGM 볼륨
    /// 슬라이더 영향을 받는다.</summary>
    internal static void SetupStingerSource(GameObject controllerObject)
    {
        var source = controllerObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = controllerObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = GameAudioSettings.BgmGroup; // GameAudioMixer가 아직 없으면 null - 문제없음
    }

    // ---- Sprite import ------------------------------------------------------------------------

    // internal (not private): reused by StageStartClearBuilder.cs for Stage 2+ (2026-08-17) - pure
    // utility, no Stage1-specific behavior.
    internal static void ImportSprites(string folder)
    {
        if (!Directory.Exists(folder))
        {
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
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    internal static Sprite LoadSprite(string folder, string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png");
    }

    internal static AudioClip FindAudioClipByBaseName(string folder, string baseName)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        foreach (var file in Directory.GetFiles(folder))
        {
            if (file.ToLowerInvariant().EndsWith(".meta"))
            {
                continue;
            }

            if (Path.GetFileNameWithoutExtension(file) == baseName)
            {
                return AssetDatabase.LoadAssetAtPath<AudioClip>(file.Replace('\\', '/'));
            }
        }

        return null;
    }

    // ---- Scene search helpers -----------------------------------------------------------------

    internal static Transform EnsureHudCanvas(Scene scene)
    {
        // 다른 HUD 도구들과 같은 규칙: ActionTest.unity의 "Instruction Canvas"를 재사용, 없으면
        // (또는 다른 HUD가 먼저 만들어둔) "HUD Canvas"를 재사용, 그것도 없으면 새로 만든다.
        var existing = FindByNameIncludingInactive(scene, "Instruction Canvas");
        if (existing != null)
        {
            return existing.transform;
        }

        existing = FindByNameIncludingInactive(scene, "HUD Canvas");
        if (existing != null)
        {
            return existing.transform;
        }

        var canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }

    internal static GameObject FindByNameIncludingInactive(Scene scene, string name)
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
