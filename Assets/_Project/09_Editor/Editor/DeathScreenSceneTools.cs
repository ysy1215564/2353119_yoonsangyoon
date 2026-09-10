using System.IO;
using GameProject.Audio;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds/updates the death screen (deathcut.png illustration + 재도전/타이틀화면으로/종료, from
/// 04_Art/StudentReplace/UI/DeathScreen) - same non-destructive "works on whatever scene is open"
/// + "tune in ActionTest (죽음은 M 키로 테스트 가능), then Sync to BackgroundTest" pattern as the
/// other HUD tools (Player HP, Special Gauge).
/// </summary>
public static class DeathScreenSceneTools
{
    private const string ArtPath = "Assets/_Project/04_Art/StudentReplace/UI/DeathScreen";
    private const string SfxRoot = "Assets/_Project/06_Audio/SFX";
    private const string UiClickClipBaseName = "mouseclick";
    private const string DeathStingerFolder = "Assets/_Project/06_Audio/BGM/DeathScreen";
    private const string DeathStingerClipBaseName = "deathscreen_bgm";

    private static readonly Vector2 IllustrationSize = new Vector2(700f, 499f); // deathcut.png 원본 크기, 조정 없이 그대로
    private static readonly Vector2 IllustrationOffset = new Vector2(0f, 120f); // 패널 중심 기준 - 대략값
    private static readonly Vector2 RetryButtonOffset = new Vector2(0f, -180f);
    private static readonly Vector2 TitleButtonOffset = new Vector2(0f, -270f);
    private static readonly Vector2 QuitButtonOffset = new Vector2(0f, -360f);

    [MenuItem("Tools/Class Template/Add Death Screen To Scene")]
    public static void AddDeathScreenToScene()
    {
        ImportDeathScreenSprites();

        var scene = SceneManager.GetActiveScene();
        var screen = EnsureDeathScreenInScene(scene);
        if (screen == null)
        {
            Debug.LogWarning("현재 씬에서 PlayerActionTestController(주인공)를 못 찾았습니다 - ActionTest 또는 BackgroundTest 씬을 열고 실행하세요.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("사망 화면을 추가/갱신했습니다. 위치는 대략값이니 Scene 뷰에서 직접 맞추고(ActionTest에서는 M 키로 테스트 가능) 저장(Ctrl+S)하세요.");
    }

    // 2026-08-18: 예전엔 여기 전용 "Sync Death Screen To BackgroundTest"(ActionTest->BackgroundTest
    // 하드코딩) 명령이 있었는데, Stage2/3_BackgroundTest에는 애초에 손이 안 닿는 구조였음(우연히
    // 기본 위치가 맞아서 문제가 안 보였을 뿐). SyncFromActionTestTool의 LayoutRootNames에
    // "DeathScreenController"를 추가해서 대체 - 그 도구가 이미 "ActionTest 기준 -> 존재하는 모든
    // 전투 씬"을 도는 구조라 여기 있던 것보다 넓게, 그리고 스테이지가 늘어나도 자동으로 커버된다.

    private static GameObject EnsureDeathScreenInScene(Scene scene)
    {
        // 2026-08-11 구조 변경 이전 버전이 만든 고장난 "DeathScreen"(컨트롤러+패널이 한 오브젝트라
        // 패널이 꺼지면 Update()도 같이 멈추던 것)이 남아있으면 정리 - 안 지우면 새 구조와 같이
        // 남아서 화면에 옛날 것이 겹쳐 보이게 된다.
        var legacyObject = FindByNameIncludingInactive(scene, "DeathScreen");
        if (legacyObject != null)
        {
            Object.DestroyImmediate(legacyObject);
        }

        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            return null;
        }

        var screenObject = FindByNameIncludingInactive(scene, "DeathScreenController");
        if (screenObject != null)
        {
            // 이미 있으면 구조(위치 등)는 안 건드리고 플레이어 레퍼런스만 다시 연결 (씬마다 플레이어
            // 인스턴스가 다르므로).
            WireReferences(screenObject, playerController);
            return screenObject;
        }

        EnsureEventSystem(scene);
        var canvasTransform = EnsureHudCanvas(scene);
        var uiClickSfx = EnsureUiClickSfx(scene);
        screenObject = CreateDeathScreenHierarchy(canvasTransform, uiClickSfx);
        WireReferences(screenObject, playerController);
        return screenObject;
    }

    /// <summary>ActionTest/BackgroundTest 둘 다 지금까지 클릭 가능한 UI가 없어서 EventSystem 자체가
    /// 아예 없었다 - 사망 화면 버튼이 각 씬의 첫 클릭 대상이라, GraphicRaycaster가 있어도 이게 없으면
    /// 클릭이 전혀 안 들어간다.</summary>
    private static void EnsureEventSystem(Scene scene)
    {
        if (FindByNameIncludingInactive(scene, "EventSystem") != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    private static Transform EnsureHudCanvas(Scene scene)
    {
        // 다른 HUD 도구들(PlayerHpDisplaySceneTools 등)과 같은 규칙: ActionTest.unity의 "Instruction
        // Canvas"를 재사용, 없으면(또는 다른 HUD가 먼저 만들어둔) "HUD Canvas"를 재사용, 그것도 없으면
        // 새로 만든다.
        var existing = FindByNameIncludingInactive(scene, "Instruction Canvas");
        if (existing != null)
        {
            EnsureGraphicRaycaster(existing);
            return existing.transform;
        }

        existing = FindByNameIncludingInactive(scene, "HUD Canvas");
        if (existing != null)
        {
            // PlayerHpDisplaySceneTools/SpecialGaugeSceneTools가 먼저 이 "HUD Canvas"를 만들었을 수
            // 있는데, 걔네는 클릭이 필요 없어서 GraphicRaycaster 없이 만든다 - 사망 화면 버튼이 처음
            // 클릭 대상이라 여기서 없으면 추가해준다.
            EnsureGraphicRaycaster(existing);
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

    private static void EnsureGraphicRaycaster(GameObject canvasObject)
    {
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>ActionTest/BackgroundTest에는 지금까지 클릭 가능한 버튼이 없었어서(HP/게이지는
    /// 순수 표시용) UiClickSfx가 아직 없다 - 사망 화면 버튼이 이 두 씬의 첫 클릭 대상이라 여기서
    /// 처음 만든다. 타이틀/컷신과 같은 클릭음(mouseclick.*) 재사용.</summary>
    private static UiClickSfx EnsureUiClickSfx(Scene scene)
    {
        var existingObject = FindByNameIncludingInactive(scene, "UiClickSfx");
        if (existingObject != null)
        {
            return existingObject.GetComponent<UiClickSfx>();
        }

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

    /// <summary>
    /// 컨트롤러 오브젝트("DeathScreenController")와 실제 보이는 패널("DeathScreenPanel")을 분리해야
    /// 한다 - 패널은 평소 꺼져있는 게 정상인데, DeathScreenController 컴포넌트가 그 패널과 같은
    /// 오브젝트에 있으면 오브젝트가 꺼진 순간 Update() 자체가 안 돌아서 "죽었는지 감시하는 로직"이
    /// 죽어있는 동안(=꺼져있어야 할 때) 같이 멈춰버리는 사고가 남(2026-08-11, 실사용 중 발견).
    /// 그래서 컨트롤러는 항상 켜진 채로 두고, 패널만 그 자식으로 붙여서 따로 켜고 끈다.
    /// </summary>
    private static GameObject CreateDeathScreenHierarchy(Transform parent, UiClickSfx uiClickSfx)
    {
        var controllerObject = new GameObject("DeathScreenController", typeof(RectTransform), typeof(AudioSource), typeof(DeathScreenController));
        controllerObject.transform.SetParent(parent, false);
        SetupStingerSource(controllerObject);
        var controllerRect = controllerObject.GetComponent<RectTransform>();
        controllerRect.anchorMin = Vector2.zero;
        controllerRect.anchorMax = Vector2.one;
        controllerRect.offsetMin = Vector2.zero;
        controllerRect.offsetMax = Vector2.zero;

        var panel = new GameObject("DeathScreenPanel", typeof(RectTransform));
        panel.transform.SetParent(controllerObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 900f); // 자식들 배치용 논리적 크기 - 실제로는 자식들만 보임

        CreateImage("Illustration", panel.transform, LoadSprite("deathcut"), IllustrationOffset, IllustrationSize);

        var retryButton = CreateDeathScreenButton("RetryButton", panel.transform, "button_retry", RetryButtonOffset);
        var titleButton = CreateDeathScreenButton("TitleButton", panel.transform, "button_title", TitleButtonOffset);
        var quitButton = CreateDeathScreenButton("QuitButton", panel.transform, "button_quit", QuitButtonOffset);

        var controller = controllerObject.GetComponent<DeathScreenController>();
        WireImageButtonClick(retryButton, controller, nameof(DeathScreenController.OnRetryClicked), uiClickSfx);
        WireImageButtonClick(titleButton, controller, nameof(DeathScreenController.OnTitleClicked), uiClickSfx);
        WireImageButtonClick(quitButton, controller, nameof(DeathScreenController.OnQuitClicked), uiClickSfx);

        panel.SetActive(false); // 사망 전까지는 숨김 - DeathScreenController(항상 켜진 상태)가 IsDead 보고 켬
        return controllerObject;
    }

    private static void WireReferences(GameObject controllerObject, PlayerActionTestController controller)
    {
        var display = controllerObject.GetComponent<DeathScreenController>();
        if (display == null)
        {
            return;
        }

        // 2026-08-12: 사망 화면(이미지+메뉴)이 뜨는 순간 1번 재생되는 스팅어 사운드용 - 기존에 이미
        // 씬에 있던 DeathScreenController(구조 변경 전)에는 AudioSource가 없을 수 있으니 없으면 추가.
        SetupStingerSource(controllerObject);

        var so = new SerializedObject(display);
        so.FindProperty("playerController").objectReferenceValue = controller;

        var panelProperty = so.FindProperty("panel");
        if (panelProperty.objectReferenceValue == null)
        {
            var panelTransform = controllerObject.transform.Find("DeathScreenPanel");
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

        var stingerClipProperty = so.FindProperty("deathScreenStinger");
        if (stingerClipProperty.objectReferenceValue == null)
        {
            var stingerClip = FindAudioClipByBaseName(DeathStingerFolder, DeathStingerClipBaseName);
            if (stingerClip == null)
            {
                Debug.LogWarning($"사망 화면 사운드 클립을 못 찾음: {DeathStingerFolder}/{DeathStingerClipBaseName}.* (파일을 넣고 Unity가 임포트하게 한 뒤 이 메뉴를 다시 실행하세요.)");
            }

            stingerClipProperty.objectReferenceValue = stingerClip;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>DeathScreenController와 같은 오브젝트에 붙는 AudioSource를 재생용으로 설정한다 -
    /// 1번만 재생되는 스팅어라 playOnAwake/loop 둘 다 꺼두고 스크립트가 PlayOneShot으로 직접 켠다.</summary>
    private static void SetupStingerSource(GameObject controllerObject)
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

    // ---- Element builders ---------------------------------------------------------------------

    private static void CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size)
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
        image.raycastTarget = false;
    }

    /// <summary>기본/오버/클릭 3장 스프라이트를 쓰는 TitleImageButton 재사용 - 타이틀 메인 버튼과
    /// 동일한 패턴.</summary>
    private static GameObject CreateDeathScreenButton(string name, Transform parent, string spritePrefix, Vector2 anchoredPosition)
    {
        var normal = LoadSprite(spritePrefix + "_normal");
        var hover = LoadSprite(spritePrefix + "_hover");
        var pressed = LoadSprite(spritePrefix + "_click");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(307, 82);

        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TitleImageButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
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

    // ---- Sprite import ----------------------------------------------------------------------

    private static void ImportDeathScreenSprites()
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

    // ---- Event wiring -------------------------------------------------------------------------

    private static void WireImageButtonClick(GameObject buttonObject, Object target, string methodName, UiClickSfx uiClickSfx)
    {
        var button = buttonObject.GetComponent<TitleImageButton>();
        var onClickField = typeof(TitleImageButton).GetField("onClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var onClickEvent = (UnityEvent)onClickField.GetValue(button);
        WirePersistentClick(onClickEvent, target, methodName);
        WirePersistentClick(onClickEvent, uiClickSfx, nameof(UiClickSfx.PlayClick));
    }

    private static void WirePersistentClick(UnityEvent unityEvent, Object target, string methodName)
    {
        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName);
        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    // ---- Scene search helpers -------------------------------------------------------------

    private static GameObject FindByNameIncludingInactive(Scene scene, string name)
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
