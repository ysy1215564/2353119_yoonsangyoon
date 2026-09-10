using System.Collections.Generic;
using System.IO;
using GameProject.Cameras;
using GameProject.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the "Background Test" scene: ground (바닥1~4) connected end to end, five
/// parallax background layers (배경1~5) behind them, a player that can walk/jump across
/// them, and a camera that follows the player. Ground/background art is placeholder
/// (flat tinted color) until the real 1920x1080 PNGs are dropped into the prefabs -
/// see EnsurePrefabs() for exactly where to swap sprites later.
///
/// World-scale assumptions (see WorkLog / chat discussion):
///  - Ground and background art is authored at 1920x1080 and imported at 100 px-per-unit,
///    so one full-width tile = 19.2 world units - which also equals the camera's full
///    visible width at orthographicSize 5.4, i.e. exactly one screen.
///  - Ground top surface sits at Y = -3.4, matching ActionTest's "Flat Test Floor".
/// </summary>
public static class BackgroundTestSceneBuilder
{
    private const string ScenePath = "Assets/_Project/00_Scenes/Stages/BackgroundTest.unity";
    private const string PrefabRoot = "Assets/_Project/02_Prefabs/Environment";
    private const string GroundPrefabFolder = PrefabRoot + "/Ground";
    private const string BackgroundPrefabFolder = PrefabRoot + "/Background";
    private const string PlaceholderTexturePath = "Assets/_Project/04_Art/StudentReplace/_Placeholder/placeholdersquare.png";
    private const string RealGroundArtFolder = "Assets/_Project/04_Art/StudentReplace/StageGround";
    private const string RealBackgroundArtFolder = "Assets/_Project/04_Art/StudentReplace/StageBackground";
    private const string WeatherPrefabFolder = PrefabRoot + "/Weather";
    private const string RealWeatherArtFolder = "Assets/_Project/04_Art/StudentReplace/StageWeather";
    private const string BgmFolder = "Assets/_Project/06_Audio/BGM";

    private const float BackgroundPixelsPerUnit = 100f; // 1920x1080 art -> 19.2 x 10.8 world units, fills the camera exactly
    private const float TileWorldWidth = 19.2f;
    private const float LayerWorldHeight = 12f; // a bit taller than the 10.8-unit camera view so nothing peeks above/below it
    private const float GroundTopY = -3.4f;
    private const float GroundThickness = 2f;
    private const int GroundCount = 4;
    private const int BackgroundLayerCount = 5;
    private const int BackgroundTileCount = 10; // fixed, generous coverage - this is one short fixed-length stage, not infinite scroll

    private const float WeatherPixelsPerUnit = 120f; // "effect" category art, same convention as player/effect sprites (not the 100 PPU ground/background use)
    private const int WeatherSortingOrder = 15; // between ground (20) and the player (10), per 바닥 -> 눈 -> 캐릭터 -> 배경1~5
    private const float WeatherShapeWidth = 22f; // a bit wider than one screen (19.2) so nothing pops in at the edges while the camera pans
    private const float WeatherShapeLocalY = 6.5f; // spawn box sits above the camera's top edge (orthographicSize 5.4 -> half-height 5.4)
    private const int WeatherAnimationCycles = 6; // how many full flip-book loops play over one particle's ~5s lifetime - raise/lower to speed up/slow down the flutter

    private const float BgmDefaultVolume = 0.45f; // relative to the hit/action SFX - tune per stage from the Inspector, no code change needed

    private static readonly Color[] GroundTints =
    {
        new Color(0.55f, 0.42f, 0.30f),
        new Color(0.50f, 0.38f, 0.27f),
        new Color(0.45f, 0.34f, 0.24f),
        new Color(0.40f, 0.30f, 0.21f),
    };

    private static readonly Color[] BackgroundTints =
    {
        new Color(0.55f, 0.55f, 0.55f), // 배경1 - nearest
        new Color(0.62f, 0.62f, 0.62f), // 배경2
        new Color(0.70f, 0.70f, 0.70f), // 배경3
        new Color(0.80f, 0.80f, 0.80f), // 배경4
        new Color(0.88f, 0.88f, 0.90f), // 배경5 - furthest / sky
    };

    // 0 = scrolls like normal level geometry, 1 = moves exactly with the camera (looks fixed/static).
    private static readonly float[] BackgroundParallaxFactors = { 0.15f, 0.35f, 0.55f, 0.75f, 1f };

    [MenuItem("Tools/Class Template/Create Background Test Scene")]
    public static void CreateBackgroundTestScene()
    {
        // This rebuilds BackgroundTest.unity from scratch (default 4 ground pieces, 5
        // background layers) and overwrites whatever is currently saved there - no undo.
        // Once someone has hand-edited the scene (added ground pieces, split colliders,
        // moved things around), running this again silently destroys that work. Warn
        // every time the target file already exists, rather than trusting people to
        // remember not to click this out of habit.
        if (File.Exists(ScenePath))
        {
            var confirmed = ActionTestSceneBuilder.ConfirmDestructive(
                "Background Test 씬 다시 만들기",
                $"{ScenePath}\n\n이 파일이 이미 있습니다. 계속하면 지금 씬의 모든 내용(바닥 배치, 콜리전 수정, Wire 연결 등 포함)이 사라지고 기본 상태(바닥 4개)로 새로 만들어집니다.\n\n되돌릴 수 없습니다. 정말 새로 만드시겠습니까?",
                "그래도 새로 만들기");

            if (!confirmed)
            {
                Debug.Log("Create Background Test Scene: 취소됨 - 기존 씬을 그대로 유지합니다.");
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? string.Empty);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Runs inside the fresh scene we just created (and are about to save as
        // BackgroundTest.unity) rather than whatever scene happened to be open before,
        // so the temporary prefab-authoring objects below never touch other work.
        EnsurePrefabs();

        const float startX = 0f;

        var player = ActionTestSceneBuilder.CreatePlayer();
        player.name = "Player_BackgroundTest";
        player.transform.position = new Vector3(startX, GroundTopY, 0f);

        // Ground first, so its transform can be wired into the camera and background
        // layers as their auto-bounds source (see GroundBoundsUtility).
        var groundRoot = CreateGroundSequence(startX);
        CreateCamera(player.transform, groundRoot.transform);
        CreateBackgroundLayers(startX, groundRoot.transform);
        CreateFallDeathZone(groundRoot.transform);
        CreateBackgroundMusic();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created background test scene: {ScenePath}");
    }

    private static void CreateCamera(Transform target, Transform groundParent)
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
        follow.SetGroundParent(groundParent); // bounds recomputed from Ground's children every Awake - see GroundBoundsUtility

        CreateWeatherEffect(cameraObject.transform);
    }

    // ---- Fall death (구멍에 떨어지면 사망) ---------------------------------------------

    /// <summary>
    /// Creates the wide invisible "FallDeathZone" trigger below the stage that kills the
    /// player if they fall through a gap in the ground - see FallDeathZone.cs. Idempotent
    /// (does nothing if one already exists), so it's safe to call from both a fresh scene
    /// build and the standalone "Add Fall Death Zone To Scene" command on an existing scene.
    /// Monsters never need this - MonsterGroundGuard stops MonsterA/B at the edge of any gap
    /// instead, whether walking on their own or knocked back by a hit.
    /// </summary>
    private static void CreateFallDeathZone(Transform groundParent)
    {
        if (GameObject.Find("FallDeathZone") != null)
        {
            return;
        }

        var zoneObject = new GameObject("FallDeathZone", typeof(BoxCollider2D), typeof(FallDeathZone));
        zoneObject.GetComponent<FallDeathZone>().SetGroundParent(groundParent);
    }

    /// <summary>
    /// Adds the fall-death trigger to whatever scene is currently open (must already have a
    /// "Ground" object) without touching anything else - unlike "Create Background Test
    /// Scene", this never rebuilds the scene. Use this once to add it to an existing
    /// hand-edited BackgroundTest.unity. Safe to rerun (does nothing if already present).
    /// </summary>
    [MenuItem("Tools/Class Template/Add Fall Death Zone To Scene")]
    public static void AddFallDeathZoneToScene()
    {
        var groundRoot = GameObject.Find("Ground");
        if (groundRoot == null)
        {
            Debug.LogWarning("No 'Ground' object found in the open scene - open BackgroundTest.unity first.");
            return;
        }

        CreateFallDeathZone(groundRoot.transform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Fall death zone added below the stage (auto-sized from the 'Ground' object's children). Save the scene (Ctrl+S).");
    }

    // ---- Background music (BGM) ----------------------------------------------------
    //
    // ActionTest.unity intentionally never gets this - that scene is for listening to
    // hit/action SFX in isolation, and a looping music track would get in the way. BGM
    // only plays in BackgroundTest.unity. Only one track is supported at a time (drop a
    // single audio file into BgmFolder); volume is the only knob students need, tuned
    // directly on the "BackgroundMusic" object's AudioSource in the Inspector.

    /// <summary>
    /// Creates the "BackgroundMusic" object (a plain AudioSource, looping, 2D, no
    /// positional falloff) and points it at whatever single audio file is sitting in
    /// BgmFolder. Safe to call from both a fresh scene build and the standalone "Add
    /// Background Music To Scene" command on an existing scene: if the object already
    /// exists, loop/volume/etc are left alone (a student's manual tuning survives a
    /// rerun), but the clip is re-checked and filled in if it's still empty (2026-08-18
    /// fix) - covers running this before a BGM file existed in BgmFolder, then dropping
    /// the file in afterwards; previously the object-existence check alone made the
    /// missing clip permanent and rerunning did nothing, contradicting the warning below.
    /// </summary>
    private static void CreateBackgroundMusic()
    {
        var musicObject = GameObject.Find("BackgroundMusic");
        AudioSource source;
        if (musicObject == null)
        {
            musicObject = new GameObject("BackgroundMusic", typeof(AudioSource));
            source = musicObject.GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f; // 2D - same volume no matter where the camera/player is
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
            source.clip = FindBgmClip();
        }

        if (source.clip == null)
        {
            Debug.LogWarning($"BackgroundMusic object created but no clip assigned - drop one audio file into {BgmFolder} and run 'Add Background Music To Scene' again.");
        }
    }

    /// <summary>
    /// Scans BgmFolder for a single audio file rather than requiring an exact filename,
    /// so any format/name the student drops in (mp3, wav, ogg...) just works. Warns
    /// (doesn't fail) if the folder is empty or has more than one file - only one BGM
    /// track is supported at a time.
    /// </summary>
    private static AudioClip FindBgmClip()
    {
        if (!Directory.Exists(BgmFolder))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var file in Directory.GetFiles(BgmFolder))
        {
            if (file.ToLowerInvariant().EndsWith(".meta"))
            {
                continue;
            }

            candidates.Add(file.Replace('\\', '/'));
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No BGM file found in {BgmFolder}. Drop one audio file in there and rerun.");
            return null;
        }

        candidates.Sort();
        if (candidates.Count > 1)
        {
            Debug.LogWarning($"Multiple files found in {BgmFolder}; using the first one alphabetically ({Path.GetFileName(candidates[0])}). Only one BGM track is supported - remove the others.");
        }

        var assetPath = candidates[0];
        ConfigureBgmImportSettings(assetPath);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    /// <summary>
    /// A full-length looping music track should stream from disk rather than load
    /// entirely into memory (which is what short one-shot SFX use) - fixes this on
    /// import so it's correct even if the student just drags the file into the folder
    /// and never opens its Inspector.
    /// </summary>
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

    /// <summary>
    /// Adds background music to whatever scene is currently open, without touching
    /// anything else - unlike "Create Background Test Scene", this never rebuilds the
    /// scene. Use this once to add it to an existing hand-edited BackgroundTest.unity.
    /// Safe to rerun (does nothing if already present).
    /// </summary>
    [MenuItem("Tools/Class Template/Add Background Music To Scene")]
    public static void AddBackgroundMusicToScene()
    {
        CreateBackgroundMusic();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Background music added (or already present) - see the 'BackgroundMusic' object's AudioSource. Adjust Volume there to balance against SFX. Save the scene (Ctrl+S).");
    }

    // ---- Ground (바닥1~4) ----------------------------------------------------------

    private static GameObject CreateGroundSequence(float startX)
    {
        var root = new GameObject("Ground");
        for (var i = 0; i < GroundCount; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GroundPrefabFolder}/Ground{i + 1}.prefab");
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

    private static void EnsureGroundPrefabs()
    {
        for (var i = 1; i <= GroundCount; i++)
        {
            EnsureGroundPrefab(i);
        }
    }

    /// <summary>
    /// Creates GroundN.prefab (1-based index) with a placeholder tinted visual + one
    /// full-width collision box, if it doesn't already exist. Never overwrites an
    /// existing prefab - safe to call for any index, including ones beyond GroundCount
    /// (e.g. index 5, 6, ...), which is what lets ApplyRealStageArt add new patterns
    /// without this file needing an edit.
    /// </summary>
    private static void EnsureGroundPrefab(int index)
    {
        Directory.CreateDirectory(GroundPrefabFolder);
        var path = $"{GroundPrefabFolder}/Ground{index}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return; // don't clobber a pattern the professor/student has already customized or swapped art on
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
        renderer.sortingOrder = 20; // in front of the player, per the front-to-back order: 바닥 -> 캐릭터 -> 배경1~5

        // Default collision box, spanning the full tile. Duplicate this child in the
        // Scene view and reposition/resize the copies to punch a fall-through gap -
        // any number of boxes is fine, this is not hardcoded to one.
        var collision = new GameObject("Collision", typeof(BoxCollider2D));
        collision.transform.SetParent(root.transform, false);
        collision.transform.localPosition = new Vector3(0f, GroundTopY - GroundThickness / 2f, 0f);
        collision.layer = LayerMask.NameToLayer("Default"); // matches PlayerActionTestController's default groundMask
        var box = collision.GetComponent<BoxCollider2D>();
        box.size = new Vector2(TileWorldWidth, GroundThickness);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // ---- Background layers (배경1~5) ------------------------------------------------

    private static void CreateBackgroundLayers(float startX, Transform groundParent)
    {
        var root = new GameObject("Background");
        for (var i = 0; i < BackgroundLayerCount; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BackgroundPrefabFolder}/BG{i + 1}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"Missing background prefab BG{i + 1}; run this command again after EnsurePrefabs creates it.");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"BG{i + 1}_Instance";
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = new Vector3(startX, 0f, 0f);
            instance.GetComponent<ParallaxLayer>().SetGroundParent(groundParent); // tiles rebuilt from Ground's children every Start
        }
    }

    private static void EnsureBackgroundPrefabs()
    {
        for (var i = 1; i <= BackgroundLayerCount; i++)
        {
            EnsureBackgroundPrefab(i);
        }
    }

    /// <summary>
    /// Creates BGN.prefab (1-based index) with a placeholder tinted, tiled layer, if it
    /// doesn't already exist. Never overwrites an existing prefab - safe to call for any
    /// index, including ones beyond BackgroundLayerCount, which is what lets
    /// ApplyRealStageArt add new layers (e.g. BG6) without this file needing an edit.
    /// Indices past the tuned tint/parallax arrays reuse the last (farthest/most-static)
    /// entry as a reasonable default - adjust ParallaxLayer's factor by hand afterward if needed.
    /// </summary>
    private static void EnsureBackgroundPrefab(int index)
    {
        Directory.CreateDirectory(BackgroundPrefabFolder);
        var path = $"{BackgroundPrefabFolder}/BG{index}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        var sprite = EnsurePlaceholderSprite();
        var root = new GameObject($"BG{index}", typeof(ParallaxLayer));
        var sortingOrder = -10 * (index - 1); // 배경1 = 0, 배경2 = -10, ... (all behind the player at 10 and ground at 20)

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

    // ---- Shared placeholder art ------------------------------------------------------

    private static void EnsurePrefabs()
    {
        EnsureGroundPrefabs();
        EnsureBackgroundPrefabs();
        EnsureWeatherPrefab();
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
        // Intentionally NOT BackgroundPixelsPerUnit here. This 4x4 texture is only ever
        // resized via transform.localScale, so its *native* size must be exactly 1 world
        // unit (texture pixels / PPU = 4 / 4 = 1) for those scale values to equal world
        // units directly - same trick ActionTestSceneBuilder.CreateRuntimeSquareSprite()
        // uses. BackgroundPixelsPerUnit (100) is the import setting for the real
        // 1920x1080 art later, not for this placeholder.
        importer.spritePixelsPerUnit = 4f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderTexturePath);
    }

    // ---- Weather effect (눈/비/꽃잎 파티클) -------------------------------------------

    /// <summary>
    /// Creates WeatherFX.prefab (one Particle System) if it doesn't already exist - never
    /// overwrites an existing prefab, so it's safe to call on every EnsurePrefabs() run
    /// without clobbering module tuning the professor/students have done in the Inspector.
    /// Tuned as a straight-down "snow" starting point:
    ///  - Start Speed is 0 - all motion comes from Velocity over Lifetime instead, so
    ///    "where it spawns" (Shape) and "how it moves" (Velocity over Lifetime) stay two
    ///    separate, easy-to-explain knobs.
    ///  - Uses the shared placeholder dot texture until ApplyRealStageArt() (via
    ///    ApplyWeatherArt()) swaps in the real weatherN.png frames from StageWeather/.
    /// Deliberately generic ("WeatherFX", not "SnowFX") - only one weather effect is ever
    /// active in the stage at a time, and if a later class wants rain or petals instead,
    /// they just swap the PNGs in StageWeather/ and re-tune this same prefab's modules.
    /// </summary>
    private static void EnsureWeatherPrefab()
    {
        Directory.CreateDirectory(WeatherPrefabFolder);
        var path = $"{WeatherPrefabFolder}/WeatherFX.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return; // don't clobber tuning the professor/students already did
        }

        var material = EnsureWeatherMaterial();
        var root = new GameObject("WeatherFX", typeof(ParticleSystem));
        var particleSystem = root.GetComponent<ParticleSystem>();
        ApplyWeatherDefaultTuning(particleSystem);

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = WeatherSortingOrder;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// Sets WeatherFX's speed/amount/size/fall-motion tuning (Main, Emission, Shape,
    /// Velocity over Lifetime) to this project's shipped defaults - "snow falling straight
    /// down." Shared by EnsureWeatherPrefab() (first-time creation) and
    /// ResetWeatherSettingsToDefaults() (undoing broken Inspector experimenting), so the two
    /// can never drift apart. Deliberately does NOT touch the Renderer's material/texture or
    /// the Texture Sheet Animation module - frame art is a separate concern, changed only via
    /// StageWeather/ + Apply Real Stage Art, not by this "reset my numbers" tuning.
    /// </summary>
    private static void ApplyWeatherDefaultTuning(ParticleSystem particleSystem)
    {
        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startSpeed = 0f; // no shape-direction velocity - Velocity over Lifetime below drives all motion
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad); // radians, not degrees
        main.startColor = Color.white; // no tint - shows the source PNG's own colors as-is
        main.gravityModifier = 0f;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // already-falling particles ignore the anchor moving with the camera

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
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); // gentle side drift
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-3f, -2f); // fall speed (negative = downward)
        // x/y/z MUST share the same MinMaxCurve mode (all "TwoConstants" here) or Unity
        // logs "Particle Velocity curves must all be in the same mode" every frame and the
        // whole module silently fails to apply - leaving particles frozen at their spawn
        // position instead of falling. z stays at 0 (this is a 2D game) but still needs the
        // TwoConstants(0, 0) form, not the single-value Constant(0) form.
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    /// <summary>
    /// Resets WeatherFX's speed/amount/size/fall-motion tuning back to this project's
    /// shipped defaults - for when a student's Inspector experimenting goes wrong badly
    /// enough that the normal "Revert" (right-click component > Revert, or the Overrides
    /// dropdown) can't help anymore: Revert only restores an instance FROM the prefab, and
    /// if the bad values were already pushed onto the prefab itself via "Apply" (or the
    /// scene/project was saved in a broken state), there's nothing left to revert to.
    /// Fixes both WeatherFX.prefab AND, if BackgroundTest.unity is the open scene and
    /// already has the weather effect added, the live WeatherFX_Instance there too
    /// (clearing any of its un-applied local overrides in the same pass).
    /// Does NOT touch weather art (StageWeather/ PNGs, the generated atlas, frame count) -
    /// that's a separate, deliberate content choice, not something this "oops, reset my
    /// numbers" command should undo. Use "Apply Real Stage Art" for that instead.
    /// </summary>
    [MenuItem("Tools/Class Template/Reset Weather Settings To Defaults")]
    public static void ResetWeatherSettingsToDefaults()
    {
        EnsureWeatherPrefab(); // in case WeatherFX.prefab was deleted entirely
        var path = $"{WeatherPrefabFolder}/WeatherFX.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"WeatherFX prefab not found at {path}.");
            return;
        }

        ApplyWeatherDefaultTuning(root.GetComponent<ParticleSystem>());
        root.GetComponent<ParticleSystemRenderer>().sortingOrder = WeatherSortingOrder;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        var fixedSceneInstance = false;
        var cameraObject = GameObject.FindWithTag("MainCamera");
        var instanceTransform = cameraObject != null ? cameraObject.transform.Find("WeatherAnchor/WeatherFX_Instance") : null;
        var instanceParticleSystem = instanceTransform != null ? instanceTransform.GetComponent<ParticleSystem>() : null;
        if (instanceParticleSystem != null)
        {
            ApplyWeatherDefaultTuning(instanceParticleSystem);
            instanceTransform.GetComponent<ParticleSystemRenderer>().sortingOrder = WeatherSortingOrder;
            EditorUtility.SetDirty(instanceParticleSystem);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            fixedSceneInstance = true;
        }

        AssetDatabase.SaveAssets();
        Debug.Log(fixedSceneInstance
            ? "Weather settings reset to defaults on WeatherFX.prefab and the open scene's WeatherFX_Instance. Save the scene (Ctrl+S). Frame art (StageWeather PNGs) was left untouched."
            : "Weather settings reset to defaults on WeatherFX.prefab. (No WeatherFX_Instance found in the open scene - open BackgroundTest.unity to also fix a placed instance.) Frame art (StageWeather PNGs) was left untouched.");
    }

    private static Material EnsureWeatherMaterial()
    {
        var path = $"{WeatherPrefabFolder}/WeatherFX_Mat.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(WeatherPrefabFolder);
        var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        var material = new Material(shader) { mainTexture = EnsurePlaceholderSprite().texture };
        // This shader's _TintColor defaults to a dull 50% gray/50% alpha, not white - without
        // this line the particle renders gray no matter what the source PNG looks like.
        material.SetColor("_TintColor", Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// Instantiates WeatherFX.prefab as a child of the camera (under a "WeatherAnchor"
    /// empty) so its spawn area always tracks the camera as CameraFollow2D moves it - see
    /// EnsureWeatherPrefab()'s World simulation space note for why already-falling
    /// particles don't get dragged along too. Idempotent: does nothing if a WeatherAnchor
    /// already exists under the given camera transform, so it's safe to call both from a
    /// fresh scene build and from the standalone "Add Weather Effect To Scene" command on
    /// an already-built scene.
    /// </summary>
    private static void CreateWeatherEffect(Transform cameraTransform)
    {
        if (cameraTransform.Find("WeatherAnchor") != null)
        {
            return;
        }

        EnsureWeatherPrefab();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeatherPrefabFolder}/WeatherFX.prefab");
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

    /// <summary>
    /// Adds the weather effect to whatever scene is currently open (must already have a
    /// "MainCamera"-tagged object) without touching anything else - unlike "Create
    /// Background Test Scene", this never rebuilds the scene. Use this once to add weather
    /// to an existing hand-edited BackgroundTest.unity. Safe to rerun (does nothing if
    /// already wired). To turn the effect off later, just uncheck WeatherAnchor (or
    /// WeatherFX_Instance) in the Hierarchy - no script/menu needed for that.
    /// </summary>
    [MenuItem("Tools/Class Template/Add Weather Effect To Scene")]
    public static void AddWeatherEffectToScene()
    {
        var cameraObject = GameObject.FindWithTag("MainCamera");
        if (cameraObject == null)
        {
            Debug.LogWarning("No Main Camera found in the open scene - open BackgroundTest.unity first.");
            return;
        }

        CreateWeatherEffect(cameraObject.transform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Weather effect added under Main Camera (WeatherAnchor/WeatherFX_Instance). Save the scene (Ctrl+S). Drop PNG frames into StageWeather/ and run 'Apply Real Stage Art' to swap in the real art - until then it uses the placeholder dot.");
    }

    // ---- Real art (once dropped into StageGround / StageBackground) -----------------

    /// <summary>
    /// Swaps the placeholder tint blocks on the GroundN / BGN prefabs for the real
    /// 1920x1080 PNGs sitting in RealGroundArtFolder / RealBackgroundArtFolder. Scans
    /// those folders for however many GroundN.png / BGN.png files actually exist - NOT
    /// limited to GroundCount/BackgroundLayerCount, so adding Ground5, Ground6, BG6, ...
    /// never requires touching this script again: draw the PNG, drop it in with the right
    /// name, rerun this command. Creates the matching prefab if it doesn't exist yet.
    ///
    /// Does NOT touch BackgroundTest.unity. Safe to rerun any time without losing scene
    /// edits (like hand-split ground colliders) - unlike "Create Background Test Scene",
    /// which rebuilds the whole scene from scratch and should NOT be rerun once you've
    /// started hand-editing it.
    ///
    /// Real art is imported at BackgroundPixelsPerUnit (100), so its *native* size is
    /// already 19.2 x 10.8 world units - exactly one screen - so unlike the placeholder,
    /// no transform.localScale multiplier is applied; sprites are centered (pivot 0.5,0.5)
    /// at local (0,0,0)/tile-X-offset with scale 1. Measured against the actual ground art:
    /// the opaque ground silhouette starts ~81% down the 1920x1080 canvas, which lines up
    /// with GroundTopY (-3.4) almost exactly when the image top sits at the camera's top
    /// edge (Y = 5.4) - i.e. this centered placement needs no manual per-pattern offset.
    /// </summary>
    [MenuItem("Tools/Class Template/Apply Real Stage Art")]
    public static void ApplyRealStageArt()
    {
        foreach (var index in ScanNumberedFiles(RealGroundArtFolder, "ground"))
        {
            var sprite = ImportRealArtSprite($"{RealGroundArtFolder}/ground{index}.png");
            if (sprite == null)
            {
                continue;
            }

            EnsureGroundPrefab(index);
            ApplyGroundSprite(index, sprite);
        }

        foreach (var index in ScanNumberedFiles(RealBackgroundArtFolder, "bg"))
        {
            var sprite = ImportRealArtSprite($"{RealBackgroundArtFolder}/bg{index}.png");
            if (sprite == null)
            {
                continue;
            }

            EnsureBackgroundPrefab(index);
            ApplyBackgroundSprite(index, sprite);
        }

        ApplyWeatherArt();

        AssetDatabase.SaveAssets();
        Debug.Log("Applied real stage art to Ground/Background/Weather prefabs. Drag any newly-created prefab from the Project window into the BackgroundTest scene to place it (Weather: run 'Add Weather Effect To Scene' instead) - this command does not touch the scene itself.");
    }

    /// <summary>
    /// Finds files named "{prefix}{N}.png" in folder and returns the N's, sorted.
    /// This is what makes ground/background pattern counts unlimited - no hardcoded loop bound.
    /// </summary>
    private static List<int> ScanNumberedFiles(string folder, string prefix)
    {
        return ScanNumberedFiles(folder, prefix, "png");
    }

    private static List<int> ScanNumberedFiles(string folder, string prefix, string extension)
    {
        var indices = new List<int>();
        if (!Directory.Exists(folder))
        {
            return indices;
        }

        foreach (var file in Directory.GetFiles(folder, $"{prefix}*.{extension}"))
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

    private static void ApplyGroundSprite(int index, Sprite sprite)
    {
        var path = $"{GroundPrefabFolder}/Ground{index}.prefab";
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
            renderer.color = Color.white; // clear the placeholder tint - let the real art's own colors show
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one; // native size already = 19.2 x 10.8 world units at 100 PPU
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ApplyBackgroundSprite(int index, Sprite sprite)
    {
        var path = $"{BackgroundPrefabFolder}/BG{index}.prefab";
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
            tile.localScale = Vector3.one; // native size already = 19.2 x 10.8 world units at 100 PPU
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    /// <summary>
    /// Scans RealWeatherArtFolder for WeatherN.png frames, combines whatever's there into
    /// one grid atlas texture, and wires it into WeatherFX.prefab's Texture Sheet Animation
    /// module. Does nothing (keeps the placeholder dot) if no WeatherN.png exists yet.
    /// </summary>
    private static void ApplyWeatherArt()
    {
        var indices = ScanNumberedFiles(RealWeatherArtFolder, "weather");
        if (indices.Count == 0)
        {
            return;
        }

        EnsureWeatherPrefab(); // make sure WeatherFX.prefab exists before we edit it

        var framePaths = new List<string>();
        foreach (var index in indices)
        {
            framePaths.Add($"{RealWeatherArtFolder}/weather{index}.png");
        }

        var atlas = BuildWeatherAtlas(framePaths, out var frameCount);
        if (atlas == null)
        {
            Debug.LogWarning("Weather art found but the atlas build failed - check the Console for texture read errors.");
            return;
        }

        ApplyWeatherAtlasToPrefab(atlas, frameCount);
        Debug.Log($"Applied {frameCount} weather frame(s) to WeatherFX.prefab (Texture Sheet Animation, {frameCount}x1 grid).");
    }

    /// <summary>
    /// Combines however many weatherN.png frames exist into one N x 1 grid texture (all
    /// frames in a single row, cell size = the largest frame's dimensions) and saves it as
    /// weatheratlas_generated.png next to the source frames. The particle system's Texture
    /// Sheet Animation module (Grid mode) reads from one texture, not several separate
    /// ones, so this is what actually gets assigned to the material. Regenerated every time
    /// this runs, so adding weather3.png later and rerunning just rebuilds a wider atlas -
    /// no manual work. Frames are placed at the bottom-left of their cell, not stretched, so
    /// keep source frames the same size (see StageWeather/README.txt).
    /// </summary>
    private static Texture2D BuildWeatherAtlas(List<string> framePaths, out int frameCount)
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
        atlas.SetPixels32(new Color32[atlas.width * atlas.height]); // fully transparent to start

        for (var i = 0; i < sourceTextures.Count; i++)
        {
            var texture = sourceTextures[i];
            atlas.SetPixels(i * cellSize, 0, texture.width, texture.height, texture.GetPixels());
        }

        atlas.Apply();

        var atlasPath = $"{RealWeatherArtFolder}/weatheratlas_generated.png";
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

    private static void ApplyWeatherAtlasToPrefab(Texture2D atlas, int frameCount)
    {
        var path = $"{WeatherPrefabFolder}/WeatherFX.prefab";
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
            // Reapplied every run (not just on first creation) so this also fixes an
            // already-saved material that predates this line - see EnsureWeatherMaterial().
            renderer.sharedMaterial.SetColor("_TintColor", Color.white);
        }

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
        textureSheetAnimation.numTilesX = frameCount;
        textureSheetAnimation.numTilesY = 1;
        textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
        // Real flip-book animation: each particle plays through every frame as it falls
        // (not a fixed random pick) - lets a student's own hand-drawn 2-3 frame animation
        // actually animate on screen, not just look like static confetti with varied shapes.
        textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        textureSheetAnimation.cycleCount = WeatherAnimationCycles; // how many full loops per particle lifetime - tune alongside Start Lifetime for the flutter speed you want
        // startFrame is a 0..1 FRACTION across the whole sheet, not a literal frame index
        // (confirmed by round-tripping through the real Editor - passing frameCount here
        // gets silently clamped to ~1). Randomizing it just staggers each particle's
        // animation phase so they don't all flip in sync - frameOverTime above is what
        // actually makes each one loop.
        textureSheetAnimation.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---- Wiring auto-bounds (camera clamp + background tile coverage) ----------------

    /// <summary>
    /// Points the camera's CameraFollow2D and every BGN instance's ParallaxLayer at the
    /// "Ground" object in the currently-open scene. This is a ONE-TIME wiring step, not a
    /// bake: from then on, both recompute their bounds/tile coverage automatically every
    /// time the scene loads (CameraFollow2D.Awake / ParallaxLayer.Start), by measuring
    /// whatever GroundN instances actually exist at that moment. So adding, removing, or
    /// reordering ground pieces later never requires re-running this - only re-run it if
    /// you rename/replace the "Ground" or "Background" root objects themselves.
    /// Freshly-built scenes (via Create Background Test Scene) are already wired; this is
    /// for reconnecting an existing hand-edited scene if that wiring was ever lost.
    /// </summary>
    [MenuItem("Tools/Class Template/Wire Auto Level Bounds")]
    public static void WireAutoLevelBounds()
    {
        var groundRoot = GameObject.Find("Ground");
        if (groundRoot == null)
        {
            Debug.LogWarning("No 'Ground' object found in the open scene - open BackgroundTest.unity first.");
            return;
        }

        var cameraObject = GameObject.FindWithTag("MainCamera");
        var follow = cameraObject != null ? cameraObject.GetComponent<CameraFollow2D>() : null;
        if (follow != null)
        {
            follow.SetGroundParent(groundRoot.transform);
            EditorUtility.SetDirty(follow);
        }
        else
        {
            Debug.LogWarning("No CameraFollow2D found on the Main Camera.");
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
        else
        {
            Debug.LogWarning("No 'Background' object found in the open scene.");
        }

        var deathZone = GameObject.Find("FallDeathZone");
        var fallDeathZone = deathZone != null ? deathZone.GetComponent<FallDeathZone>() : null;
        if (fallDeathZone != null)
        {
            fallDeathZone.SetGroundParent(groundRoot.transform);
            EditorUtility.SetDirty(fallDeathZone);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Wired auto level bounds: camera={(follow != null)}, background layers wired={wiredLayers}, fall death zone={(fallDeathZone != null)}. Save the scene (Ctrl+S) - the actual bounds/tiles are computed automatically every time you press Play, not baked here.");
    }
}
