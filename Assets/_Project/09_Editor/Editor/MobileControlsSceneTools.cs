using System.IO;
using GameProject.Cameras;
using GameProject.Mobile;
using GameProject.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ActionTest.unity/BackgroundTest.unity 어느 쪽에 실행하든 모바일 터치 컨트롤(조그셔틀+점프+공격+
/// 대쉬+스페셜 차징)을 추가/갱신하고, 메인 카메라에 화면비율 고정(레터박스) 컴포넌트를 붙인다.
/// 2026-08-13 모바일 대응 확정 사항 + 2026-08-14 대쉬 전용 버튼 추가 + 2026-08-15 공격 버튼에서
/// 차징을 분리해 전용 버튼으로 뺌(실기기에서 한 버튼에 탭/홀드를 겸하니 판정이 늦어 갑갑하다는
/// 피드백). DeathScreenSceneTools/PauseMenuSceneTools와
/// 같은 "이미 있으면 위치는 안 건드리고 레퍼런스만 다시 연결" 패턴 - 몇 번을 다시 실행해도 안전함.
///
/// 터치 UI는 HUD Canvas(게임 화면 카메라의 레터박스 안쪽)와 별개의 전체화면 Canvas에 둔다 - 조작
/// 버튼은 실제 손가락이 닿는 화면 진짜 모서리에 있어야 편해서, 검은 여백 안쪽으로 밀려 들어가면 안
/// 되기 때문(레터박스가 생기는 기기에서도 버튼은 항상 화면 끝에 붙어있게).
/// </summary>
public static class MobileControlsSceneTools
{
    private const string ArtPath = "Assets/_Project/04_Art/StudentReplace/UI/MobileControls";

    [MenuItem("Tools/Class Template/Add Mobile Controls To Scene")]
    public static void AddMobileControlsToScene()
    {
        ImportSprites(ArtPath);

        var scene = SceneManager.GetActiveScene();
        EnsureEventSystem(scene);
        EnsureLetterboxOnMainCamera();

        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerActionTestController(주인공)를 찾을 수 없어 터치 컨트롤을 만들지 못했습니다.");
            return;
        }

        var rootObject = FindByNameIncludingInactive(scene, "MobileControlsCanvas");
        if (rootObject == null)
        {
            rootObject = CreateMobileControlsCanvas(playerController);
        }
        else
        {
            WireExistingReferences(rootObject, playerController);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("모바일 터치 컨트롤과 화면비율 고정(레터박스)을 추가(또는 갱신)했습니다. 조그셔틀 감지 영역 크기/버튼 위치는 대략값이니 Scene 뷰에서 직접 맞추세요. Play 모드에서 마우스 클릭으로 터치 시뮬레이션 테스트 가능(MobileControlsVisibility의 Preview In Editor가 켜져 있으면 PC 빌드 타깃이어도 항상 보임).");
    }

    [MenuItem("Tools/Class Template/Add Mobile Controls To Scene", true)]
    private static bool ValidateAddMobileControlsToScene()
    {
        // 2026-08-17: 스테이지1 이름만 인식하던 조건을 StageIdentity.IsRecognizedGameplayScene로
        // 넓힘 - 이 파일의 나머지 로직은 이미 "지금 열린 씬" 기준으로 완전히 범용이라(스테이지1
        // 전용 경로/데이터 없음), 활성화 조건만 넓히면 스테이지2 이상에도 그대로 동작함.
        return StageIdentity.IsRecognizedGameplayScene(SceneManager.GetActiveScene().name);
    }

    // ---- Camera aspect lock -------------------------------------------------------------------

    private static void EnsureLetterboxOnMainCamera()
    {
        var cameraObject = GameObject.FindWithTag("MainCamera");
        if (cameraObject == null)
        {
            Debug.LogWarning("Main Camera(MainCamera 태그)를 찾을 수 없어 화면비율 고정을 못 붙였습니다.");
            return;
        }

        if (cameraObject.GetComponent<AspectRatioLetterbox>() == null)
        {
            cameraObject.AddComponent<AspectRatioLetterbox>();
        }
    }

    // ---- Hierarchy ------------------------------------------------------------------------------

    private static GameObject CreateMobileControlsCanvas(PlayerActionTestController playerController)
    {
        var canvasObject = new GameObject("MobileControlsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileControlsVisibility));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 레터박스와 무관하게 항상 진짜 화면 전체를 씀(HUD Canvas와 다른 점) - 조작 버튼은
        // 화면 여백이 아니라 실제 화면 모서리에 붙어야 손이 편하다.

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateJoystick(canvasObject.transform, playerController);
        CreateJumpButton(canvasObject.transform, playerController);
        CreateAttackButton(canvasObject.transform, playerController);
        CreateDashButton(canvasObject.transform, playerController);
        CreateSpecialChargeButton(canvasObject.transform, playerController);

        return canvasObject;
    }

    private static void WireExistingReferences(GameObject rootObject, PlayerActionTestController playerController)
    {
        var joystick = rootObject.transform.Find("JoystickHitArea")?.GetComponent<MobileJoystick>();
        if (joystick != null)
        {
            var so = new SerializedObject(joystick);
            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var attackButton = rootObject.transform.Find("AttackButton")?.GetComponent<MobileAttackButton>();
        if (attackButton != null)
        {
            var so = new SerializedObject(attackButton);
            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var jumpButtonTransform = rootObject.transform.Find("JumpButton");
        var jumpButton = jumpButtonTransform != null ? jumpButtonTransform.GetComponent<MobileTouchButton>() : null;
        if (jumpButton != null)
        {
            var so = new SerializedObject(jumpButton);
            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var dashButton = rootObject.transform.Find("DashButton")?.GetComponent<MobileDashButton>();
        if (dashButton != null)
        {
            var so = new SerializedObject(dashButton);
            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            // 2026-08-14 추가된 버튼 - 기존에(2026-08-13에) 이미 만들어둔 씬에는 없을 수 있으니,
            // 없으면 새로 만들어서 붙여준다(이미 있는 조그셔틀/점프/공격 위치는 안 건드림).
            CreateDashButton(rootObject.transform, playerController);
        }

        var specialChargeButton = rootObject.transform.Find("SpecialChargeButton")?.GetComponent<MobileSpecialChargeButton>();
        if (specialChargeButton != null)
        {
            var so = new SerializedObject(specialChargeButton);
            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            // 2026-08-15 추가된 버튼(공격 버튼에서 차징 기능 분리) - 그 이전에 만들어둔 씬에는
            // 없을 수 있으니, 없으면 새로 만들어서 붙여준다(기존 버튼 위치는 안 건드림).
            CreateSpecialChargeButton(rootObject.transform, playerController);
        }
    }

    /// <summary>조그셔틀: 감지 영역(JoystickHitArea, 넓고 안 보임)을 화면 좌하단 넓은 범위에 두고,
    /// 그 자식으로 눈에 보이는 프레임+스틱을 고정된 위치에 배치. 감지 영역 크기는 앵커 기반(비율)이라
    /// 화면 크기가 달라져도 알아서 비례하고, Scene 뷰에서 앵커/오프셋만 조절하면 범위를 바꿀 수 있음.</summary>
    private static MobileJoystick CreateJoystick(Transform parent, PlayerActionTestController playerController)
    {
        var hitAreaObject = new GameObject("JoystickHitArea", typeof(RectTransform), typeof(Image), typeof(MobileJoystick));
        hitAreaObject.transform.SetParent(parent, false);
        var hitAreaRect = hitAreaObject.GetComponent<RectTransform>();
        // 화면 좌하단 40% x 55% 영역 - 비율 기반이라 기기 화면 크기가 달라져도 자동으로 비례함.
        hitAreaRect.anchorMin = new Vector2(0f, 0f);
        hitAreaRect.anchorMax = new Vector2(0.4f, 0.55f);
        hitAreaRect.offsetMin = Vector2.zero;
        hitAreaRect.offsetMax = Vector2.zero;

        var hitAreaImage = hitAreaObject.GetComponent<Image>();
        hitAreaImage.color = new Color(0f, 0f, 0f, 0f); // 안 보임 - 순수 터치 감지용
        hitAreaImage.raycastTarget = true;

        var frameSprite = LoadSprite("joystick_frame");
        var frameSize = frameSprite != null ? new Vector2(frameSprite.rect.width, frameSprite.rect.height) : new Vector2(331, 331);
        var frameObject = new GameObject("JoystickFrame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(hitAreaObject.transform, false);
        var frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0f);
        frameRect.anchorMax = new Vector2(0f, 0f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = new Vector2(200f, 180f); // 대략값
        frameRect.sizeDelta = frameSize;
        var frameImage = frameObject.GetComponent<Image>();
        frameImage.sprite = frameSprite;
        frameImage.preserveAspect = true;
        frameImage.raycastTarget = false;

        var stickSprite = LoadSprite("joystick_stick");
        var stickSize = stickSprite != null ? new Vector2(stickSprite.rect.width, stickSprite.rect.height) : new Vector2(331, 331);
        var stickObject = new GameObject("JoystickStick", typeof(RectTransform), typeof(Image));
        stickObject.transform.SetParent(frameObject.transform, false);
        var stickRect = stickObject.GetComponent<RectTransform>();
        stickRect.anchorMin = new Vector2(0.5f, 0.5f);
        stickRect.anchorMax = new Vector2(0.5f, 0.5f);
        stickRect.pivot = new Vector2(0.5f, 0.5f);
        stickRect.anchoredPosition = Vector2.zero;
        stickRect.sizeDelta = stickSize;
        var stickImage = stickObject.GetComponent<Image>();
        stickImage.sprite = stickSprite;
        stickImage.preserveAspect = true;
        stickImage.raycastTarget = false;

        var joystick = hitAreaObject.GetComponent<MobileJoystick>();
        var so = new SerializedObject(joystick);
        so.FindProperty("frame").objectReferenceValue = frameRect;
        so.FindProperty("stick").objectReferenceValue = stickRect;
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();

        return joystick;
    }

    private static MobileTouchButton CreateJumpButton(Transform parent, PlayerActionTestController playerController)
    {
        var normal = LoadSprite("jump_button_normal");
        var pressed = LoadSprite("jump_button_pressed");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(222, 222);

        var obj = new GameObject("JumpButton", typeof(RectTransform), typeof(Image), typeof(MobileTouchButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-150f, 280f); // 대략값
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<MobileTouchButton>();
        var so = new SerializedObject(button);
        so.FindProperty("normalSprite").objectReferenceValue = normal;
        so.FindProperty("pressedSprite").objectReferenceValue = pressed;
        so.FindProperty("targetImage").objectReferenceValue = image;
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    private static MobileAttackButton CreateAttackButton(Transform parent, PlayerActionTestController playerController)
    {
        var normal = LoadSprite("attack_button_normal");
        var pressed = LoadSprite("attack_button_pressed");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(222, 222);

        var obj = new GameObject("AttackButton", typeof(RectTransform), typeof(Image), typeof(MobileAttackButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-320f, 160f); // 대략값
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<MobileAttackButton>();
        var so = new SerializedObject(button);
        so.FindProperty("normalSprite").objectReferenceValue = normal;
        so.FindProperty("pressedSprite").objectReferenceValue = pressed;
        so.FindProperty("targetImage").objectReferenceValue = image;
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    /// <summary>전용 대쉬 버튼(2026-08-14) - 공격 버튼 좌측에 기본 배치. 조그셔틀 플릭 대쉬를
    /// 대체함(MobileJoystick 주석 참고).</summary>
    private static MobileDashButton CreateDashButton(Transform parent, PlayerActionTestController playerController)
    {
        var normal = LoadSprite("dash_button_normal");
        var pressed = LoadSprite("dash_button_pressed");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(222, 222);

        var obj = new GameObject("DashButton", typeof(RectTransform), typeof(Image), typeof(MobileDashButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-500f, 160f); // 대략값 - 공격 버튼(-320, 160) 좌측
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<MobileDashButton>();
        var so = new SerializedObject(button);
        so.FindProperty("normalSprite").objectReferenceValue = normal;
        so.FindProperty("pressedSprite").objectReferenceValue = pressed;
        so.FindProperty("targetImage").objectReferenceValue = image;
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    /// <summary>스페셜 차징 전용 버튼(2026-08-15) - 원래 AttackButton 하나가 탭(일반공격)/홀드(차징)를
    /// 겸했는데, 실기기에서 판정이 늦어 갑갑하다는 피드백으로 분리(MobileSpecialChargeButton 주석
    /// 참고). 기본 위치는 대쉬 버튼 좌측 + 공격 버튼 위(사용자 지정) - 정확한 좌표는 대략값이니
    /// Scene 뷰에서 직접 맞추면 됨.</summary>
    private static MobileSpecialChargeButton CreateSpecialChargeButton(Transform parent, PlayerActionTestController playerController)
    {
        var normal = LoadSprite("attack_special_button_normal");
        var pressed = LoadSprite("attack_special_button_pressed");
        var size = normal != null ? new Vector2(normal.rect.width, normal.rect.height) : new Vector2(222, 222);

        var obj = new GameObject("SpecialChargeButton", typeof(RectTransform), typeof(Image), typeof(MobileSpecialChargeButton));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-680f, 280f); // 대략값 - 대쉬 버튼(-500, 160)보다 왼쪽, 공격 버튼(-320, 160)보다 위
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        image.raycastTarget = true;

        var button = obj.GetComponent<MobileSpecialChargeButton>();
        var so = new SerializedObject(button);
        so.FindProperty("normalSprite").objectReferenceValue = normal;
        so.FindProperty("pressedSprite").objectReferenceValue = pressed;
        so.FindProperty("targetImage").objectReferenceValue = image;
        so.FindProperty("playerController").objectReferenceValue = playerController;
        so.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    // ---- Import helpers ---------------------------------------------------------------------------

    private static void ImportSprites(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning($"모바일 UI 리소스 폴더를 못 찾음: {folder}");
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

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtPath}/{name}.png");
    }

    // ---- Scene helpers (EventSystem / search) ----------------------------------------------------

    private static void EnsureEventSystem(Scene scene)
    {
        if (FindByNameIncludingInactive(scene, "EventSystem") != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
