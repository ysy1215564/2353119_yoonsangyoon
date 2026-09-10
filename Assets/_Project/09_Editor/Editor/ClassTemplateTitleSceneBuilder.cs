using System.Collections.Generic;
using System.IO;
using GameProject.Audio;
using GameProject.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Builds "Title.unity" from scratch: background+logo art, watermark overlay, five main buttons,
/// three popups (조작설명/설정/크레딧) each with their own art + close hit-area, and the settings
/// popup's BGM/효과음 sliders + 저장후닫기 button. Also doubles as the class's title *test* scene -
/// there's no separate production-only title scene, matching how ActionTest/BackgroundTest work.
///
/// 2026-08-10 rewrite: replaces the 2026-07-27 placeholder version entirely (old
/// TitleMenuController/PopupDismissArea behavior - toggle-panel, click-anywhere-to-dismiss - does
/// not match the confirmed spec: modal popups, X = cancel vs 저장후닫기 = save, real BGM/SFX
/// volume via AudioMixer). See ClassDocs WorkLog for the session this was decided in.
/// </summary>
public static class ClassTemplateTitleSceneBuilder
{
    private const string ScenePath = "Assets/_Project/00_Scenes/Flow/Title.unity";
    private const string TitleArtPath = "Assets/_Project/04_Art/StudentReplace/UI/Title";
    private const string TitleBgmFolder = "Assets/_Project/06_Audio/BGM/Title";
    private const float TitleBgmDefaultVolume = 0.45f; // same convention as BackgroundTestSceneBuilder.BgmDefaultVolume

    private const string SfxRoot = "Assets/_Project/06_Audio/SFX";
    private const string UiClickClipBaseName = "mouseclick"; // 06_Audio/SFX/mouseclick.* (확장자 무관)
    private const string SfxPreviewClipFolder = SfxRoot + "/Player";
    private const string SfxPreviewClipBaseName = "player_attack1_swing"; // 효과음 슬라이더 조절할 때 미리듣기용

    private const string TitleMotionFramePrefix = "title_motion"; // title_motion01.png, title_motion02.png ...
    private const float TitleMotionDefaultFps = 5f; // 사용자 확정: 느긋한 느낌, 나중에 인스펙터에서 숫자만 바꾸면 됨

    // 팝업 아트(602x715) 위 X 닫기 아이콘의 대략적인 위치 - 세 팝업(조작설명/설정/크레딧) 모두 같은 틀을
    // 재사용해서 X 위치도 동일하다. 정확한 좌표가 아니라 목업을 보고 잡은 대략값이라, 실제로는
      // "Tools/Class Template/Title Preview"로 팝업을 켜놓고 눈으로 보면서 다시 맞춰야 한다.
    private static readonly Vector2 PopupSize = new Vector2(602, 715);
    private static readonly Vector2 PopupCloseHitAreaPosition = new Vector2(219, 298);
    private static readonly Vector2 PopupCloseHitAreaSize = new Vector2(70, 70);

    [MenuItem("Tools/Class Template/Create Title Scene")]
    public static void CreateTitleScene()
    {
        Directory.CreateDirectory(TitleBgmFolder);
        AssetDatabase.Refresh(); // new PNGs (renamed/copied outside Unity) need an importer before ImportTitleSprites can touch them
        ImportTitleSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // UI-only 씬이라 카메라가 시각적으로 하는 일은 없지만(Screen Space Overlay 캔버스는 카메라 없이도
        // 그려짐), 카메라가 하나도 없으면 Game 뷰에 "No cameras rendering" 경고가 계속 뜨고 - 더 중요하게는
        // - AudioListener를 붙일 곳이 없어서 BGM/효과음이 하나도 안 들린다. MainCamera 태그를 붙여서 다른
        // 씬들과 같은 관례(EnsureAudioListenerOnMainCamera가 찾는 방식)를 따른다.
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        // 씬이 열릴 때마다 저장된 BGM/효과음 볼륨을 자동 적용 - 다른 씬들과 달리 별도 도구를 안 돌려도
        // 되게 Title 빌더 자체에 포함(이 씬은 Create Title Scene을 다시 돌리면 통째로 새로 만들어지므로).
        new GameObject("AudioSettingsBootstrap", typeof(AudioSettingsBootstrap));

        var uiClickSfx = CreateUiClickSfx();

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasContainScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // 1(높이만 반영) - 에디터 미리보기/빌드 시점 기본값일 뿐, 실제 값은 실행 시점에 CanvasContain-
        // Scaler.Awake()가 화면 비율을 보고 0(가로 기준) 또는 1(세로 기준)으로 다시 정한다. 이 씬의
        // 배경 이미지들은 스트레치가 아니라 1920x1080 고정 크기를 중앙에 놓고 CanvasScaler의 배율만으로
        // 키우는 방식이라, 고정값 하나로는 16:9보다 넓은 화면(Galaxy S21 등)과 좁은 화면(Galaxy Tab S10
        // 등)을 동시에 안 잘리게 만들 수 없다 - 자세한 배경은 CanvasContainScaler 클래스 주석 참고.
        scaler.matchWidthOrHeight = 1f;

        var controllerObject = new GameObject("TitleMenuController", typeof(TitleMenuController));
        var controller = controllerObject.GetComponent<TitleMenuController>();

        // ---- 1. Background (가장 아래) --------------------------------------------------
        CreateImage("Background", canvasObject.transform, Sprite("title_background"), Vector2.zero, new Vector2(1920, 1080), false);

        // ---- 1.5. 타이틀 배경 애니메이션 (Background 바로 위, 버튼/팝업과 무관하게 항상 재생) -------
        CreateTitleMotion(canvasObject.transform);

        // ---- 2. 메인 버튼 5개 --------------------------------------------------------------
        // 목업 화면을 보고 잡은 대략 위치(중앙정렬 4개 스택 + 우하단 종료) - 정확한 좌표는 나중에
        // Scene 뷰에서 직접 드래그해서 맞추면 된다.
        var startButton = CreateMainButton("StartButton", canvasObject.transform, "button_start", new Vector2(0, 40));
        var controlsButton = CreateMainButton("ControlsButton", canvasObject.transform, "button_controls", new Vector2(0, -60));
        var settingsButton = CreateMainButton("SettingsButton", canvasObject.transform, "button_settings", new Vector2(0, -160));
        var creditsButton = CreateMainButton("CreditsButton", canvasObject.transform, "button_credits", new Vector2(0, -260));
        var quitButton = CreateMainButton("QuitButton", canvasObject.transform, "button_quit", new Vector2(-140, 120), new Vector2(1f, 0f));

        // ---- 3. 모달 차단막 (버튼들 위, 팝업들 아래) ----------------------------------------
        var modalBlocker = CreateFullScreenBlocker("ModalBlocker", canvasObject.transform);
        modalBlocker.SetActive(false);

        // ---- 4. 팝업 3종 (모달 차단막 위) ---------------------------------------------------
        var controlsPopup = CreateSimplePopup("ControlsPopup", canvasObject.transform, "popup_controls", controller, nameof(TitleMenuController.CloseCurrentPopup), uiClickSfx);
        var creditsPopup = CreateSimplePopup("CreditsPopup", canvasObject.transform, "popup_credits", controller, nameof(TitleMenuController.CloseCurrentPopup), uiClickSfx);
        var settingsPopup = CreateSettingsPopup(canvasObject.transform, controller, uiClickSfx);

        // ---- 5. 워터마크 (항상 최상단, 클릭 통과) -------------------------------------------
        CreateImage("Watermark", canvasObject.transform, Sprite("title_watermark"), Vector2.zero, new Vector2(1920, 1080), false);

        // ---- Controller 필드 연결 ----------------------------------------------------------
        var so = new SerializedObject(controller);
        so.FindProperty("controlsPopup").objectReferenceValue = controlsPopup;
        so.FindProperty("settingsPopup").objectReferenceValue = settingsPopup;
        so.FindProperty("creditsPopup").objectReferenceValue = creditsPopup;
        so.FindProperty("modalBlocker").objectReferenceValue = modalBlocker;
        so.FindProperty("startSceneName").stringValue = "IntroCutscene"; // 인트로 컷신 완성 - 더 이상 BackgroundTest로 임시 연결 아님
        so.ApplyModifiedPropertiesWithoutUndo();

        WireMainButton(startButton, controller, nameof(TitleMenuController.StartGame), uiClickSfx);
        WireMainButton(controlsButton, controller, nameof(TitleMenuController.OpenControls), uiClickSfx);
        WireMainButton(settingsButton, controller, nameof(TitleMenuController.OpenSettings), uiClickSfx);
        WireMainButton(creditsButton, controller, nameof(TitleMenuController.OpenCredits), uiClickSfx);
        WireMainButton(quitButton, controller, nameof(TitleMenuController.QuitGame), uiClickSfx);

        CreateBackgroundMusic();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created title scene: {ScenePath}. 버튼/팝업 위치는 대략값이니 Scene 뷰에서 직접 맞추고, 팝업 확인은 'Tools/Class Template/Title Preview' 메뉴로 켜서 볼 것.");
    }

    // ---- Background music (title BGM) -----------------------------------------------------
    // BackgroundTestSceneBuilder와 동일한 패턴: 폴더에 파일 하나만 넣으면(이름/확장자 무관) 재생됨.

    private static void CreateBackgroundMusic()
    {
        if (GameObject.Find("BackgroundMusic") != null)
        {
            return;
        }

        var clip = FindBgmClip();

        var musicObject = new GameObject("BackgroundMusic", typeof(AudioSource));
        var source = musicObject.GetComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = TitleBgmDefaultVolume;
        source.outputAudioMixerGroup = GameAudioSettings.BgmGroup; // GameAudioMixer가 아직 없으면 null - 문제없음

        if (clip == null)
        {
            Debug.LogWarning($"BackgroundMusic object created but no clip assigned - drop one audio file into {TitleBgmFolder} and run 'Add Background Music To Title Scene' again.");
        }
    }

    // 이 폴더에는 오디오 파일 말고 README.txt도 같이 들어있어서(안내문) - .meta만 걸러내면
    // "README.txt"가 알파벳순으로 대부분의 음원 파일명보다 앞서서(대문자 R < 소문자 파일명) 실제
    // BGM 대신 골라지는 사고가 났었다(2026-08-10). 오디오 확장자만 후보로 받도록 수정.
    private static readonly string[] AudioFileExtensions = { ".mp3", ".wav", ".ogg", ".aif", ".aiff", ".flac" };

    private static AudioClip FindBgmClip()
    {
        if (!Directory.Exists(TitleBgmFolder))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var file in Directory.GetFiles(TitleBgmFolder))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (System.Array.IndexOf(AudioFileExtensions, extension) < 0)
            {
                continue;
            }

            candidates.Add(file.Replace('\\', '/'));
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        candidates.Sort();
        if (candidates.Count > 1)
        {
            Debug.LogWarning($"Multiple files found in {TitleBgmFolder}; using the first one alphabetically ({Path.GetFileName(candidates[0])}). Only one BGM track is supported - remove the others.");
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

    /// <summary>Rerun this after dropping/replacing a music file in TitleBgmFolder without
    /// rebuilding the whole scene.</summary>
    [MenuItem("Tools/Class Template/Add Background Music To Title Scene")]
    public static void AddBackgroundMusicToTitleScene()
    {
        Directory.CreateDirectory(TitleBgmFolder);
        var existing = GameObject.Find("BackgroundMusic");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        CreateBackgroundMusic();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Background music (re)added from {TitleBgmFolder}. Save the scene (Ctrl+S).");
    }

    // ---- Sprite import ----------------------------------------------------------------------

    private static void ImportTitleSprites()
    {
        foreach (var path in Directory.GetFiles(TitleArtPath, "*.png", SearchOption.TopDirectoryOnly))
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

    private static Sprite Sprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{TitleArtPath}/{name}.png");
    }

    // ---- Title background motion (title_motion01, 02, 03 ...) ------------------------------
    //
    // 프레임 개수가 고정이 아니라 자유롭게 늘고 줄어야 해서(사용자 요청), 씬 빌드 시점에 폴더를
    // 스캔해서 몇 장이든 그대로 읽어들인다. 프레임을 추가/삭제한 뒤에는 RefreshTitleMotionFrames를
    // 다시 실행해야 반영됨 - BGM/SFX 파일 교체 후 Wire/Add 도구를 다시 돌리는 것과 같은 패턴.

    /// <summary>그림(프레임 flip-book) 아니면 영상, 둘 중 하나만 선택 - 타이틀 아트 폴더에 영상
    /// 파일(title_motion*.mp4)이 있으면 영상 모드, 없으면 기존 그림 프레임 모드("파일 존재 여부가 곧
    /// 스위치", 사용자 확정 2026-08-14/15).</summary>
    private static void CreateTitleMotion(Transform parent)
    {
        var video = FindTitleMotionVideo();
        if (video != null)
        {
            CreateTitleMotionVideo(parent, video);
        }
        else
        {
            CreateTitleMotionFrames(parent);
        }
    }

    private static void CreateTitleMotionFrames(Transform parent)
    {
        var obj = new GameObject("TitleMotion", typeof(RectTransform), typeof(Image), typeof(TitleBackgroundMotionPlayer));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920, 1080);

        var image = obj.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false; // Background/Watermark와 동일하게 장식용, 클릭 안 막음

        var player = obj.GetComponent<TitleBackgroundMotionPlayer>();
        var playerSo = new SerializedObject(player);
        playerSo.FindProperty("framesPerSecond").floatValue = TitleMotionDefaultFps;
        playerSo.ApplyModifiedPropertiesWithoutUndo();

        WireTitleMotionFrames(obj);
    }

    private static void CreateTitleMotionVideo(Transform parent, VideoClip clip)
    {
        var obj = new GameObject("TitleMotion", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer), typeof(TitleBackgroundVideoPlayer));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920, 1080);

        var rawImage = obj.GetComponent<RawImage>();
        rawImage.raycastTarget = false; // Background/Watermark와 동일하게 장식용, 클릭 안 막음

        var videoPlayer = obj.GetComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;

        var player = obj.GetComponent<TitleBackgroundVideoPlayer>();
        var playerSo = new SerializedObject(player);
        playerSo.FindProperty("targetImage").objectReferenceValue = rawImage;
        playerSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>title_motion*.mp4가 TitleArtPath에 있으면 그 클립을(여러 개면 알파벳순 첫 번째,
    /// BGM 후보 선정과 동일한 관례), 없으면 null을 반환 - 타이틀 배경은 그림 프레임처럼 여러 개를
    /// 이어붙이는 방식이 아니라 영상 하나만 지원(사용자 확정).</summary>
    private static VideoClip FindTitleMotionVideo()
    {
        if (!Directory.Exists(TitleArtPath))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var path in Directory.GetFiles(TitleArtPath, $"{TitleMotionFramePrefix}*.mp4", SearchOption.TopDirectoryOnly))
        {
            candidates.Add(path.Replace('\\', '/'));
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        candidates.Sort();
        if (candidates.Count > 1)
        {
            Debug.LogWarning($"Multiple title motion video files found in {TitleArtPath}; using the first one alphabetically ({Path.GetFileName(candidates[0])}). 타이틀 배경 영상은 하나만 지원 - 나머지는 지우세요.");
        }

        LogVideoFileSizes(candidates);

        return AssetDatabase.LoadAssetAtPath<VideoClip>(candidates[0]);
    }

    /// <summary>영상 파일 총 개수/용량을 콘솔에 로그로 찍어줌(8/14 설계 논의에서 합의한 항목, 8/15
    /// 구현) - 용량/성능 트레이드오프는 학생 자율 판단으로 두되, 판단 근거를 "적용" 도구 실행 시점에
    /// 바로 보여주기 위함(사용자 확정 - 교수가 미리 제한하지 않고 알려만 줌). 영상이 하나도 없으면
    /// 조용히 넘어감(그림만 쓰는 학생에게는 불필요한 로그).</summary>
    private static void LogVideoFileSizes(List<string> videoPaths)
    {
        if (videoPaths.Count == 0)
        {
            return;
        }

        long totalBytes = 0;
        var details = new List<string>();
        foreach (var path in videoPaths)
        {
            var bytes = new FileInfo(path).Length;
            totalBytes += bytes;
            details.Add($"{Path.GetFileName(path)}: {bytes / 1024f / 1024f:F1}MB");
        }

        Debug.Log($"영상 파일 {videoPaths.Count}개, 합계 {totalBytes / 1024f / 1024f:F1}MB ({string.Join(", ", details)})");
    }

    /// <summary>Rerun this after adding/removing title_motion*.png/.mp4 files without rebuilding the
    /// whole scene. 그림<->영상 전환은 컴포넌트 구성 자체가 달라서(Image+TitleBackgroundMotionPlayer
    /// vs RawImage+VideoPlayer+TitleBackgroundVideoPlayer), 기존 TitleMotion 오브젝트를 지우고
    /// 현재 폴더 상태 기준으로 새로 만드는 방식 - 같은 이름/같은 계층 위치를 유지해서 재실행해도
    /// 안전하다.</summary>
    [MenuItem("Tools/Class Template/Refresh Title Motion Frames")]
    public static void RefreshTitleMotionFrames()
    {
        var motionObject = GameObject.Find("TitleMotion");
        if (motionObject == null)
        {
            Debug.LogWarning("'TitleMotion' 오브젝트를 찾을 수 없습니다. Title.unity를 열어서 실행하세요 (없으면 Create Title Scene을 먼저 실행).");
            return;
        }

        var parent = motionObject.transform.parent;
        var siblingIndex = motionObject.transform.GetSiblingIndex();
        Object.DestroyImmediate(motionObject);

        CreateTitleMotion(parent);

        var newMotionObject = GameObject.Find("TitleMotion");
        if (newMotionObject != null)
        {
            newMotionObject.transform.SetSiblingIndex(siblingIndex);
        }

        EnsureCanvasContainScaler(parent.gameObject);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("타이틀 배경(그림/영상)을 다시 읽어들였습니다. Ctrl+S로 저장하세요.");
    }

    /// <summary>영상 크롭 대응(CanvasContainScaler) 이전에 만들어진 기존 Title.unity에서 Refresh를
    /// 실행해도 안전하게 컴포넌트를 붙여준다(모바일 VideoImage 도구와 동일한 "재실행해도 안전하게
    /// 추가" 관례). 이미 있으면 아무것도 안 함.</summary>
    private static void EnsureCanvasContainScaler(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<CanvasContainScaler>() == null)
        {
            canvasObject.AddComponent<CanvasContainScaler>();
        }
    }

    private static void WireTitleMotionFrames(GameObject motionObject)
    {
        var frames = LoadTitleMotionFrames();
        var player = motionObject.GetComponent<TitleBackgroundMotionPlayer>();
        var image = motionObject.GetComponent<Image>();

        var so = new SerializedObject(player);
        so.FindProperty("targetImage").objectReferenceValue = image;
        var framesProperty = so.FindProperty("frames");
        framesProperty.arraySize = frames.Length;
        for (var i = 0; i < frames.Length; i++)
        {
            framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        // Play 모드가 아니어도 Scene/Game 뷰에 첫 프레임이 보이도록 - TitleBackgroundMotionPlayer의
        // OnEnable()은 Edit 모드에서 자동 실행되지 않아서, 여기서 정적으로 한 번 넣어준다.
        image.sprite = frames.Length > 0 ? frames[0] : null;

        if (frames.Length == 0)
        {
            Debug.LogWarning($"{TitleMotionFramePrefix}*.png 프레임을 하나도 못 찾음: {TitleArtPath}");
        }
    }

    private static Sprite[] LoadTitleMotionFrames()
    {
        var paths = new List<string>();
        foreach (var path in Directory.GetFiles(TitleArtPath, $"{TitleMotionFramePrefix}*.png", SearchOption.TopDirectoryOnly))
        {
            paths.Add(path.Replace('\\', '/'));
        }

        // 파일명 뒤의 숫자만 뽑아서 숫자 기준으로 정렬 - "01, 02 ... 09, 10, 11" 순서가 문자열
        // 정렬("1, 10, 2, 3...")로 깨지는 걸 방지. 자릿수를 안 맞춰도(motion7, motion12 섞여도) 안전.
        paths.Sort((a, b) => ExtractTrailingNumber(a).CompareTo(ExtractTrailingNumber(b)));

        var frames = new List<Sprite>();
        foreach (var path in paths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.ToArray();
    }

    private static int ExtractTrailingNumber(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var digits = string.Empty;
        foreach (var c in name)
        {
            if (char.IsDigit(c))
            {
                digits += c;
            }
        }

        return int.TryParse(digits, out var number) ? number : int.MaxValue; // 번호 없는 파일은 맨 뒤로
    }

    // ---- Small element builders ---------------------------------------------------------------

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size, bool raycastTarget)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = raycastTarget;
        return image;
    }

    /// <summary>Full-screen invisible Image with raycastTarget on - blocks clicks to everything
    /// behind it without needing any click-handling script (see TitleMenuController's class doc).</summary>
    private static GameObject CreateFullScreenBlocker(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = obj.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        return obj;
    }

    /// <summary>One of the 5 main menu buttons (Start/Controls/Settings/Credits/Quit) - 기본/오버/
    /// 클릭 3장 스프라이트를 쓰는 TitleImageButton.</summary>
    private static GameObject CreateMainButton(string name, Transform parent, string spritePrefix, Vector2 anchoredPosition, Vector2? anchorOverride = null)
    {
        var normal = Sprite(spritePrefix + "_normal");
        var hover = Sprite(spritePrefix + "_hover");
        var pressed = Sprite(spritePrefix + "_click");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(307, 82);

        var anchor = anchorOverride ?? new Vector2(0.5f, 0.5f);
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TitleImageButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<TitleImageButton>();
        button.SetSprites(normal, hover, pressed);
        return obj;
    }

    /// <summary>Invisible clickable region for closing a popup (X). No visual asset by design -
    /// 학생마다 팝업 아트가 달라서 위치/크기만 조절 가능한 빈 히트영역으로 제공.</summary>
    private static Button CreateCloseHitArea(Transform popupTransform)
    {
        var obj = new GameObject("CloseHitArea", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(popupTransform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = PopupCloseHitAreaPosition;
        rect.sizeDelta = PopupCloseHitAreaSize;

        var image = obj.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        var button = obj.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        return button;
    }

    /// <summary>조작설명 / 크레딧처럼 "닫기만 되는" 단순 팝업 - popupImagePath는 popup_controls /
    /// popup_credits.</summary>
    private static GameObject CreateSimplePopup(string name, Transform parent, string popupSpriteName, TitleMenuController controller, string closeMethodName, UiClickSfx uiClickSfx)
    {
        var popup = new GameObject(name, typeof(RectTransform));
        popup.transform.SetParent(parent, false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = PopupSize;

        CreateImage("Art", popup.transform, Sprite(popupSpriteName), Vector2.zero, PopupSize, false);

        var closeButton = CreateCloseHitArea(popup.transform);
        WirePersistentClick(closeButton.onClick, controller, closeMethodName);
        WirePersistentClick(closeButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));

        popup.SetActive(false);
        return popup;
    }

    /// <summary>설정 팝업: 팝업 아트 + 닫기(X, 저장 안 하고 취소) + BGM/효과음 슬라이더(같은 트랙/손잡이
    /// 아트 재사용) + 저장후닫기 버튼 + 효과음 슬라이더 조절할 때 들려줄 미리듣기 소스.</summary>
    private static GameObject CreateSettingsPopup(Transform parent, TitleMenuController controller, UiClickSfx uiClickSfx)
    {
        var popup = new GameObject("SettingsPopup", typeof(RectTransform), typeof(AudioSource), typeof(SettingsPopupController));
        popup.transform.SetParent(parent, false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = PopupSize;

        CreateImage("Art", popup.transform, Sprite("popup_settings"), Vector2.zero, PopupSize, false);

        // BGM/효과음 두 줄 - 목업의 텍스트 위치를 대략 따라간 값, 나중에 미리보기로 켜놓고 재조정.
        var bgmSlider = CreateVolumeSlider("BgmSlider", popup.transform, new Vector2(90, 83));
        var sfxSlider = CreateVolumeSlider("SfxSlider", popup.transform, new Vector2(90, -88));

        var saveButton = CreateSaveButton(popup.transform);

        var closeButton = CreateCloseHitArea(popup.transform);

        // 효과음 슬라이더 전용 미리듣기 소스 - SFX 믹서 그룹에 연결해서 슬라이더로 조절한 크기가
        // 미리듣기 소리에도 바로 반영되도록 한다.
        var previewSource = popup.GetComponent<AudioSource>();
        previewSource.playOnAwake = false;
        previewSource.spatialBlend = 0f;
        previewSource.outputAudioMixerGroup = GameAudioSettings.SfxGroup;
        var previewClip = FindAudioClipByBaseName(SfxPreviewClipFolder, SfxPreviewClipBaseName);
        if (previewClip == null)
        {
            Debug.LogWarning($"효과음 미리듣기 클립을 못 찾음: {SfxPreviewClipFolder}/{SfxPreviewClipBaseName}.*");
        }

        var settingsController = popup.GetComponent<SettingsPopupController>();
        var so = new SerializedObject(settingsController);
        so.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("menuController").objectReferenceValue = controller;
        so.FindProperty("sfxPreviewSource").objectReferenceValue = previewSource;
        so.FindProperty("sfxPreviewClip").objectReferenceValue = previewClip;
        so.ApplyModifiedPropertiesWithoutUndo();

        WirePersistentClick(saveButton.onClick, settingsController, nameof(SettingsPopupController.SaveAndClose));
        WirePersistentClick(saveButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
        WirePersistentClick(closeButton.onClick, settingsController, nameof(SettingsPopupController.CancelAndClose));
        WirePersistentClick(closeButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));

        popup.SetActive(false);
        return popup;
    }

    private static Slider CreateVolumeSlider(string name, Transform parent, Vector2 anchoredPosition)
    {
        var trackSprite = Sprite("slider_track");
        var handleSprite = Sprite("slider_handle");
        var trackSize = trackSprite != null ? new Vector2(trackSprite.rect.width, trackSprite.rect.height) : new Vector2(258, 15);

        var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        var sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = trackSize;

        // raycastTarget는 true여야 트랙 아무 곳이나 클릭했을 때도 슬라이더 값이 그 위치로 바로 이동함
        // (핸들을 정확히 잡아야만 드래그되는 게 아니라).
        var background = CreateImage("Track", sliderObj.transform, trackSprite, Vector2.zero, trackSize, true);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        var handleSize = handleSprite != null ? new Vector2(handleSprite.rect.width, handleSprite.rect.height) : new Vector2(18, 53);
        var handle = CreateImage("Handle", handleArea.transform, handleSprite, Vector2.zero, handleSize, true);

        var slider = sliderObj.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.targetGraphic = handle;
        slider.handleRect = handle.rectTransform;
        slider.value = 1f; // 사용자 확정: 초기값 최대치 (SettingsPopupController.OnEnable에서 저장값으로 다시 맞춤)
        return slider;
    }

    private static Button CreateSaveButton(Transform parent)
    {
        var sprite = Sprite("button_settings_save");
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(241, 80);

        var obj = new GameObject("SaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -273);
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint; // 상태 이미지가 하나뿐이라(의도) 기본 틴트로만 눌림 피드백
        return button;
    }

    // ---- Sound helpers ---------------------------------------------------------------------

    /// <summary>모든 버튼(메인 5개 + 팝업 X 3개 + 저장후닫기)이 공유하는 클릭 소리 하나. SFX 믹서
    /// 그룹에 연결해서 효과음 슬라이더로 크기 조절도 같이 된다.</summary>
    private static UiClickSfx CreateUiClickSfx()
    {
        var obj = new GameObject("UiClickSfx", typeof(AudioSource), typeof(UiClickSfx));
        var source = obj.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = GameAudioSettings.SfxGroup;

        var clip = FindAudioClipByBaseName(SfxRoot, UiClickClipBaseName);
        if (clip == null)
        {
            Debug.LogWarning($"클릭 효과음 클립을 못 찾음: {SfxRoot}/{UiClickClipBaseName}.*");
        }

        var clickSfx = obj.GetComponent<UiClickSfx>();
        var so = new SerializedObject(clickSfx);
        so.FindProperty("source").objectReferenceValue = source;
        so.FindProperty("clip").objectReferenceValue = clip;
        so.ApplyModifiedPropertiesWithoutUndo();

        return clickSfx;
    }

    /// <summary>확장자 무관하게 파일명만으로 찾는다 - 프로젝트 전역에서 이미 쓰는 패턴
    /// (BackgroundTestSceneBuilder.FindBgmClip / ActionTestSceneBuilder.FindAudioClipByBaseName)과 동일.</summary>
    private static AudioClip FindAudioClipByBaseName(string folder, string baseName)
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

    // ---- Event wiring helpers ------------------------------------------------------------

    /// <summary>메인 버튼(TitleImageButton) 클릭 시 실제 동작 + 클릭 효과음, 두 리스너를 같이 붙인다.</summary>
    private static void WireMainButton(GameObject buttonObject, Object target, string methodName, UiClickSfx uiClickSfx)
    {
        var clickEvent = GetImageButtonClickEvent(buttonObject);
        WirePersistentClick(clickEvent, target, methodName);
        WirePersistentClick(clickEvent, uiClickSfx, nameof(UiClickSfx.PlayClick));
    }

    private static UnityEvent GetImageButtonClickEvent(GameObject buttonObject)
    {
        var button = buttonObject.GetComponent<TitleImageButton>();
        var onClickField = typeof(TitleImageButton).GetField("onClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (UnityEvent)onClickField.GetValue(button);
    }

    private static void WirePersistentClick(UnityEvent unityEvent, Object target, string methodName)
    {
        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName);
        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existing = EditorBuildSettings.scenes;
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };

        foreach (var s in existing)
        {
            if (s.path != scenePath)
            {
                scenes.Add(s);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
