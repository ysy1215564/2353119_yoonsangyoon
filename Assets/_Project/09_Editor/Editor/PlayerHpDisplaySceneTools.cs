using System.Collections.Generic;
using System.IO;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds/updates the pip-style player HP display (hp_background + Dot on/off from
/// 04_Art/StudentReplace/UI/HUD) - non-destructive, works on whatever scene is currently open
/// (unlike the full "Create ... Scene" rebuilders), matching the "Add Background Music To Scene"
/// style of tool.
///
/// 사용자 확정 흐름(2026-08-11): ActionTest에서 먼저 추가하고 위치를 직접 맞춘 뒤,
/// "Sync Player & HUD From ActionTest"(SyncFromActionTestTool, 2026-08-17~)로 그 결과를
/// BackgroundTest/Stage2+에도 그대로 반영. (이 파일에 있던 전용 SyncPlayerHpDisplayToBackgroundTest는
/// 그 통합 도구에 완전히 흡수되어 2026-08-18 삭제됨.)
/// </summary>
public static class PlayerHpDisplaySceneTools
{
    private const string ArtPath = "Assets/_Project/04_Art/StudentReplace/UI/HUD";

    private static readonly Vector2 DefaultRootMargin = new Vector2(24f, -24f); // 화면 좌상단 여백 - 대략값
    private static readonly Vector2 BackgroundSize = new Vector2(591f, 155f); // hp_background.png 원본 크기, 조정 없이 그대로
    private static readonly Vector2 DefaultDotGroupPosition = new Vector2(170f, -78f); // helmet 원 다음, 세로 중앙 - 대략값

    private static readonly Vector2 InstructionTextDefaultPosition = new Vector2(32f, -28f);

    [MenuItem("Tools/Class Template/Add Player HP Display To Scene")]
    public static void AddPlayerHpDisplayToScene()
    {
        ImportHudSprites();

        var scene = SceneManager.GetActiveScene();
        var display = EnsureDisplayInScene(scene);
        if (display == null)
        {
            Debug.LogWarning("현재 씬에서 PlayerActionTestController(주인공)를 못 찾았습니다 - ActionTest 또는 BackgroundTest 씬을 열고 실행하세요.");
            return;
        }

        MoveInstructionTextIfDefault(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("주인공 HP UI를 추가/갱신했습니다. 위치는 대략값이니 Scene 뷰에서 직접 맞추고 저장(Ctrl+S)하세요.");
    }

    /// <summary>
    /// 일회성 정리 도구(2026-08-11) - CreateInstructionCanvas()가 idempotent가 아니었던 시절
    /// (MonsterB/C Action Test를 갱신할 때마다 "Instruction Canvas"/"Instruction Text"를 매번 새로
    /// 만들었음) 쌓인 중복을 정리한다. 메서드 자체는 이제 고쳐졌으니(중복 방지) 이 도구는 기존
    /// ActionTest.unity에 이미 쌓여있는 중복만 한 번 청소하면 됨 - 안전하게 여러 번 실행 가능
    /// (중복이 없으면 아무 일도 안 함).
    /// </summary>
    [MenuItem("Tools/Class Template/Remove Duplicate Instruction Canvases")]
    public static void RemoveDuplicateInstructionCanvases()
    {
        var scene = SceneManager.GetActiveScene();

        // 2026-08-24: 두 이름을 다 모은다. 이 캔버스는 원래 ActionTest에서만 "Instruction Canvas"로
        // 불렸는데 이름을 "HUD Canvas"로 통일했다 - 옛 이름으로 만들어진 씬과 새 이름으로 만들어진
        // 씬이 섞일 수 있고, 중복이 생긴다면 오히려 그 둘이 한 씬에 같이 있는 형태다.
        var canvases = CollectAllByName(scene, "HUD Canvas");
        canvases.AddRange(CollectAllByName(scene, "Instruction Canvas"));
        if (canvases.Count <= 1)
        {
            Debug.Log("중복된 HUD 캔버스가 없습니다.");
            return;
        }

        // 첫 번째를 살리고, 나머지의 자식은 전부 살아남는 캔버스로 옮긴 뒤(PlayerHpDisplay 등 뭐가
        // 붙어있든 안 잃어버리게) 빈 캔버스만 지운다.
        var keep = canvases[0];
        for (var i = 1; i < canvases.Count; i++)
        {
            var duplicate = canvases[i];
            for (var c = duplicate.transform.childCount - 1; c >= 0; c--)
            {
                duplicate.transform.GetChild(c).SetParent(keep.transform, true);
            }

            Object.DestroyImmediate(duplicate);
        }

        // "Instruction Text"도 같이 중복 생성됐었으니, 남은 것 중 하나만 남기고 정리.
        var texts = CollectAllByName(scene, "Instruction Text");
        for (var i = 1; i < texts.Count; i++)
        {
            Object.DestroyImmediate(texts[i]);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"중복된 HUD 캔버스 {canvases.Count - 1}개 / Instruction Text {Mathf.Max(0, texts.Count - 1)}개를 정리했습니다. Ctrl+S로 저장하세요.");
    }

    // 2026-08-18: 예전엔 여기 전용 "Sync Player HP Display To BackgroundTest"(ActionTest->
    // BackgroundTest 하드코딩, Stage2/3엔 안 미침) 명령이 있었는데, SyncFromActionTestTool의
    // LayoutRootNames("PlayerHpDisplay" 포함)로 대체됨 - Max Hp/showDebugHpText 강제 끄기/
    // ForceRefresh 타이밍 챙기기까지 전부 그쪽으로 옮겨감(SyncFromActionTestTool.cs 참고).

    private static GameObject EnsureDisplayInScene(Scene scene)
    {
        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            return null;
        }

        var displayObject = FindByNameIncludingInactive(scene, "PlayerHpDisplay");
        if (displayObject != null)
        {
            // 이미 있으면 구조(위치 등)는 안 건드리고 플레이어/스프라이트 레퍼런스만 다시 연결
            // (씬마다 플레이어 인스턴스가 다르므로).
            WireReferences(displayObject, playerController);
            return displayObject;
        }

        var canvasTransform = EnsureHudCanvas(scene);
        displayObject = CreateDisplayHierarchy(canvasTransform);
        WireReferences(displayObject, playerController);
        return displayObject;
    }

    private static Transform EnsureHudCanvas(Scene scene)
    {
        // ActionTest.unity는 이미 "Instruction Canvas"가 있어서 재사용 - 캔버스를 하나 더 안 만듦.
        // 없으면(BackgroundTest 등) 전용 캔버스를 새로 만든다.
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

        var canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }

    private static GameObject CreateDisplayHierarchy(Transform parent)
    {
        var root = new GameObject("PlayerHpDisplay", typeof(RectTransform), typeof(PlayerHpDisplay));
        root.transform.SetParent(parent, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = DefaultRootMargin;
        rootRect.sizeDelta = BackgroundSize;

        var backgroundRect = CreateImage("Background", root.transform, LoadSprite("hp_background"), Vector2.zero, BackgroundSize);

        var dotGroupObject = new GameObject("DotGroup", typeof(RectTransform));
        dotGroupObject.transform.SetParent(root.transform, false);
        var dotGroupRect = dotGroupObject.GetComponent<RectTransform>();
        dotGroupRect.anchorMin = new Vector2(0f, 1f);
        dotGroupRect.anchorMax = new Vector2(0f, 1f);
        dotGroupRect.pivot = new Vector2(0f, 0.5f);
        dotGroupRect.anchoredPosition = DefaultDotGroupPosition;
        dotGroupRect.sizeDelta = Vector2.zero;

        var so = new SerializedObject(root.GetComponent<PlayerHpDisplay>());
        so.FindProperty("dotGroup").objectReferenceValue = dotGroupRect;
        so.FindProperty("background").objectReferenceValue = backgroundRect;
        so.FindProperty("dotOnSprite").objectReferenceValue = LoadSprite("hp_dot_black");
        so.FindProperty("dotOffSprite").objectReferenceValue = LoadSprite("hp_dot_white");
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static void WireReferences(GameObject displayObject, PlayerActionTestController controller)
    {
        var display = displayObject.GetComponent<PlayerHpDisplay>();
        if (display == null)
        {
            return;
        }

        var so = new SerializedObject(display);
        so.FindProperty("playerController").objectReferenceValue = controller;

        // 2026-08-14 추가된 필드 - 이미 만들어진 씬(ActionTest/BackgroundTest)에는 없던 참조라,
        // 없으면 기존 "Background" 자식에서 찾아 한 번 채워준다(HP 아이템이 날아가는 목표 지점).
        var backgroundProperty = so.FindProperty("background");
        if (backgroundProperty.objectReferenceValue == null)
        {
            backgroundProperty.objectReferenceValue = displayObject.transform.Find("Background")?.GetComponent<RectTransform>();
        }

        var dotOnProperty = so.FindProperty("dotOnSprite");
        if (dotOnProperty.objectReferenceValue == null)
        {
            dotOnProperty.objectReferenceValue = LoadSprite("hp_dot_black");
        }

        var dotOffProperty = so.FindProperty("dotOffSprite");
        if (dotOffProperty.objectReferenceValue == null)
        {
            dotOffProperty.objectReferenceValue = LoadSprite("hp_dot_white");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>ActionTest.unity 전용(조작설명 텍스트가 그 씬에만 있음) - 아직 기본 위치(좌상단)
    /// 그대로면 HP UI와 안 겹치게 우측으로 옮기고, 이미 손으로 옮겨둔 상태면 건드리지 않는다.</summary>
    private static void MoveInstructionTextIfDefault(Scene scene)
    {
        var textObject = FindByNameIncludingInactive(scene, "Instruction Text");
        if (textObject == null)
        {
            return;
        }

        var rect = textObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        var isAtOriginalDefault = rect.anchorMin == new Vector2(0f, 1f)
            && Vector2.Distance(rect.anchoredPosition, InstructionTextDefaultPosition) < 0.5f;
        if (!isAtOriginalDefault)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-InstructionTextDefaultPosition.x, InstructionTextDefaultPosition.y);
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rect;
    }

    private static void ImportHudSprites()
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

    private static List<GameObject> CollectAllByName(Scene scene, string name)
    {
        var results = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            CollectInChildren(root.transform, name, results);
        }

        return results;
    }

    private static void CollectInChildren(Transform parent, string name, List<GameObject> results)
    {
        if (parent.name == name)
        {
            results.Add(parent.gameObject);
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            CollectInChildren(parent.GetChild(i), name, results);
        }
    }
}
