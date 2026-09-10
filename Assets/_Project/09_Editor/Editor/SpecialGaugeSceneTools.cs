using System.IO;
using GameProject.Player;
using GameProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds/updates the special-attack charge gauge (sp_gauge_frame + sp_gauge_full from
/// 04_Art/StudentReplace/UI/HUD) - same non-destructive "works on whatever scene is open" +
/// "tune in ActionTest, then Sync to BackgroundTest" pattern as PlayerHpDisplaySceneTools.
/// </summary>
public static class SpecialGaugeSceneTools
{
    private const string ArtPath = "Assets/_Project/04_Art/StudentReplace/UI/HUD";

    private static readonly Vector2 GaugeSize = new Vector2(326f, 66f); // sp_gauge 원본 크기, 조정 없이 그대로
    private static readonly Vector2 DefaultBottomMargin = new Vector2(0f, 40f); // 화면 하단 중앙에서 위로 띄우는 여백 - 대략값

    [MenuItem("Tools/Class Template/Add Special Gauge To Scene")]
    public static void AddSpecialGaugeToScene()
    {
        ImportHudSprites();

        var scene = SceneManager.GetActiveScene();
        var gauge = EnsureGaugeInScene(scene);
        if (gauge == null)
        {
            Debug.LogWarning("현재 씬에서 PlayerActionTestController(주인공)를 못 찾았습니다 - ActionTest 또는 BackgroundTest 씬을 열고 실행하세요.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("스페셜 게이지를 추가/갱신했습니다. 위치는 대략값이니 Scene 뷰에서 직접 맞추고 저장(Ctrl+S)하세요.");
    }

    // 2026-08-18: 예전엔 여기 전용 "Sync Special Gauge To BackgroundTest"(ActionTest->
    // BackgroundTest 하드코딩, Stage2/3엔 안 미침) 명령이 있었는데, SyncFromActionTestTool의
    // LayoutRootNames("SpecialGauge" 포함)로 대체됨 - showDebugSpecialGauge 강제 끄기도
    // 그쪽으로 옮겨감(SyncFromActionTestTool.cs 참고).

    private static GameObject EnsureGaugeInScene(Scene scene)
    {
        var playerController = Object.FindObjectOfType<PlayerActionTestController>();
        if (playerController == null)
        {
            return null;
        }

        var gaugeObject = FindByNameIncludingInactive(scene, "SpecialGauge");
        if (gaugeObject != null)
        {
            // 이미 있으면 구조(위치 등)는 안 건드리고 레퍼런스만 다시 연결 (씬마다 플레이어 인스턴스가
            // 다르므로).
            WireReferences(gaugeObject, playerController);
            return gaugeObject;
        }

        var canvasTransform = EnsureHudCanvas(scene);
        gaugeObject = CreateGaugeHierarchy(canvasTransform);
        WireReferences(gaugeObject, playerController);
        return gaugeObject;
    }

    private static Transform EnsureHudCanvas(Scene scene)
    {
        // PlayerHpDisplaySceneTools와 같은 규칙: ActionTest.unity의 "Instruction Canvas"를 재사용,
        // 없으면(또는 HP UI가 먼저 만들어둔) "HUD Canvas"를 재사용, 그것도 없으면 새로 만든다.
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

    private static GameObject CreateGaugeHierarchy(Transform parent)
    {
        var root = new GameObject("SpecialGauge", typeof(RectTransform), typeof(SpecialGaugeDisplay));
        root.transform.SetParent(parent, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = DefaultBottomMargin;
        rootRect.sizeDelta = GaugeSize;

        CreateStretchedImage("Frame", root.transform, LoadSprite("sp_gauge_frame"));
        var fillImage = CreateFillImage("Fill", root.transform, LoadSprite("sp_gauge_full"));

        var so = new SerializedObject(root.GetComponent<SpecialGaugeDisplay>());
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static void WireReferences(GameObject gaugeObject, PlayerActionTestController controller)
    {
        var display = gaugeObject.GetComponent<SpecialGaugeDisplay>();
        if (display == null)
        {
            return;
        }

        var so = new SerializedObject(display);
        so.FindProperty("playerController").objectReferenceValue = controller;

        var fillProperty = so.FindProperty("fillImage");
        if (fillProperty.objectReferenceValue == null)
        {
            var fillTransform = gaugeObject.transform.Find("Fill");
            if (fillTransform != null)
            {
                fillProperty.objectReferenceValue = fillTransform.GetComponent<Image>();
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateStretchedImage(string name, Transform parent, Sprite sprite)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private static Image CreateFillImage(string name, Transform parent, Sprite sprite)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0f;
        return image;
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
}
