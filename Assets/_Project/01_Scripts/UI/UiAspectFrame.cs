using System.Collections.Generic;
using GameProject.Mobile;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// 화면 UI(HUD, 버튼, 팝업, 컷신 버튼 등)를 게임 화면과 같은 16:9(1920x1080) 틀 안에 넣는다.
    ///
    /// 배경: 카메라는 AspectRatioLetterbox 가 16:9 로 묶어서 폰(약 20:9)에서는 좌우가 검게 남는데,
    /// UI 캔버스(Screen Space - Overlay)는 폰 화면 전체 기준이라 모서리에 붙은 버튼이 검은 여백
    /// 쪽으로 밀려났다(2026-11-05 N반 1차 빌드 때 학생 폰에서 확인). PC(16:9)에서는 차이가 없다.
    ///
    /// 방식: 씬을 고치지 않고, 실행 중에 씬이 열릴 때마다 자동으로 처리한다.
    ///  1) 화면용 루트 캔버스마다 그 바로 아래 UI 를 16:9 틀(UiAspectFrame) 안으로 옮긴다.
    ///     모서리에 붙은 UI 는 틀의 모서리 = 게임 화면의 모서리에 붙게 된다.
    ///  2) 화면 전체를 덮는 판(앵커가 0,0~1,1 로 꽉 찬 것 - 일시정지/사망 화면의 어두운 바탕,
    ///     팝업 뒤 클릭 막이 등)은 그대로 화면 전체를 덮게 두고, 그 안의 내용만 틀에 넣는다.
    ///  3) 그리는 순서(위/아래)는 그대로 유지한다 - 연속된 것끼리만 한 틀로 묶는다.
    ///  4) 캔버스 맞춤값(Match)을 화면 비율에 따라 고른다(CanvasContainScaler 와 같은 규칙) -
    ///     16:9 틀이 PC 와 똑같은 1920x1080 크기가 되어 UI 크기 비율도 PC 와 같아진다.
    ///
    /// 제외: 모바일 조작(MobileControlsCanvas - 조이스틱, 점프/공격/대시/특수공격 버튼)은 손가락이
    /// 닿기 편하도록 지금처럼 폰 화면 전체 기준으로 둔다(화면 밖 여백으로 나가는 것 인정, 2026-11-05
    /// 확정). 월드 공간 캔버스와 다른 캔버스 안에 들어 있는 캔버스도 건드리지 않는다.
    /// </summary>
    public sealed class UiAspectFrame : MonoBehaviour
    {
        private const float TargetAspect = 1920f / 1080f;
        private const string FrameName = "UiAspectFrame";
        private const int MaxDepth = 4;

        private readonly List<RectTransform> frames = new List<RectTransform>();
        private CanvasScaler scaler;
        private int lastWidth;
        private int lastHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (ShouldFrame(canvas))
                    {
                        canvas.gameObject.AddComponent<UiAspectFrame>().Build();
                    }
                }
            }
        }

        private static bool ShouldFrame(Canvas canvas)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                return false;
            }

            // 다른 캔버스 안에 들어 있는 캔버스는 바깥 캔버스를 처리할 때 같이 옮겨진다.
            var parent = canvas.transform.parent;
            if (parent != null && parent.GetComponentInParent<Canvas>(true) != null)
            {
                return false;
            }

            return canvas.GetComponent<MobileControlsVisibility>() == null
                && canvas.GetComponent<UiAspectFrame>() == null;
        }

        private void Build()
        {
            scaler = GetComponent<CanvasScaler>();
            WrapChildren((RectTransform)transform, 0);
            Apply();
        }

        /// <summary>parent 바로 아래 자식 중 화면 전체를 덮는 판은 그대로 두고(안쪽만 다시 처리),
        /// 나머지는 연속된 것끼리 틀 하나로 묶는다.</summary>
        private void WrapChildren(RectTransform parent, int depth)
        {
            var children = new List<RectTransform>();
            foreach (Transform child in parent)
            {
                if (child is RectTransform rect)
                {
                    children.Add(rect);
                }
            }

            RectTransform currentFrame = null;
            foreach (var child in children)
            {
                if (IsFullScreen(child) && depth < MaxDepth)
                {
                    currentFrame = null;
                    WrapChildren(child, depth + 1);
                    continue;
                }

                if (currentFrame == null)
                {
                    currentFrame = CreateFrame(parent, child.GetSiblingIndex());
                }

                child.SetParent(currentFrame, false);
            }
        }

        private static bool IsFullScreen(RectTransform rect)
        {
            return rect.anchorMin == Vector2.zero
                && rect.anchorMax == Vector2.one
                && rect.offsetMin.sqrMagnitude < 1f
                && rect.offsetMax.sqrMagnitude < 1f
                && rect.localScale == Vector3.one;
        }

        private RectTransform CreateFrame(RectTransform parent, int siblingIndex)
        {
            var frameObject = new GameObject(FrameName, typeof(RectTransform));
            frameObject.layer = parent.gameObject.layer;
            var frame = (RectTransform)frameObject.transform;
            frame.SetParent(parent, false);
            frame.SetSiblingIndex(siblingIndex);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frames.Add(frame);
            return frame;
        }

        private void Update()
        {
            // 폰 회전, PC 창 크기 조절에 대응 - 화면 크기가 바뀐 프레임에만 다시 계산.
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (lastWidth <= 0 || lastHeight <= 0)
            {
                return;
            }

            var screenAspect = (float)lastWidth / lastHeight;
            var min = Vector2.zero;
            var max = Vector2.one;
            if (screenAspect > TargetAspect)
            {
                // 화면이 16:9 보다 넓음(폰 가로) - 좌우를 비운다.
                var width = TargetAspect / screenAspect;
                min.x = (1f - width) / 2f;
                max.x = 1f - min.x;
            }
            else
            {
                // 화면이 16:9 보다 좁음(태블릿 16:10 등) - 위아래를 비운다.
                var height = screenAspect / TargetAspect;
                min.y = (1f - height) / 2f;
                max.y = 1f - min.y;
            }

            // 틀이 들어간 판들은 모두 화면 전체를 덮으므로 같은 앵커 값을 쓰면 된다.
            foreach (var frame in frames)
            {
                if (frame == null)
                {
                    continue;
                }

                frame.anchorMin = min;
                frame.anchorMax = max;
                frame.offsetMin = Vector2.zero;
                frame.offsetMax = Vector2.zero;
            }

            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.matchWidthOrHeight = screenAspect >= TargetAspect ? 1f : 0f;
            }
        }
    }
}
