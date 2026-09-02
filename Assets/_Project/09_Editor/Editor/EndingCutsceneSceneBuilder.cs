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
/// Builds "EndingCutscene.unity": IntroCutsceneSceneBuilder와 완전히 같은 구조(전체화면 슬라이드
/// 시퀀스 - 그림/영상 자유 혼합 + Next/Skip/Prev 코너 버튼 + 루프 BGM) - IntroCutsceneController가
/// 씬 이름을 필드로만 갖고 있어서 그대로 재사용 가능, 새 컨트롤러 스크립트가 필요 없다.
///
/// 인트로컷신과 다른 점은 딱 하나: 마지막 장 Next 또는 Skip을 누르면 BackgroundTest가 아니라
/// Title로 이동한다(사용자 확정, 2026-08-12) - 레벨클리어 화면(StageClearController)의 클리어
/// 사운드 재생이 끝나면 이 씬으로 자동 진입.
/// </summary>
public static class EndingCutsceneSceneBuilder
{
    private const string ScenePath = "Assets/_Project/00_Scenes/Flow/EndingCutscene.unity";
    private const string TitleScenePath = "Assets/_Project/00_Scenes/Flow/Title.unity";
    private const string ArtPath = "Assets/_Project/04_Art/StudentReplace/Story/EndingCutscene";
    private const string BgmFolder = "Assets/_Project/06_Audio/BGM/EndingCutscene";
    private const string SfxRoot = "Assets/_Project/06_Audio/SFX";
    private const string UiClickClipBaseName = "mouseclick"; // 06_Audio/SFX/mouseclick.* (확장자 무관)
    private const string SlideFramePrefix = "ending"; // ending01.png, ending02.png ...
    private const float BgmDefaultVolume = 0.45f;

    // 화면 가장자리에 버튼이 딱 붙어 답답해 보이지 않도록 여백(인트로컷신과 동일값) - 대략값.
    private static readonly Vector2 ButtonMargin = new Vector2(60f, 50f);

    [MenuItem("Tools/Class Template/Create Ending Cutscene Scene")]
    public static void CreateEndingCutsceneScene()
    {
        Directory.CreateDirectory(BgmFolder);
        AssetDatabase.Refresh(); // 새로 복사된 PNG들이 임포터를 갖기 전이면 ImportArtSprites가 못 건드림
        ImportArtSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        new GameObject("AudioSettingsBootstrap", typeof(AudioSettingsBootstrap));

        var uiClickSfx = CreateUiClickSfx();

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasContainScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // 1(높이 기준) - 에디터 미리보기/빌드 시점 기본값일 뿐, 실제 값은 실행 시점에 CanvasContain-
        // Scaler.Awake()가 화면 비율을 보고 0(가로 기준) 또는 1(세로 기준)으로 다시 정한다. 고정값
        // 하나로는 16:9보다 넓은 화면(Galaxy S21 등)과 좁은 화면(Galaxy Tab S10 등)을 동시에 안 잘리게
        // 만들 수 없다 - 자세한 배경은 CanvasContainScaler 클래스 주석 참고.
        scaler.matchWidthOrHeight = 1f;

        var controllerObject = new GameObject("IntroCutsceneController", typeof(IntroCutsceneController));
        var controller = controllerObject.GetComponent<IntroCutsceneController>();

        // ---- 1. 슬라이드 이미지 + 영상 (가장 아래, 항상 하나만 활성화되고 ShowSlide가 전환) ------
        var slideImage = CreateSlideImage(canvasObject.transform);
        var videoImage = CreateVideoImage(canvasObject.transform, out var videoPlayer);

        // ---- 2. 코너 버튼 3종 (여백 있게) --------------------------------------------------
        var nextButton = CreateCornerButton("NextButton", canvasObject.transform, "button_next", new Vector2(1f, 0f), ButtonMargin);
        var skipButton = CreateCornerButton("SkipButton", canvasObject.transform, "button_skip", new Vector2(1f, 1f), ButtonMargin);
        var prevButton = CreateCornerButton("PrevButton", canvasObject.transform, "button_prev", new Vector2(0f, 0f), ButtonMargin);

        WirePersistentClick(nextButton.onClick, controller, nameof(IntroCutsceneController.OnNext));
        WirePersistentClick(nextButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
        WirePersistentClick(skipButton.onClick, controller, nameof(IntroCutsceneController.OnSkip));
        WirePersistentClick(skipButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
        WirePersistentClick(prevButton.onClick, controller, nameof(IntroCutsceneController.OnPrev));
        WirePersistentClick(prevButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));

        var so = new SerializedObject(controller);
        so.FindProperty("slideImage").objectReferenceValue = slideImage;
        so.FindProperty("videoImage").objectReferenceValue = videoImage;
        so.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        so.FindProperty("prevButtonObject").objectReferenceValue = prevButton.gameObject;
        so.FindProperty("nextSceneName").stringValue = "Title"; // 사용자 확정 - 엔딩은 Title로 복귀
        so.ApplyModifiedPropertiesWithoutUndo();

        WireSlides(controller, slideImage, prevButton.gameObject);

        CreateBackgroundMusic();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureScenesInBuildSettings(ScenePath, TitleScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created ending cutscene scene: {ScenePath}. 버튼 위치는 대략값이니 Scene 뷰에서 직접 맞출 것. 인스펙터에서 장 미리보기로 확인 가능.");
    }

    // ---- Slides (ending01, 02, 03 ...) --------------------------------------------------------
    //
    // 인트로컷신과 동일한 패턴: 폴더를 스캔해서 몇 장이든 그대로 읽어들이고, 장을 추가/삭제한 뒤에는
    // RefreshEndingCutsceneSlides만 다시 실행하면 됨 - 버튼 위치를 다시 맞출 필요 없이.

    /// <summary>Rerun this after adding/removing ending*.png files without rebuilding the whole
    /// scene (버튼 위치가 초기화되지 않음).</summary>
    [MenuItem("Tools/Class Template/Refresh Ending Cutscene Slides")]
    public static void RefreshEndingCutsceneSlides()
    {
        // GameObject.Find는 비활성 오브젝트를 못 찾는데, PrevButton은 평소(1번 장) 꺼져있는 게
        // 기본 상태라 이걸로 찾으면 못 찾는 경우가 실제로 생김 - 계층을 직접 훑는 방식으로 찾는다.
        var controllerObject = FindByNameIncludingInactive("IntroCutsceneController");
        var slideImageObject = FindByNameIncludingInactive("SlideImage");
        var prevButtonObject = FindByNameIncludingInactive("PrevButton");
        if (controllerObject == null || slideImageObject == null)
        {
            Debug.LogWarning("'IntroCutsceneController' 또는 'SlideImage'를 찾을 수 없습니다. EndingCutscene.unity를 열어서 실행하세요 (없으면 Create Ending Cutscene Scene을 먼저 실행).");
            return;
        }

        var controller = controllerObject.GetComponent<IntroCutsceneController>();
        EnsureVideoImage(controller, slideImageObject.transform.parent);
        WireSlides(controller, slideImageObject.GetComponent<Image>(), prevButtonObject);
        EnsureCanvasContainScaler(slideImageObject.transform.parent.gameObject);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("엔딩 컷신 장(그림+영상 혼합)을 다시 읽어들였습니다. Ctrl+S로 저장하세요.");
    }

    /// <summary>영상 지원 이전에 만들어진 기존 EndingCutscene.unity에서 Refresh를 실행해도 안전하게
    /// VideoImage를 새로 붙여준다(모바일 대쉬 버튼 도구와 동일한 관례 - 다시 실행해도 기존 요소는
    /// 안 건드리고 빠진 것만 추가). 이미 있으면 아무것도 안 함.</summary>
    private static void EnsureVideoImage(IntroCutsceneController controller, Transform canvasTransform)
    {
        if (FindByNameIncludingInactive("VideoImage") != null)
        {
            return;
        }

        var videoImage = CreateVideoImage(canvasTransform, out var videoPlayer);
        var so = new SerializedObject(controller);
        so.FindProperty("videoImage").objectReferenceValue = videoImage;
        so.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>영상 크롭 대응(CanvasContainScaler) 이전에 만들어진 기존 EndingCutscene.unity에서
    /// Refresh를 실행해도 안전하게 컴포넌트를 붙여준다(EnsureVideoImage와 동일한 "재실행해도 안전하게
    /// 추가" 관례). 이미 있으면 아무것도 안 함.</summary>
    private static void EnsureCanvasContainScaler(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<CanvasContainScaler>() == null)
        {
            canvasObject.AddComponent<CanvasContainScaler>();
        }
    }

    private static void WireSlides(IntroCutsceneController controller, Image slideImage, GameObject prevButtonObject)
    {
        var slides = LoadSlides();

        var so = new SerializedObject(controller);
        var slidesProperty = so.FindProperty("slides");
        slidesProperty.arraySize = slides.Length;
        for (var i = 0; i < slides.Length; i++)
        {
            var element = slidesProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("image").objectReferenceValue = slides[i].image;
            element.FindPropertyRelative("video").objectReferenceValue = slides[i].video;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        // Edit 모드에서도 1번 장이 바로 보이도록 정적으로 반영 (Awake는 Edit 모드에서 자동 실행 안 됨).
        // 1번 장이 영상이면 Edit 모드에서는 미리 보여줄 방법이 없음(VideoPlayer가 Play 모드에서만
        // 프레임을 디코딩) - Play 모드에서 확인.
        if (slideImage != null)
        {
            slideImage.sprite = slides.Length > 0 ? slides[0].image : null;
        }

        // 1번 장 기준이라 Prev는 항상 숨김에서 시작 (2장 이상일 때 실제 이동은 런타임에 자동 처리됨).
        if (prevButtonObject != null)
        {
            prevButtonObject.SetActive(false);
        }

        if (slides.Length == 0)
        {
            Debug.LogWarning($"{SlideFramePrefix}*.png / {SlideFramePrefix}*.mp4 슬라이드를 하나도 못 찾음: {ArtPath}");
        }
    }

    /// <summary>
    /// ending01.png, ending02.mp4, ending03.png ... 처럼 그림/영상을 파일명 뒤 번호 하나로 같이
    /// 정렬 - 확장자와 무관하게 번호 순서가 곧 슬라이드 순서(사용자가 파일명으로 직접 정함, 사용자
    /// 확정 2026-08-15). 그림/영상 자유 혼합을 지원하는 핵심 함수.
    /// </summary>
    private static CutsceneSlide[] LoadSlides()
    {
        var paths = new List<string>();
        foreach (var path in Directory.GetFiles(ArtPath, $"{SlideFramePrefix}*.png", SearchOption.TopDirectoryOnly))
        {
            paths.Add(path.Replace('\\', '/'));
        }

        var videoPaths = new List<string>();
        foreach (var path in Directory.GetFiles(ArtPath, $"{SlideFramePrefix}*.mp4", SearchOption.TopDirectoryOnly))
        {
            videoPaths.Add(path.Replace('\\', '/'));
        }
        paths.AddRange(videoPaths);

        LogVideoFileSizes(videoPaths);

        // 숫자 기준 정렬 - "01, 02 ... 09, 10, 11" 순서가 문자열 정렬로 깨지는 걸 방지.
        paths.Sort((a, b) => ExtractTrailingNumber(a).CompareTo(ExtractTrailingNumber(b)));

        var slides = new List<CutsceneSlide>();
        foreach (var path in paths)
        {
            var slide = new CutsceneSlide();
            if (Path.GetExtension(path).ToLowerInvariant() == ".mp4")
            {
                slide.video = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
                if (slide.video == null)
                {
                    continue;
                }
            }
            else
            {
                slide.image = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (slide.image == null)
                {
                    continue;
                }
            }

            slides.Add(slide);
        }

        return slides.ToArray();
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

    private static GameObject FindByNameIncludingInactive(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
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

        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    // ---- Background music -------------------------------------------------------------------

    private static readonly string[] AudioFileExtensions = { ".mp3", ".wav", ".ogg", ".aif", ".aiff", ".flac" };

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
        source.volume = BgmDefaultVolume;
        source.outputAudioMixerGroup = GameAudioSettings.BgmGroup;

        if (clip == null)
        {
            Debug.LogWarning($"BackgroundMusic object created but no clip assigned - drop one audio file into {BgmFolder} and run 'Add Background Music To Ending Cutscene Scene' again.");
        }
    }

    private static AudioClip FindBgmClip()
    {
        if (!Directory.Exists(BgmFolder))
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (var file in Directory.GetFiles(BgmFolder))
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
            Debug.LogWarning($"Multiple files found in {BgmFolder}; using the first one alphabetically ({Path.GetFileName(candidates[0])}). Only one BGM track is supported - remove the others.");
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

    [MenuItem("Tools/Class Template/Add Background Music To Ending Cutscene Scene")]
    public static void AddBackgroundMusicToEndingCutsceneScene()
    {
        Directory.CreateDirectory(BgmFolder);
        var existing = GameObject.Find("BackgroundMusic");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        CreateBackgroundMusic();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Background music (re)added from {BgmFolder}. Save the scene (Ctrl+S).");
    }

    // ---- Sprite import ----------------------------------------------------------------------

    private static void ImportArtSprites()
    {
        foreach (var path in Directory.GetFiles(ArtPath, "*.png", SearchOption.TopDirectoryOnly))
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

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtPath}/{name}.png");
    }

    // ---- Element builders ---------------------------------------------------------------------

    private static Image CreateSlideImage(Transform parent)
    {
        var obj = new GameObject("SlideImage", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920, 1080);

        var image = obj.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false; // 장식용 - 클릭은 버튼만
        return image;
    }

    /// <summary>SlideImage와 같은 자리/같은 크기, 영상 장일 때만 활성화되는 RawImage - IntroCutscene-
    /// Controller.ShowSlide가 그림/영상 둘 중 하나만 켜고 끔. RenderTexture는 여기서 미리 만들지 않고
    /// 컨트롤러의 Awake(SetupVideoPlayer)에서 만든다 - 씬 저장 파일에 런타임 전용 텍스처를 남기지
    /// 않기 위함.</summary>
    private static RawImage CreateVideoImage(Transform parent, out VideoPlayer videoPlayer)
    {
        var obj = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920, 1080);

        var rawImage = obj.GetComponent<RawImage>();
        rawImage.raycastTarget = false; // 장식용 - 클릭은 버튼만 (SlideImage와 동일 관례)

        obj.SetActive(false); // 기본은 그림 장부터 시작 - ShowSlide가 런타임에 알아서 전환

        videoPlayer = obj.GetComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.playOnAwake = false;

        return rawImage;
    }

    /// <summary>anchor (0,0)=좌하단, (1,0)=우하단, (1,1)=우상단 등 - 그 코너에 margin만큼 안쪽으로
    /// 띄워서 배치. 상태 이미지 1장뿐이라(Next/Skip/Prev 전부) 평범한 Button + ColorTint 전환.</summary>
    private static Button CreateCornerButton(string name, Transform parent, string spriteName, Vector2 anchor, Vector2 margin)
    {
        var sprite = LoadSprite(spriteName);
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(214, 87);

        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;

        var signedX = anchor.x <= 0.01f ? margin.x : -margin.x;
        var signedY = anchor.y <= 0.01f ? margin.y : -margin.y;
        rect.anchoredPosition = new Vector2(signedX, signedY);

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        return button;
    }

    // ---- Sound helpers ---------------------------------------------------------------------

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

    // ---- Event wiring / build settings ------------------------------------------------------

    private static void WirePersistentClick(UnityEvent unityEvent, Object target, string methodName)
    {
        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName);
        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    /// <summary>기존에 등록된 씬은 그대로 두고, 빠진 것만 추가.</summary>
    private static void EnsureScenesInBuildSettings(params string[] scenePaths)
    {
        var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var existingPaths = new HashSet<string>();
        foreach (var s in existing)
        {
            existingPaths.Add(s.path);
        }

        var changed = false;
        foreach (var path in scenePaths)
        {
            if (!existingPaths.Contains(path) && File.Exists(path))
            {
                existing.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
