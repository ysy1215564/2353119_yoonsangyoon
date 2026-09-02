using System.IO;
using System.Linq;
using GameProject.Environment;
using GameProject.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HP 회복 상자(2026-08-14) - 04_Art/StudentReplace/Items의 상자 여는 프레임/아이템 이미지를
/// 임포트하고, 02_Prefabs/Environment/Items/HpItemBox.prefab을 만들거나(프레임 개수가 바뀌었으면)
/// 새로고침한 뒤, 현재 열려있는 씬에 테스트용 인스턴스 하나를 놓는다(없을 때만 - 몬스터 배치
/// 도구와 동일한 관례). 추가 배치는 이 프리팹을 Project 창에서 Scene 뷰로 자유롭게 드래그하면 됨
/// (Ground/Monster와 동일 - 별도 도구 없이 몇 개든 원하는 자리에).
///
/// 씬별로 따로 연결해줘야 하는 참조가 없다 - HpItemPickup이 PlayerHpDisplay/카메라를 런타임에
/// 스스로 찾기 때문에(ActionTest의 "_Runtime" 클론 패턴과 동일한 이유로 매 프레임이 아니라
/// 필요한 그 순간에 1회 탐색), BackgroundTest에 몇 개를 복사해 놓든 전부 알아서 그 씬의 HP UI를
/// 찾아간다.
/// </summary>
public static class HpItemBoxSceneTools
{
    private const string ArtRoot = "Assets/_Project/04_Art/StudentReplace/Items";
    private const string FramesFolder = ArtRoot + "/HpBox_frames";
    private const string ItemSpritePath = ArtRoot + "/hpitem.png";
    // 2026-08-29: 확장자를 고정한 ".../hpitem.mp3"였다. W11 사운드 문서는 "확장자는 자유입니다
    // (wav/mp3/ogg 다 됨)"라고 안내하는데 여기만 mp3가 아니면 못 찾았고, 효과음은 없어도 그냥
    // 넘어가는 자리라 경고도 없이 조용히 무음이 됐다 - 학생은 소리를 넣었는데 안 난다고 본다.
    // 플레이어/몬스터 효과음과 같은 해석기(FindAudioClipByBaseName)를 쓰도록 바꿨다.
    private const string ItemSfxBasePath = "Assets/_Project/06_Audio/SFX/Items/hpitem";
    private const string PrefabFolder = "Assets/_Project/02_Prefabs/Environment/Items";
    private const string PrefabPath = PrefabFolder + "/HpItemBox.prefab";
    private const float PixelsPerUnit = 120f;
    private const string InstanceName = "HpItemBox_Instance";

    [MenuItem("Tools/Class Template/Add HP Item Box To Scene")]
    public static void AddHpItemBoxToScene()
    {
        ImportArt();
        var prefab = EnsurePrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"HP 상자 아트를 찾을 수 없습니다 - {FramesFolder}에 hpbox_frames_*.png, {ItemSpritePath}에 hpitem.png가 있는지 확인하세요.");
            return;
        }

        // HP 아이템이 날아가는 목표 지점(PlayerHpDisplay.BackgroundRect)이 예전 씬에는 비어있을 수
        // 있어서, 여기서 같이 한 번 갱신해준다(이미 있으면 위치는 안 건드리고 참조만 재연결하는
        // PlayerHpDisplaySceneTools의 기존 동작 그대로 - 안전하게 몇 번이든 다시 실행 가능).
        PlayerHpDisplaySceneTools.AddPlayerHpDisplayToScene();

        var scene = SceneManager.GetActiveScene();
        if (FindByNameIncludingInactive(scene, InstanceName) == null)
        {
            // 2D 직교 카메라는 보통 Z -10에 있는데, cameraObject.transform.position을 그대로
            // 더해버리면 그 Z(-10)까지 같이 따라와서 스프라이트 평면(Z 0)이 아니라 카메라 코앞에
            // 놓여버린다 - Scene 뷰(자유 시점)에는 보여도 실제 게임 카메라로는 안 보이는 실수
            // (2026-08-14 발견). X만 카메라 기준으로 가져오고 Y/Z는 항상 정해진 절대값을 쓰도록
            // 명시적으로 새로 만든다.
            var cameraObject = GameObject.FindWithTag("MainCamera");
            var cameraX = cameraObject != null ? cameraObject.transform.position.x : 0f;
            var spawnPosition = new Vector3(cameraX + 3f, -3.4f, 0f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = InstanceName;
            instance.transform.position = spawnPosition;

            Debug.Log("HP 회복 상자를 씬에 배치했습니다. 위치는 대략값이니 Scene 뷰에서 직접 옮기고 저장(Ctrl+S)하세요. 더 놓고 싶으면 같은 프리팹을 드래그해서 복사하면 됩니다.");
        }
        else
        {
            Debug.Log($"'{InstanceName}'가 이미 씬에 있습니다. 더 배치하려면 Project 창의 {PrefabPath}를 Scene 뷰로 드래그하세요.");
        }

        // ActionTest 전용 디버그 리스폰 버튼 - BackgroundTest는 "한 번 쓰고 끝"이 실제 규칙이라
        // 절대 넣지 않는다(사용자 확정, 2026-08-14).
        if (scene.name == "ActionTest")
        {
            EnsureTestRespawner(scene, prefab);
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsureTestRespawner(Scene scene, GameObject prefab)
    {
        var respawnerObject = FindByNameIncludingInactive(scene, "HpItemBoxTestRespawner");
        if (respawnerObject == null)
        {
            respawnerObject = new GameObject("HpItemBoxTestRespawner", typeof(HpItemBoxTestRespawner));
        }

        var so = new SerializedObject(respawnerObject.GetComponent<HpItemBoxTestRespawner>());
        so.FindProperty("hpItemBoxPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsurePrefab()
    {
        var openFrames = LoadFrames(FramesFolder);
        var itemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ItemSpritePath);
        if (openFrames.Length == 0 || itemSprite == null)
        {
            return null;
        }

        // 효과음은 없어도 프리팹 생성 자체는 막지 않음(선택 사항 취급) - 파일이 나중에 추가되면
        // 다시 실행할 때 자동으로 채워짐.
        var itemClip = ActionTestSceneBuilder.FindAudioClipByBaseName(ItemSfxBasePath, warnIfMissing: false);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            // 이미 있으면 프레임/아이템 스프라이트/효과음만 새로고침 - 콜라이더 크기나 healAmount
            // 기본값 등 사용자가 프리팹에서 직접 조절했을 수 있는 값은 안 건드림("Refresh Title
            // Motion Frames"와 동일한 관례 - 전체 재생성이 아니라 필요한 것만 갱신).
            RefreshExistingPrefab(openFrames, itemSprite, itemClip);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        Directory.CreateDirectory(PrefabFolder);
        BuildNewPrefab(openFrames, itemSprite, itemClip);
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void RefreshExistingPrefab(Sprite[] openFrames, Sprite itemSprite, AudioClip itemClip)
    {
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var box = contents.transform.Find("Box");
            var item = contents.transform.Find("Item");
            if (box == null || item == null)
            {
                Debug.LogWarning($"{PrefabPath}의 구조가 예상과 달라 자동 새로고침을 건너뜁니다 (Box/Item 자식을 못 찾음).");
                return;
            }

            var boxRenderer = box.GetComponent<SpriteRenderer>();
            if (boxRenderer != null)
            {
                boxRenderer.sprite = openFrames[0];
            }

            // 콜라이더 크기 수정(2026-08-14) - 기존에 만들어진 프리팹은 콜라이더가 기본값(1x1,
            // 중심 0,0)에 머물러 있어서 공격 판정이 스프라이트와 거의 안 맞았음. BuildNewPrefab의
            // 같은 로직 참고.
            var boxCollider = box.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                var bounds = openFrames[0].bounds;
                boxCollider.size = bounds.size;
                boxCollider.offset = bounds.center;
            }

            var boxController = box.GetComponent<HpItemBoxController>();
            if (boxController != null)
            {
                var so = new SerializedObject(boxController);
                var framesProperty = so.FindProperty("openFrames");
                framesProperty.arraySize = openFrames.Length;
                for (var i = 0; i < openFrames.Length; i++)
                {
                    framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = openFrames[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var itemRenderer = item.GetComponent<SpriteRenderer>();
            if (itemRenderer != null)
            {
                itemRenderer.sprite = itemSprite;
            }

            var itemPickup = item.GetComponent<HpItemPickup>();
            if (itemPickup != null && itemClip != null)
            {
                var itemSo = new SerializedObject(itemPickup);
                itemSo.FindProperty("flySfx").FindPropertyRelative("clip").objectReferenceValue = itemClip;
                itemSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void BuildNewPrefab(Sprite[] openFrames, Sprite itemSprite, AudioClip itemClip)
    {
        var root = new GameObject("HpItemBox");

        var boxObject = new GameObject("Box", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(HpItemBoxController));
        boxObject.transform.SetParent(root.transform, false);
        var boxRenderer = boxObject.GetComponent<SpriteRenderer>();
        boxRenderer.sprite = openFrames[0];
        boxRenderer.sortingOrder = 8;
        var boxCollider = boxObject.GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true; // 스펙: 플레이어/몬스터가 그냥 통과, 공격 판정에는 걸림
        // 스크립트로 SpriteRenderer+Collider2D를 같이 만들면 콜라이더가 스프라이트 크기에 자동으로
        // 안 맞춰지고 기본값(1x1, 중심 0,0)으로 남는다(에디터 "Add Component" 메뉴로 손으로 추가할
        // 때만 자동 맞춤이 적용됨) - sprite.bounds(임포트 시점 PPU/피벗이 이미 반영된 실제 월드 크기)
        // 로 명시적으로 맞춰준다. 2026-08-14: 이걸 빠뜨렸더니 콜라이더가 스프라이트 아래쪽 절반과
        // 땅속 일부에만 걸쳐 있어서, 공격이 눈에는 맞는 것처럼 보여도 판정에 거의 안 걸렸다.
        var bounds = openFrames[0].bounds;
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center;

        // 아이템은 Box의 자식이 아니라 형제(둘 다 root 밑) - Box가 열림 애니메이션 끝에 자기 자신을
        // Destroy() 하는데, 만약 아이템이 그 자식이었다면 같이 파괴돼버리기 때문(2026-08-14 설계 시
        // 발견하고 피한 함정). Sorting Order를 Box보다 낮게 둬서 "뒤에 배치"를 표현 - 같은 위치에
        // 있어도 Box가 위에 그려져 시작 프레임(닫힌 상자)에서는 완전히 가려지고, 여는 프레임이
        // 진행되며 생기는 빈틈 사이로 자연스럽게 드러난다(사용자 확정: 열리는 순간부터 뒤에 존재).
        var itemObject = new GameObject("Item", typeof(SpriteRenderer), typeof(HpItemPickup));
        itemObject.transform.SetParent(root.transform, false);
        var itemRenderer = itemObject.GetComponent<SpriteRenderer>();
        itemRenderer.sprite = itemSprite;
        itemRenderer.sortingOrder = 7;

        if (itemClip != null)
        {
            var itemSo = new SerializedObject(itemObject.GetComponent<HpItemPickup>());
            itemSo.FindProperty("flySfx").FindPropertyRelative("clip").objectReferenceValue = itemClip;
            itemSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var boxSo = new SerializedObject(boxObject.GetComponent<HpItemBoxController>());
        var framesProperty = boxSo.FindProperty("openFrames");
        framesProperty.arraySize = openFrames.Length;
        for (var i = 0; i < openFrames.Length; i++)
        {
            framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = openFrames[i];
        }

        boxSo.FindProperty("item").objectReferenceValue = itemObject.GetComponent<HpItemPickup>();
        boxSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ImportArt()
    {
        ImportSpriteFolder(FramesFolder);
        ImportSprite(ItemSpritePath);
    }

    private static void ImportSpriteFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
        {
            ImportSprite(path.Replace('\\', '/'));
        }
    }

    private static void ImportSprite(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            return;
        }

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f); // 바닥에 놓는 오브젝트라 아래쪽 중앙 기준
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadFrames(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new Sprite[0];
        }

        return Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/')))
            .Where(sprite => sprite != null)
            .ToArray();
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
