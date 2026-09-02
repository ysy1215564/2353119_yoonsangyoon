using System.Collections.Generic;
using System.IO;
using GameProject.Cameras;
using GameProject.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage-number-parameterized twin of BackgroundTestSceneBuilder, for Stage 2 and up (심화 -
/// 스테이지 확장, 2026-08-17). Builds a stage's ground/background/weather/camera/BGM exactly the
/// same way BackgroundTestSceneBuilder does for Stage 1 - same world-scale math, same
/// placeholder-tint-then-swap-real-art flow, same auto-bounds wiring - just reading every path
/// from a <see cref="StageIdentity"/> instead of a hardcoded constant, so it works for any stage
/// number without a new copy of this file.
///
/// Deliberately a SEPARATE file from BackgroundTestSceneBuilder.cs rather than a refactor of it -
/// Stage 1's builder is left completely untouched (原 파일 그대로) so there's zero risk of this
/// work changing Stage 1's behavior. Some duplication with BackgroundTestSceneBuilder is the
/// accepted cost of that safety (see chat 2026-08-17 - "스테이지1은 절대 안 건드린다").
///
/// Monster prefabs/placement and the start/clear banners are NOT built here - see
/// StageMonsterBuilder / StageStartClearBuilder (next pass) for those. This file only produces
/// an empty-but-walkable stage (ground, parallax background, weather, camera, BGM, player).
/// </summary>
public static class StageSceneBuilder
{
    // World-scale constants - identical across every stage, not stage-specific (same camera/PPU
    // convention the whole project uses). See BackgroundTestSceneBuilder's class doc for why.
    private const float TileWorldWidth = 19.2f;
    private const float LayerWorldHeight = 12f;
    private const float GroundTopY = -3.4f;
    private const float GroundThickness = 2f;
    private const int GroundCount = 4;
    private const int BackgroundLayerCount = 5;
    private const int BackgroundTileCount = 10;

    private const float BackgroundPixelsPerUnit = 100f;
    private const float WeatherPixelsPerUnit = 120f;
    private const int WeatherSortingOrder = 15;
    private const float WeatherShapeWidth = 22f;
    private const float WeatherShapeLocalY = 6.5f;
    private const int WeatherAnimationCycles = 6;

    private const float BgmDefaultVolume = 0.45f;

    private const string PlaceholderTexturePath = "Assets/_Project/04_Art/StudentReplace/_Placeholder/placeholdersquare.png";

    private static readonly Color[] GroundTints =
    {
        new Color(0.55f, 0.42f, 0.30f),
        new Color(0.50f, 0.38f, 0.27f),
        new Color(0.45f, 0.34f, 0.24f),
        new Color(0.40f, 0.30f, 0.21f),
    };

    private static readonly Color[] BackgroundTints =
    {
        new Color(0.55f, 0.55f, 0.55f),
        new Color(0.62f, 0.62f, 0.62f),
        new Color(0.70f, 0.70f, 0.70f),
        new Color(0.80f, 0.80f, 0.80f),
        new Color(0.88f, 0.88f, 0.90f),
    };

    private static readonly float[] BackgroundParallaxFactors = { 0.15f, 0.35f, 0.55f, 0.75f, 1f };

    // ---- Entry points (called from StageExpansionWindow with a chosen stage number) --------

    public static void CreateBackgroundTestScene(int stageNumber)
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Stage 씬 만들기"))
        {
            return;
        }

        if (stageNumber <= 1)
        {
            Debug.LogWarning("스테이지1은 이 도구 대상이 아닙니다 - 기존 'Create Background Test Scene'을 쓰세요.");
            return;
        }

        var stage = StageIdentity.For(stageNumber);

        if (File.Exists(stage.BackgroundTestScenePath))
        {
            var confirmed = ActionTestSceneBuilder.ConfirmDestructive(
                $"{stage.DisplayName} Background Test 씬 다시 만들기",
                $"{stage.BackgroundTestScenePath}\n\n이 파일이 이미 있습니다. 계속하면 지금 씬의 모든 내용이 사라지고 기본 상태(바닥 4개)로 새로 만들어집니다.\n\n되돌릴 수 없습니다. 정말 새로 만드시겠습니까?",
                "그래도 새로 만들기");

            if (!confirmed)
            {
                Debug.Log($"{stage.DisplayName} Create Background Test Scene: 취소됨 - 기존 씬을 그대로 유지합니다.");
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(stage.BackgroundTestScenePath) ?? string.Empty);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        EnsurePrefabs(stage);

        const float startX = 0f;

        var player = ActionTestSceneBuilder.CreatePlayer();
        player.name = $"Player_{stage.DisplayName}";
        player.transform.position = new Vector3(startX, GroundTopY, 0f);

        var groundRoot = CreateGroundSequence(stage, startX);
        CreateCamera(stage, player.transform, groundRoot.transform);
        CreateBackgroundLayers(stage, startX, groundRoot.transform);
        CreateFallDeathZone(groundRoot.transform);
        CreateBackgroundMusic(stage);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, stage.BackgroundTestScenePath);
        AddSceneToBuildSettings(stage.BackgroundTestScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {stage.DisplayName} background test scene: {stage.BackgroundTestScenePath}. 몬스터/스테이지 시작-클리어 배너는 별도 도구로 추가하세요.");
    }

    private static void CreateCamera(StageIdentity stage, Transform target, Transform groundParent)
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow2D));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(target.position.x, 0f, -10f);

        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.82f, 0.86f, 0.9f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        var follow = cameraObject.GetComponent<CameraFollow2D>();
        follow.SetTarget(target);
        follow.SetGroundParent(groundParent);

        CreateWeatherEffect(stage, cameraObject.transform);
    }

    private static void CreateFallDeathZone(Transform groundParent)
    {
        if (GameObject.Find("FallDeathZone") != null)
        {
            return;
        }

        var zoneObject = new GameObject("FallDeathZone", typeof(BoxCollider2D), typeof(FallDeathZone));
        zoneObject.GetComponent<FallDeathZone>().SetGroundParent(groundParent);
    }

    // ---- Background music -------------------------------------------------------------------

    /// <summary>Creates the "BackgroundMusic" object if missing, same as before. Also re-checks
    /// an already-existing one (2026-08-18 fix): only the clip is filled in when empty - loop/
    /// volume/etc on an existing object are left alone so a student's manual tuning survives a
    /// rerun. Covers the case where "Add Background Music To Scene" was first run before a BGM
    /// file existed in stage.BgmFolder (object got created with no clip) and the file was
    /// dropped in afterwards - previously the object-existence check alone made that permanent
    /// and re-running the tool did nothing, contradicting its own warning message below.</summary>
    private static void CreateBackgroundMusic(StageIdentity stage)
    {
        var musicObject = GameObject.Find("BackgroundMusic");
        AudioSource source;
        if (musicObject == null)
        {
            musicObject = new GameObject("BackgroundMusic", typeof(AudioSource));
            source = musicObject.GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = BgmDefaultVolume;
        }
        else
        {
            source = musicObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = musicObject.AddComponent<AudioSource>();
            }
        }

        if (source.clip == null)
        {
            source.clip = FindBgmClip(stage);
            if (source.clip == null)
            {
                Debug.LogWarning($"BackgroundMusic object created but no clip assigned - drop one audio file into {stage.BgmFolder} and run 'Add Background Music To Scene' again.");
            }
        }
    }

    private static AudioClip FindBgmClip(StageIdentity stage)
    {
        Directory.CreateDirectory(stage.BgmFolder);
        if (!Directory.Exists(stage.BgmFolder))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var file in Directory.GetFiles(stage.BgmFolder))
        {
            if (file.ToLowerInvariant().EndsWith(".meta"))
            {
                continue;
            }

            candidates.Add(file.Replace('\\', '/'));
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No BGM file found in {stage.BgmFolder}. Drop one audio file in there and rerun.");
            return null;
        }

        candidates.Sort();
        if (candidates.Count > 1)
        {
            Debug.LogWarning($"Multiple files found in {stage.BgmFolder}; using the first one alphabetically ({Path.GetFileName(candidates[0])}). Only one BGM track is supported - remove the others.");
        }

        var assetPath = candidates[0];
        ConfigureBgmImportSettings(assetPath);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    private static void ConfigureBgmImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
        {
            return;
        }

        var settings = importer.defaultSampleSettings;
        if (settings.loadType == AudioClipLoadType.Streaming)
        {
            return;
        }

        settings.loadType = AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    /// <summary>Rerun after dropping/replacing the BGM file in the currently open stage's BGM
    /// folder, without rebuilding the whole scene - infers which stage from the open scene's
    /// name, same trick as the other Refresh-style tools in this project.</summary>
    public static void AddBackgroundMusicToOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add Background Music To Scene"))
        {
            return;
        }

        var stage = InferStageFromOpenScene();
        if (stage == null)
        {
            return;
        }

        CreateBackgroundMusic(stage);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Background music added (or already present) from {stage.BgmFolder}. Save the scene (Ctrl+S).");
    }

    // ---- Ground -----------------------------------------------------------------------------

    private static GameObject CreateGroundSequence(StageIdentity stage, float startX)
    {
        var root = new GameObject("Ground");
        for (var i = 0; i < GroundCount; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{stage.GroundPrefabFolder}/Ground{i + 1}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"Missing ground prefab Ground{i + 1}; run this command again after EnsurePrefabs creates it.");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Ground{i + 1}_Instance";
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = new Vector3(startX + i * TileWorldWidth, 0f, 0f);
        }

        return root;
    }

    private static void EnsureGroundPrefabs(StageIdentity stage)
    {
        for (var i = 1; i <= GroundCount; i++)
        {
            EnsureGroundPrefab(stage, i);
        }
    }

    private static void EnsureGroundPrefab(StageIdentity stage, int index)
    {
        Directory.CreateDirectory(stage.GroundPrefabFolder);
        var path = $"{stage.GroundPrefabFolder}/Ground{index}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        var sprite = EnsurePlaceholderSprite();
        var root = new GameObject($"Ground{index}");

        var visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, GroundTopY - GroundThickness / 2f, 0f);
        visual.transform.localScale = new Vector3(TileWorldWidth, GroundThickness, 1f);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = GroundTints[(index - 1) % GroundTints.Length];
        renderer.sortingOrder = 20;

        var collision = new GameObject("Collision", typeof(BoxCollider2D));
        collision.transform.SetParent(root.transform, false);
        collision.transform.localPosition = new Vector3(0f, GroundTopY - GroundThickness / 2f, 0f);
        collision.layer = LayerMask.NameToLayer("Default");
        var box = collision.GetComponent<BoxCollider2D>();
        box.size = new Vector2(TileWorldWidth, GroundThickness);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ---- Background layers --------------------------------------------------------------------

    private static void CreateBackgroundLayers(StageIdentity stage, float startX, Transform groundParent)
    {
        var root = new GameObject("Background");
        for (var i = 0; i < BackgroundLayerCount; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{stage.BackgroundPrefabFolder}/BG{i + 1}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"Missing background prefab BG{i + 1}; run this command again after EnsurePrefabs creates it.");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"BG{i + 1}_Instance";
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = new Vector3(startX, 0f, 0f);
            instance.GetComponent<ParallaxLayer>().SetGroundParent(groundParent);
        }
    }

    private static void EnsureBackgroundPrefabs(StageIdentity stage)
    {
        for (var i = 1; i <= BackgroundLayerCount; i++)
        {
            EnsureBackgroundPrefab(stage, i);
        }
    }

    private static void EnsureBackgroundPrefab(StageIdentity stage, int index)
    {
        Directory.CreateDirectory(stage.BackgroundPrefabFolder);
        var path = $"{stage.BackgroundPrefabFolder}/BG{index}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        var sprite = EnsurePlaceholderSprite();
        var root = new GameObject($"BG{index}", typeof(ParallaxLayer));
        var sortingOrder = -10 * (index - 1);

        for (var t = 0; t < BackgroundTileCount; t++)
        {
            var tile = new GameObject($"Tile_{t}", typeof(SpriteRenderer));
            tile.transform.SetParent(root.transform, false);
            var offsetIndex = t - (BackgroundTileCount - 1) / 2f;
            tile.transform.localPosition = new Vector3(offsetIndex * TileWorldWidth, 0f, 0f);
            tile.transform.localScale = new Vector3(TileWorldWidth, LayerWorldHeight, 1f);

            var renderer = tile.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = BackgroundTints[(index - 1) % BackgroundTints.Length];
            renderer.sortingOrder = sortingOrder;
        }

        var parallax = root.GetComponent<ParallaxLayer>();
        parallax.ParallaxFactor = BackgroundParallaxFactors[Mathf.Min(index - 1, BackgroundParallaxFactors.Length - 1)];

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ---- Shared placeholder art (same physical file as Stage1's - genuinely shared, not
    // per-stage, since it's just a blank 4x4 tinting texture with no stage identity) ----------

    private static void EnsurePrefabs(StageIdentity stage)
    {
        EnsureGroundPrefabs(stage);
        EnsureBackgroundPrefabs(stage);
        EnsureWeatherPrefab(stage);
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderTexturePath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderTexturePath) ?? string.Empty);

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        File.WriteAllBytes(PlaceholderTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(PlaceholderTexturePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderTexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderTexturePath);
    }

    // ---- Weather effect -----------------------------------------------------------------------

    private static void EnsureWeatherPrefab(StageIdentity stage)
    {
        Directory.CreateDirectory(stage.WeatherPrefabFolder);
        var path = $"{stage.WeatherPrefabFolder}/WeatherFX.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        var material = EnsureWeatherMaterial(stage);
        var root = new GameObject("WeatherFX", typeof(ParticleSystem));
        var particleSystem = root.GetComponent<ParticleSystem>();
        ApplyWeatherDefaultTuning(particleSystem);

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = WeatherSortingOrder;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void ApplyWeatherDefaultTuning(ParticleSystem particleSystem)
    {
        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particleSystem.emission;
        emission.rateOverTime = 40f;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, WeatherShapeLocalY, 0f);
        shape.scale = new Vector3(WeatherShapeWidth, 0.5f, 1f);

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-3f, -2f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private static Material EnsureWeatherMaterial(StageIdentity stage)
    {
        var path = $"{stage.WeatherPrefabFolder}/WeatherFX_Mat.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(stage.WeatherPrefabFolder);
        var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        var material = new Material(shader) { mainTexture = EnsurePlaceholderSprite().texture };
        material.SetColor("_TintColor", Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateWeatherEffect(StageIdentity stage, Transform cameraTransform)
    {
        if (cameraTransform.Find("WeatherAnchor") != null)
        {
            return;
        }

        EnsureWeatherPrefab(stage);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{stage.WeatherPrefabFolder}/WeatherFX.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("WeatherFX prefab missing - run EnsurePrefabs first.");
            return;
        }

        var anchor = new GameObject("WeatherAnchor");
        anchor.transform.SetParent(cameraTransform, false);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "WeatherFX_Instance";
        instance.transform.SetParent(anchor.transform, false);
    }

    // ---- Real art ---------------------------------------------------------------------------

    /// <summary>Stage-N twin of BackgroundTestSceneBuilder.ApplyRealStageArt() - infers which
    /// stage from the currently open scene's name (same as the Refresh-style tools elsewhere in
    /// this project), so there's one menu entry regardless of stage number.</summary>
    public static void ApplyRealStageArtToOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Apply Real Stage Art"))
        {
            return;
        }

        var stage = InferStageFromOpenScene();
        if (stage == null)
        {
            return;
        }

        ApplyRealStageArt(stage);
    }

    private static void ApplyRealStageArt(StageIdentity stage)
    {
        foreach (var index in ScanNumberedFiles(stage.GroundArtFolder, "ground"))
        {
            var sprite = ImportRealArtSprite($"{stage.GroundArtFolder}/ground{index}.png");
            if (sprite == null)
            {
                continue;
            }

            EnsureGroundPrefab(stage, index);
            ApplyGroundSprite(stage, index, sprite);
        }

        foreach (var index in ScanNumberedFiles(stage.BackgroundArtFolder, "bg"))
        {
            var sprite = ImportRealArtSprite($"{stage.BackgroundArtFolder}/bg{index}.png");
            if (sprite == null)
            {
                continue;
            }

            EnsureBackgroundPrefab(stage, index);
            ApplyBackgroundSprite(stage, index, sprite);
        }

        ApplyWeatherArt(stage);

        AssetDatabase.SaveAssets();
        Debug.Log($"Applied real stage art to {stage.DisplayName}'s Ground/Background/Weather prefabs. Drag any newly-created prefab from the Project window into {Path.GetFileName(stage.BackgroundTestScenePath)} to place it (Weather: run 'Add Weather Effect To Scene' instead) - this command does not touch the scene itself.");
    }

    private static List<int> ScanNumberedFiles(string folder, string prefix)
    {
        var indices = new List<int>();
        if (!Directory.Exists(folder))
        {
            return indices;
        }

        foreach (var file in Directory.GetFiles(folder, $"{prefix}*.png"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.Length <= prefix.Length || !name.StartsWith(prefix))
            {
                continue;
            }

            if (int.TryParse(name.Substring(prefix.Length), out var index))
            {
                indices.Add(index);
            }
        }

        indices.Sort();
        return indices;
    }

    private static Sprite ImportRealArtSprite(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = BackgroundPixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ApplyGroundSprite(StageIdentity stage, int index, Sprite sprite)
    {
        var path = $"{stage.GroundPrefabFolder}/Ground{index}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"Ground{index} prefab not found at {path}.");
            return;
        }

        var visual = root.transform.Find("Visual");
        if (visual != null)
        {
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ApplyBackgroundSprite(StageIdentity stage, int index, Sprite sprite)
    {
        var path = $"{stage.BackgroundPrefabFolder}/BG{index}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"BG{index} prefab not found at {path}.");
            return;
        }

        for (var t = 0; t < root.transform.childCount; t++)
        {
            var tile = root.transform.GetChild(t);
            var renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                continue;
            }

            renderer.sprite = sprite;
            renderer.color = Color.white;
            var localPos = tile.localPosition;
            localPos.y = 0f;
            tile.localPosition = localPos;
            tile.localScale = Vector3.one;
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ApplyWeatherArt(StageIdentity stage)
    {
        var indices = ScanNumberedFiles(stage.WeatherArtFolder, "weather");
        if (indices.Count == 0)
        {
            return;
        }

        EnsureWeatherPrefab(stage);

        var framePaths = new List<string>();
        foreach (var index in indices)
        {
            framePaths.Add($"{stage.WeatherArtFolder}/weather{index}.png");
        }

        var atlas = BuildWeatherAtlas(stage, framePaths, out var frameCount);
        if (atlas == null)
        {
            Debug.LogWarning("Weather art found but the atlas build failed - check the Console for texture read errors.");
            return;
        }

        ApplyWeatherAtlasToPrefab(stage, atlas, frameCount);
        Debug.Log($"Applied {frameCount} weather frame(s) to {stage.DisplayName}'s WeatherFX.prefab (Texture Sheet Animation, {frameCount}x1 grid).");
    }

    private static Texture2D BuildWeatherAtlas(StageIdentity stage, List<string> framePaths, out int frameCount)
    {
        frameCount = 0;
        var sourceTextures = new List<Texture2D>();
        foreach (var path in framePaths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                continue;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                sourceTextures.Add(texture);
            }
        }

        if (sourceTextures.Count == 0)
        {
            return null;
        }

        var cellSize = 0;
        foreach (var texture in sourceTextures)
        {
            cellSize = Mathf.Max(cellSize, Mathf.Max(texture.width, texture.height));
        }

        frameCount = sourceTextures.Count;
        var atlas = new Texture2D(cellSize * frameCount, cellSize, TextureFormat.RGBA32, false);
        atlas.SetPixels32(new Color32[atlas.width * atlas.height]);

        for (var i = 0; i < sourceTextures.Count; i++)
        {
            var texture = sourceTextures[i];
            atlas.SetPixels(i * cellSize, 0, texture.width, texture.height, texture.GetPixels());
        }

        atlas.Apply();

        var atlasPath = $"{stage.WeatherArtFolder}/weatheratlas_generated.png";
        File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
        Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceUpdate);

        var atlasImporter = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
        atlasImporter.textureType = TextureImporterType.Default;
        atlasImporter.alphaIsTransparency = true;
        atlasImporter.mipmapEnabled = false;
        atlasImporter.filterMode = FilterMode.Bilinear;
        atlasImporter.wrapMode = TextureWrapMode.Clamp;
        atlasImporter.textureCompression = TextureImporterCompression.Uncompressed;
        atlasImporter.isReadable = false;
        atlasImporter.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
    }

    private static void ApplyWeatherAtlasToPrefab(StageIdentity stage, Texture2D atlas, int frameCount)
    {
        var path = $"{stage.WeatherPrefabFolder}/WeatherFX.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"WeatherFX prefab not found at {path}.");
            return;
        }

        var particleSystem = root.GetComponent<ParticleSystem>();
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        if (particleSystem == null || renderer == null)
        {
            Debug.LogWarning("WeatherFX prefab is missing ParticleSystem/ParticleSystemRenderer.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        if (renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.mainTexture = atlas;
            renderer.sharedMaterial.SetColor("_TintColor", Color.white);
        }

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
        textureSheetAnimation.numTilesX = frameCount;
        textureSheetAnimation.numTilesY = 1;
        textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        textureSheetAnimation.cycleCount = WeatherAnimationCycles;
        textureSheetAnimation.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---- Add-to-open-scene tools (weather / auto bounds) -------------------------------------

    public static void AddWeatherEffectToOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Add Weather Effect To Scene"))
        {
            return;
        }

        var stage = InferStageFromOpenScene();
        if (stage == null)
        {
            return;
        }

        var cameraObject = GameObject.FindWithTag("MainCamera");
        if (cameraObject == null)
        {
            Debug.LogWarning("No Main Camera found in the open scene.");
            return;
        }

        CreateWeatherEffect(stage, cameraObject.transform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Weather effect added under Main Camera (WeatherAnchor/WeatherFX_Instance). Save the scene (Ctrl+S).");
    }

    public static void WireAutoLevelBoundsOnOpenScene()
    {
        if (EditorPlayModeGuard.BlockIfPlaying("Wire Auto Level Bounds"))
        {
            return;
        }

        var groundRoot = GameObject.Find("Ground");
        if (groundRoot == null)
        {
            Debug.LogWarning("No 'Ground' object found in the open scene.");
            return;
        }

        var cameraObject = GameObject.FindWithTag("MainCamera");
        var follow = cameraObject != null ? cameraObject.GetComponent<CameraFollow2D>() : null;
        if (follow != null)
        {
            follow.SetGroundParent(groundRoot.transform);
            EditorUtility.SetDirty(follow);
        }

        var backgroundRoot = GameObject.Find("Background");
        var wiredLayers = 0;
        if (backgroundRoot != null)
        {
            foreach (Transform child in backgroundRoot.transform)
            {
                var layer = child.GetComponent<ParallaxLayer>();
                if (layer == null)
                {
                    continue;
                }

                layer.SetGroundParent(groundRoot.transform);
                EditorUtility.SetDirty(layer);
                wiredLayers++;
            }
        }

        var deathZone = GameObject.Find("FallDeathZone");
        var fallDeathZone = deathZone != null ? deathZone.GetComponent<FallDeathZone>() : null;
        if (fallDeathZone != null)
        {
            fallDeathZone.SetGroundParent(groundRoot.transform);
            EditorUtility.SetDirty(fallDeathZone);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Wired auto level bounds: camera={(follow != null)}, background layers wired={wiredLayers}, fall death zone={(fallDeathZone != null)}. Save the scene (Ctrl+S).");
    }

    // ---- Stage inference from the open scene --------------------------------------------------

    /// <summary>
    /// "Refresh"/"Add"-style tools (unlike "Create") run against whatever scene is already
    /// open, so they figure out which stage they're operating on from that scene's file name
    /// instead of asking again - same UX as every other Refresh tool in this project. Scene
    /// names follow "Stage{N}_BackgroundTest"; a bare "BackgroundTest" is Stage 1 (handled by
    /// the original BackgroundTestSceneBuilder, not this class, but recognized here too in case
    /// a stage-agnostic tool call ever routes through here).
    /// </summary>
    // internal (not private): reused by StageStartClearBuilder.cs (2026-08-17) - pure lookup, no
    // behavior change.
    internal static StageIdentity InferStageFromOpenScene()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "BackgroundTest")
        {
            return StageIdentity.For(1);
        }

        if (sceneName.StartsWith("Stage") && sceneName.Contains("_"))
        {
            var numberPart = sceneName.Substring(5, sceneName.IndexOf('_') - 5);
            if (int.TryParse(numberPart, out var stageNumber) && stageNumber >= 2)
            {
                return StageIdentity.For(stageNumber);
            }
        }

        Debug.LogWarning($"현재 열린 씬('{sceneName}')이 어느 스테이지인지 알 수 없습니다 - Stage{{N}}_BackgroundTest.unity를 열고 실행하세요.");
        return null;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in existing)
        {
            if (s.path == scenePath)
            {
                return;
            }
        }

        existing.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = existing.ToArray();
    }
}
