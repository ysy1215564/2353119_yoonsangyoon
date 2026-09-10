using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 학생이 폴더에 "새로 추가"하는 그림에, 같은 폴더에 이미 있던 그림의 임포트 설정을 그대로 물려준다.
///
/// 왜 필요한가 (2026-09-08에 실제로 터진 것):
/// 학생이 프레임을 늘릴 때 - 예: idle 2장 -> 3장 - 기존 파일을 복사해 새 이름으로 만든다. 그 파일에는
/// .meta 가 없어서 Unity 가 프로젝트 기본값(피벗 Center + PPU 100)으로 새로 만든다. 그런데 이 프로젝트의
/// 몸 그림은 피벗 (0.5, 0)(발밑) + PPU 120 이라, 그 프레임만 캐릭터가 배꼽 기준으로 놓여서
/// **화면 아래로 뚝 떨어지고** 크기도 1.2배 커진다. 에러는 하나도 안 뜬다.
/// 프레임 개수를 바꾸는 것은 2주차부터 매 주차 과제의 핵심 작업이라 학생 전원이 겪는다.
///
/// 왜 "폴더 규칙"이 아니라 "옆 파일 복사"인가:
/// 규격이 한 종류가 아니다. 몸은 (0.5, 0)/120, 이펙트는 (0.5, 0.5)/120, UI는 Center/100 이다
/// (ActionTestSceneBuilder.ImportFrameFolder 호출부 참고 - 몸은 isPlayer=true+발밑 피벗,
/// 이펙트는 false+중앙 피벗). 폴더 이름으로 분기하면 Effects, MonsterB/attack1_effect_frames,
/// MonsterC/Effects/Projectile 처럼 예외가 계속 생기고 새 폴더가 늘 때마다 깨진다.
/// **같은 폴더의 형제 파일을 보고 맞추면** 규격이 몇 종류든, 폴더가 늘어나든 알아서 맞는다.
///
/// 건드리는 범위:
///  - 04_Art/StudentReplace 아래만
///  - .meta 가 아직 없는 **새 파일만**(importSettingsMissing). 기존 파일은 절대 안 건드린다
///    (학생이 일부러 바꾼 값을 덮어쓰지 않기 위해서)
///  - 같은 폴더에 물려받을 형제가 없으면 아무것도 하지 않는다
/// </summary>
public sealed class StudentSpriteImportDefaults : AssetPostprocessor
{
    private const string StudentArtRoot = "Assets/_Project/04_Art/StudentReplace";

    private void OnPreprocessTexture()
    {
        if (!IsUnderStudentArt(assetPath))
        {
            return;
        }

        // .meta 가 이미 있으면(= 기존 파일) 손대지 않는다. 새로 들어온 파일만 채워준다.
        if (!assetImporter.importSettingsMissing)
        {
            return;
        }

        var sibling = FindSettledSibling(assetPath);
        if (sibling == null)
        {
            return; // 물려받을 형제가 없다 - 기본값 그대로 둔다
        }

        CopyImportSettings(sibling, (TextureImporter)assetImporter);
        Debug.Log($"[{Path.GetFileName(assetPath)}] 새 그림이라 같은 폴더의 «{Path.GetFileName(sibling.assetPath)}» 설정을 물려받았습니다 (피벗·Pixels Per Unit 등).");
    }

    private static bool IsUnderStudentArt(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').StartsWith(StudentArtRoot);
    }

    /// <summary>같은 폴더에서 이미 임포트 설정이 자리잡은 그림 하나를 찾는다.
    /// 새로 들어오는 중인 파일(.meta 없음)은 기준으로 삼지 않는다 - 여러 장을 한꺼번에 넣었을 때
    /// 아직 설정이 없는 형제를 보고 잘못 따라가는 것을 막기 위해서다.</summary>
    private static TextureImporter FindSettledSibling(string newAssetPath)
    {
        var folder = Path.GetDirectoryName(newAssetPath)?.Replace(Path.DirectorySeparatorChar, '/');
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        var candidates = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
            .Select(p => p.Replace(Path.DirectorySeparatorChar, '/'))
            .Where(p => p != newAssetPath)
            .OrderBy(p => p, System.StringComparer.Ordinal);

        foreach (var path in candidates)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.importSettingsMissing)
            {
                return importer;
            }
        }

        return null;
    }

    private static void CopyImportSettings(TextureImporter from, TextureImporter to)
    {
        to.textureType = from.textureType;
        to.spriteImportMode = from.spriteImportMode;
        to.spritePixelsPerUnit = from.spritePixelsPerUnit;
        to.alphaIsTransparency = from.alphaIsTransparency;
        to.mipmapEnabled = from.mipmapEnabled;
        to.filterMode = from.filterMode;
        to.textureCompression = from.textureCompression;
        to.wrapMode = from.wrapMode;

        // 피벗은 TextureImporterSettings 를 거쳐야 alignment 까지 같이 간다.
        // spritePivot 만 건드리면 alignment 가 Center 로 남아 아무 효과가 없다.
        var source = new TextureImporterSettings();
        from.ReadTextureSettings(source);

        var target = new TextureImporterSettings();
        to.ReadTextureSettings(target);
        target.spriteAlignment = source.spriteAlignment;
        target.spritePivot = source.spritePivot;
        to.SetTextureSettings(target);
    }

    /// <summary>이미 어긋난 채로 만들어진 그림을 폴더 단위로 맞춘다.
    /// 스크립트가 생기기 전에 추가한 파일이나, 설정을 잘못 만진 파일의 구제용이다.
    /// 각 폴더에서 **파일명 순으로 첫 번째** 그림을 기준으로 삼는다 - 학생이 추가하는 것은 대개
    /// 뒤 번호(03, 04...)라 01 이 기준이 된다.</summary>
    [MenuItem("Tools/Class Template/Fix Sprite Import Settings In StudentReplace")]
    public static void FixMismatchedSprites()
    {
        var byFolder = new Dictionary<string, List<string>>();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { StudentArtRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var folder = Path.GetDirectoryName(path)?.Replace(Path.DirectorySeparatorChar, '/');
            if (string.IsNullOrEmpty(folder))
            {
                continue;
            }

            if (!byFolder.TryGetValue(folder, out var list))
            {
                list = new List<string>();
                byFolder[folder] = list;
            }

            list.Add(path);
        }

        var fixedPaths = new List<string>();

        foreach (var kvp in byFolder)
        {
            var paths = kvp.Value.OrderBy(p => p, System.StringComparer.Ordinal).ToList();
            if (paths.Count < 2)
            {
                continue; // 기준으로 삼을 것이 없다
            }

            var reference = AssetImporter.GetAtPath(paths[0]) as TextureImporter;
            if (reference == null)
            {
                continue;
            }

            var referenceSettings = new TextureImporterSettings();
            reference.ReadTextureSettings(referenceSettings);

            for (var i = 1; i < paths.Count; i++)
            {
                var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                var matches =
                    settings.spriteAlignment == referenceSettings.spriteAlignment &&
                    settings.spritePivot == referenceSettings.spritePivot &&
                    Mathf.Approximately(importer.spritePixelsPerUnit, reference.spritePixelsPerUnit);

                if (matches)
                {
                    continue;
                }

                CopyImportSettings(reference, importer);
                importer.SaveAndReimport();
                fixedPaths.Add($"{paths[i]}  (기준: {Path.GetFileName(paths[0])})");
            }
        }

        if (fixedPaths.Count == 0)
        {
            Debug.Log("그림 임포트 설정 - 고칠 것이 없습니다. 폴더마다 전부 같습니다.");
            return;
        }

        Debug.Log($"그림 임포트 설정을 맞췄습니다 ({fixedPaths.Count}개):\n - " + string.Join("\n - ", fixedPaths));
    }
}
