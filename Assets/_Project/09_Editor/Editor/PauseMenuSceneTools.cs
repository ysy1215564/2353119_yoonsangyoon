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
/// ActionTest.unity/BackgroundTest.unity 어느 쪽에 실행하든 톱니바퀴 버튼 + 6버튼 일시정지 메뉴를
/// 추가/갱신한다. 조작설명/설정 팝업은 새 아트를 그리지 않고 Title의 popup_controls/popup_settings
/// 아트와 슬라이더 배치를 그대로 재사용(사용자 확정) - Title.unity 자체는 건드리지 않고, 이 씬 안에
/// 같은 아트로 새로 만든다. DeathScreenSceneTools와 같은 "이미 있으면 위치는 안 건드리고 레퍼런스만
/// 다시 연결" 패턴 - 몇 번을 다시 실행해도 안전함.
/// </summary>
public static class PauseMenuSceneTools
{
    private const string PauseMenuArtPath = "Assets/_Project/04_Art/StudentReplace/UI/PauseMenu";
    private const string TitleArtPath = "Assets/_Project/04_Art/StudentReplace/UI/Title";
    private const string SfxRoot = "Assets/_Project/06_Audio/SFX";
    private const string UiClickClipBaseName = "mouseclick";
    private const string SfxPreviewClipFolder = SfxRoot + "/Player";
    private const string SfxPreviewClipBaseName = "player_attack1_swing";

    // Title의 팝업과 완전히 동일한 틀(602x715) + X 위치를 그대로 재사용 - 이미 눈으로 맞춘 좌표라
    // 다시 조절할 필요가 없음.
    private static readonly Vector2 PopupSize = new Vector2(602, 715);
    private static readonly Vector2 PopupCloseHitAreaPosition = new Vector2(219, 298);
    private static readonly Vector2 PopupCloseHitAreaSize = new Vector2(70, 70);

    [MenuItem("Tools/Class Template/Add Pause Menu To Scene")]
    public static void AddPauseMenuToScene()
    {
        ImportSprites(PauseMenuArtPath);

        var scene = SceneManager.GetActiveScene();
        EnsureEventSystem(scene);
        var canvasTransform = EnsureHudCanvas(scene);
        var uiClickSfx = EnsureUiClickSfx(scene);

        var controllerObject = FindByNameIncludingInactive(scene, "PauseMenuController");
        if (controllerObject == null)
        {
            controllerObject = CreatePauseMenuHierarchy(canvasTransform, uiClickSfx);
        }

        var controller = controllerObject.GetComponent<PauseMenuController>();
        WirePlayerReference(controller);
        EnsureGearButton(scene, canvasTransform, controller, uiClickSfx);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("일시정지 메뉴를 추가(또는 갱신)했습니다. 버튼/딤 위치는 대략값이니 'Toggle Pause Menu Preview'로 켜놓고 Scene 뷰에서 맞춘 뒤 저장(Ctrl+S)하세요.");
    }

    [MenuItem("Tools/Class Template/Add Pause Menu To Scene", true)]
    private static bool ValidateAddPauseMenuToScene()
    {
        return IsSupportedScene();
    }

    /// <summary>PausePanel은 평소 꺼져있는 게 정상이라 Scene/Game 뷰 어디서도 안 보임 - 타이틀
    /// 팝업/클리어 화면 미리보기 도구와 같은 이유로 만든 Edit 모드 전용 토글.</summary>
    [MenuItem("Tools/Class Template/Toggle Pause Menu Preview")]
    public static void TogglePauseMenuPreview()
    {
        var scene = SceneManager.GetActiveScene();
        var panel = FindByNameIncludingInactive(scene, "PausePanel");
        if (panel == null)
        {
            Debug.LogWarning("'PausePanel'을 찾을 수 없습니다. 'Add Pause Menu To Scene'을 먼저 실행하세요.");
            return;
        }

        panel.SetActive(!panel.activeSelf);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(panel.activeSelf
            ? "일시정지 메뉴를 켰습니다 - Scene/Game 뷰에서 위치를 맞춘 뒤, 이 메뉴를 한 번 더 눌러 끄고 나서 저장하세요(켠 채로 저장하면 Play 시작하자마자 메뉴가 떠 있게 됨)."
            : "일시정지 메뉴를 껐습니다. 저장(Ctrl+S)하세요.");
    }

    [MenuItem("Tools/Class Template/Toggle Pause Menu Preview", true)]
    private static bool ValidateTogglePauseMenuPreview()
    {
        return IsSupportedScene();
    }

    private static bool IsSupportedScene()
    {
        // 2026-08-17: 스테이지1 이름만 인식하던 조건을 StageIdentity.IsRecognizedGameplayScene로
        // 넓힘 - 이 파일의 나머지 로직은 이미 "지금 열린 씬" 기준으로 완전히 범용이라(스테이지1
        // 전용 경로/데이터 없음), 활성화 조건만 넓히면 스테이지2 이상에도 그대로 동작함.
        return StageIdentity.IsRecognizedGameplayScene(SceneManager.GetActiveScene().name);
    }

    // ---- Player reference ---------------------------------------------------------------------

    private static void WirePlayerReference(PauseMenuController controller)
    {
        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerActionTestController(주인공)를 찾을 수 없어 플레이어 레퍼런스를 연결하지 못했습니다.");
            return;
        }

        var so = new SerializedObject(controller);
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- Gear button ---------------------------------------------------------------------------

    /// <summary>화면 우측 상단, 호버/클릭 상태 없는 1장짜리 그림이라 TitleImageButton 대신 일반
    /// Button+ColorTint 사용(인트로컷신 코너 버튼들과 동일한 처리).</summary>
    private static void EnsureGearButton(Scene scene, Transform canvasTransform, PauseMenuController controller, UiClickSfx uiClickSfx)
    {
        if (FindByNameIncludingInactive(scene, "PauseGearButton") != null)
        {
            return; // 위치는 안 건드림
        }

        var sprite = LoadPauseSprite("pause");
        var size = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) : new Vector2(122, 122);

        var obj = new GameObject("PauseGearButton", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(canvasTransform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f); // 대략값
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        WirePersistentClick(button.onClick, controller, nameof(PauseMenuController.OnGearClicked));
        WirePersistentClick(button.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
    }

    // ---- Pause menu hierarchy -------------------------------------------------------------------

    private static GameObject CreatePauseMenuHierarchy(Transform parent, UiClickSfx uiClickSfx)
    {
        var controllerObject = new GameObject("PauseMenuController", typeof(RectTransform), typeof(PauseMenuController));
        controllerObject.transform.SetParent(parent, false);
        var controllerRect = controllerObject.GetComponent<RectTransform>();
        controllerRect.anchorMin = Vector2.zero;
        controllerRect.anchorMax = Vector2.one;
        controllerRect.offsetMin = Vector2.zero;
        controllerRect.offsetMax = Vector2.zero;

        var panel = new GameObject("PausePanel", typeof(RectTransform));
        panel.transform.SetParent(controllerObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ---- 1. 딤 배경 (맨 아래) -----------------------------------------------------------
        CreateFullScreenDim("Backdrop", panel.transform);

        // ---- 2. 메뉴 버튼 6개 (화면 정중앙, 세로 스택) ---------------------------------------
        var controller = controllerObject.GetComponent<PauseMenuController>();
        var resumeButton = CreateMenuButton("ResumeButton", panel.transform, "button_continue", new Vector2(0, 220));
        var retryButton = CreateMenuButton("RetryButton", panel.transform, "button_retry", new Vector2(0, 132));
        var titleButton = CreateMenuButton("TitleButton", panel.transform, "button_title", new Vector2(0, 44));
        var controlsButton = CreateMenuButton("ControlsButton", panel.transform, "button_controls", new Vector2(0, -44));
        var settingsButton = CreateMenuButton("SettingsButton", panel.transform, "button_settings", new Vector2(0, -132));
        var quitButton = CreateMenuButton("QuitButton", panel.transform, "button_quit", new Vector2(0, -220));

        WireMenuButton(resumeButton, controller, nameof(PauseMenuController.OnResumeClicked), uiClickSfx);
        WireMenuButton(retryButton, controller, nameof(PauseMenuController.OnRetryClicked), uiClickSfx);
        WireMenuButton(titleButton, controller, nameof(PauseMenuController.OnTitleClicked), uiClickSfx);
        WireMenuButton(controlsButton, controller, nameof(PauseMenuController.OnControlsClicked), uiClickSfx);
        WireMenuButton(settingsButton, controller, nameof(PauseMenuController.OnSettingsClicked), uiClickSfx);
        WireMenuButton(quitButton, controller, nameof(PauseMenuController.OnQuitClicked), uiClickSfx);

        // ---- 3. 서브 팝업 모달 차단막 (버튼들 위, 팝업들 아래) --------------------------------
        var popupBlocker = CreateFullScreenBlocker("PopupModalBlocker", panel.transform);
        popupBlocker.SetActive(false);

        // ---- 4. 조작설명 팝업 (Title 아트 재사용) --------------------------------------------
        var controlsPopup = CreatePopupArt("ControlsPopup", panel.transform, "popup_controls");
        var closeControlsButton = CreateCloseHitArea(controlsPopup.transform);
        WirePersistentClick(closeControlsButton.onClick, controller, nameof(PauseMenuController.OnCloseControlsPopupClicked));
        WirePersistentClick(closeControlsButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
        controlsPopup.SetActive(false);

        // ---- 5. 설정 팝업 (Title 아트 + 슬라이더 재사용) -------------------------------------
        var settingsPopup = CreateSettingsPopup(panel.transform, uiClickSfx);
        settingsPopup.SetActive(false);

        var so = new SerializedObject(controller);
        so.FindProperty("pausePanel").objectReferenceValue = panel;
        so.FindProperty("controlsPopup").objectReferenceValue = controlsPopup;
        so.FindProperty("settingsPopup").objectReferenceValue = settingsPopup;
        so.FindProperty("popupModalBlocker").objectReferenceValue = popupBlocker;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return controllerObject;
    }

    /// <summary>설정 팝업: Title과 동일한 아트 + 닫기(X, 취소) + BGM/효과음 슬라이더 + 저장후닫기.
    /// menuController는 일부러 비워둔다 - 이 씬엔 TitleMenuController가 없고,
    /// SettingsPopupController는 menuController가 비어있으면 그냥 자기 자신을 끈다(기존 fallback).</summary>
    private static GameObject CreateSettingsPopup(Transform parent, UiClickSfx uiClickSfx)
    {
        var popup = new GameObject("SettingsPopup", typeof(RectTransform), typeof(AudioSource), typeof(SettingsPopupController));
        popup.transform.SetParent(parent, false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = PopupSize;

        CreateImage("Art", popup.transform, TitleSprite("popup_settings"), Vector2.zero, PopupSize, false);

        var bgmSlider = CreateVolumeSlider("BgmSlider", popup.transform, new Vector2(90, 83));
        var sfxSlider = CreateVolumeSlider("SfxSlider", popup.transform, new Vector2(90, -88));
        var saveButton = CreateSaveButton(popup.transform);
        var closeButton = CreateCloseHitArea(popup.transform);

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
        so.FindProperty("sfxPreviewSource").objectReferenceValue = previewSource;
        so.FindProperty("sfxPreviewClip").objectReferenceValue = previewClip;
        so.ApplyModifiedPropertiesWithoutUndo();

        WirePersistentClick(saveButton.onClick, settingsController, nameof(SettingsPopupController.SaveAndClose));
        WirePersistentClick(saveButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));
        WirePersistentClick(closeButton.onClick, settingsController, nameof(SettingsPopupController.CancelAndClose));
        WirePersistentClick(closeButton.onClick, uiClickSfx, nameof(UiClickSfx.PlayClick));

        return popup;
    }

    private static GameObject CreatePopupArt(string name, Transform parent, string titleArtSpriteName)
    {
        var popup = new GameObject(name, typeof(RectTransform));
        popup.transform.SetParent(parent, false);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = PopupSize;

        CreateImage("Art", popup.transform, TitleSprite(titleArtSpriteName), Vector2.zero, PopupSize, false);
        return popup;
    }

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

    private static Slider CreateVolumeSlider(string name, Transform parent, Vector2 anchoredPosition)
    {
        var trackSprite = TitleSprite("slider_track");
        var handleSprite = TitleSprite("slider_handle");
        var trackSize = trackSprite != null ? new Vector2(trackSprite.rect.width, trackSprite.rect.height) : new Vector2(258, 15);

        var sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        var sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = trackSize;

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
        slider.value = 1f; // SettingsPopupController.OnEnable에서 저장값으로 다시 맞춤
        return slider;
    }

    private static Button CreateSaveButton(Transform parent)
    {
        var sprite = TitleSprite("button_settings_save");
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
        button.transition = Selectable.Transition.ColorTint;
        return button;
    }

    private static GameObject CreateFullScreenDim(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = obj.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f); // 대략값 - 딤 정도는 Scene 뷰에서 직접 조절
        image.raycastTarget = true;
        return obj;
    }

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

    /// <summary>기본/오버/클릭 3장 스프라이트를 쓰는 TitleImageButton 재사용 - 타이틀 메인 버튼과
    /// 동일한 패턴.</summary>
    private static GameObject CreateMenuButton(string name, Transform parent, string spritePrefix, Vector2 anchoredPosition)
    {
        var normal = LoadPauseSprite(spritePrefix + "_normal");
        var hover = LoadPauseSprite(spritePrefix + "_hover");
        var pressed = LoadPauseSprite(spritePrefix + "_click");
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

    private static void WireMenuButton(GameObject buttonObject, Object target, string methodName, UiClickSfx uiClickSfx)
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

    // ---- Sprite import / lookup ----------------------------------------------------------------

    private static void ImportSprites(string folder)
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

    private static Sprite LoadPauseSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{PauseMenuArtPath}/{name}.png");
    }

    private static Sprite TitleSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{TitleArtPath}/{name}.png");
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

    // ---- Scene helpers (EventSystem / Canvas / UiClickSfx) ---------------------------------------

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
        var existing = FindByNameIncludingInactive(scene, "Instruction Canvas");
        if (existing != null)
        {
            EnsureGraphicRaycaster(existing);
            return existing.transform;
        }

        existing = FindByNameIncludingInactive(scene, "HUD Canvas");
        if (existing != null)
        {
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
