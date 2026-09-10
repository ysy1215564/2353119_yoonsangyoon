using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lets you see a popup's real art in the Scene/Game view WITHOUT entering Play Mode, so dragging
/// the close hit-area / sliders / save button into place is a normal Edit Mode change that saves
/// with the scene (Ctrl+S) - unlike Play Mode, where anything moved gets thrown away the moment
/// you stop. Only enabled while Title.unity is the open scene.
/// </summary>
public static class TitleScenePreviewTools
{
    [MenuItem("Tools/Class Template/Title Preview/조작설명 팝업 토글")]
    public static void ToggleControlsPopup() => TogglePopup("ControlsPopup");

    [MenuItem("Tools/Class Template/Title Preview/조작설명 팝업 토글", true)]
    private static bool ValidateToggleControlsPopup() => IsTitleSceneActive();

    [MenuItem("Tools/Class Template/Title Preview/설정 팝업 토글")]
    public static void ToggleSettingsPopup() => TogglePopup("SettingsPopup");

    [MenuItem("Tools/Class Template/Title Preview/설정 팝업 토글", true)]
    private static bool ValidateToggleSettingsPopup() => IsTitleSceneActive();

    [MenuItem("Tools/Class Template/Title Preview/크레딧 팝업 토글")]
    public static void ToggleCreditsPopup() => TogglePopup("CreditsPopup");

    [MenuItem("Tools/Class Template/Title Preview/크레딧 팝업 토글", true)]
    private static bool ValidateToggleCreditsPopup() => IsTitleSceneActive();

    [MenuItem("Tools/Class Template/Title Preview/모두 닫기")]
    public static void CloseAllPopups()
    {
        SetActive("ControlsPopup", false);
        SetActive("SettingsPopup", false);
        SetActive("CreditsPopup", false);
        SetActive("ModalBlocker", false);

        // 2026-08-12: 이 메서드만 MarkSceneDirty가 빠져있었음 - 화면엔 바로 닫힌 걸로 보이지만
        // "저장 필요" 표시가 안 돼서, 눈에 보이는 대로 Ctrl+S를 눌러도 실제 파일엔 반영이 안 되는
        // 사고가 있었음(에디터를 껐다 켜면 팝업이 다시 열린 상태로 돌아옴).
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Class Template/Title Preview/모두 닫기", true)]
    private static bool ValidateCloseAllPopups() => IsTitleSceneActive();

    private const string TitleArtPath = "Assets/_Project/04_Art/StudentReplace/UI/Title";

    /// <summary>
    /// 손잡이(Handle)는 위치가 Slider 컴포넌트에 의해 자동 계산되는 값이라, Scene 뷰에서 실수로
    /// 옮기거나 크기를 바꿔버려도 이 명령으로 원래(스프라이트 원본 크기 + 현재 슬라이더 값에 맞는
    /// 위치)로 되돌릴 수 있다. BgmSlider/SfxSlider 둘 다 한 번에 처리.
    /// </summary>
    [MenuItem("Tools/Class Template/Title Preview/슬라이더 손잡이 원래대로")]
    public static void ResetSliderHandles()
    {
        var fixedBgm = ResetSliderHandle("BgmSlider");
        var fixedSfx = ResetSliderHandle("SfxSlider");

        if (!fixedBgm && !fixedSfx)
        {
            Debug.LogWarning("BgmSlider/SfxSlider를 찾을 수 없습니다. Title.unity를 열어서 실행하세요.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("슬라이더 손잡이 크기/위치를 원래 상태로 되돌렸습니다. Ctrl+S로 저장하세요.");
    }

    [MenuItem("Tools/Class Template/Title Preview/슬라이더 손잡이 원래대로", true)]
    private static bool ValidateResetSliderHandles() => IsTitleSceneActive();

    private static bool ResetSliderHandle(string sliderName)
    {
        var sliderObject = FindByName(sliderName);
        if (sliderObject == null)
        {
            return false;
        }

        var slider = sliderObject.GetComponent<Slider>();
        if (slider == null || slider.handleRect == null)
        {
            return false;
        }

        var handleRect = slider.handleRect;

        // 크기는 원본 손잡이 스프라이트 픽셀 크기로 고정.
        var handleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TitleArtPath}/slider_handle.png");
        if (handleSprite != null)
        {
            handleRect.sizeDelta = new Vector2(handleSprite.rect.width, handleSprite.rect.height);
        }

        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;

        // 위치(가로축 앵커)는 Slider.UpdateVisuals()가 하는 것과 똑같은 계산을 여기서 재현 -
        // slider.value를 그대로 대입하면 값이 안 바뀌었을 때 아무 일도 안 일어나는 문제가 있어서
        // (Slider.Set()의 "값이 같으면 그냥 return" 최적화), 직접 앵커를 계산해서 넣는다.
        var normalized = slider.normalizedValue;
        var anchorMin = new Vector2(0f, 0f);
        var anchorMax = new Vector2(1f, 1f);
        if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
        {
            anchorMin.x = anchorMax.x = normalized;
        }
        else
        {
            anchorMin.y = anchorMax.y = normalized;
        }

        handleRect.anchorMin = anchorMin;
        handleRect.anchorMax = anchorMax;

        return true;
    }

    private static void TogglePopup(string popupName)
    {
        var popup = FindByName(popupName);
        if (popup == null)
        {
            Debug.LogWarning($"'{popupName}' 오브젝트를 찾을 수 없습니다. Title.unity를 열어서 실행하세요.");
            return;
        }

        var next = !popup.activeSelf;

        // 다른 팝업이 켜져 있으면 같이 꺼서, 실제 게임처럼 한 번에 하나만 보이게 한다.
        if (next)
        {
            SetActive("ControlsPopup", false);
            SetActive("SettingsPopup", false);
            SetActive("CreditsPopup", false);
        }

        popup.SetActive(next);
        SetActive("ModalBlocker", next);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void SetActive(string name, bool active)
    {
        var obj = FindByName(name);
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// GameObject.Find only searches ACTIVE objects, so it can't ever find a popup once it's been
    /// switched off - which is exactly the state these are in most of the time. Walks every root
    /// object's full hierarchy (Resources.FindObjectsOfTypeAll would also see prefab assets/other
    /// scenes, so a plain recursive search of this scene's roots is safer) and matches by name,
    /// active or not.
    /// </summary>
    private static GameObject FindByName(string name)
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

    private static bool IsTitleSceneActive()
    {
        return SceneManager.GetActiveScene().name == "Title";
    }
}
