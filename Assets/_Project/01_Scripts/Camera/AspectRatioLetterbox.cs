using UnityEngine;

namespace GameProject.Cameras
{
    /// <summary>
    /// PC 기준(기본 16:9, 1920x1080)으로 만든 화면 비율을 다른 화면 비율(특히 모바일)에서도 그대로
    /// 유지하고, 남는 영역은 검게 비워둔다(레터박스/필러박스) - 2026-08-13 모바일 대응 확정 사항
    /// ("화면 비율은 PC 고정비율로 간다, 여백은 인정").
    ///
    /// 카메라의 Viewport Rect만 줄이는 방식이고, 그 밖으로 남는 여백은 자식으로 자동 생성하는
    /// 전용 배경 클리어 카메라(LetterboxBackdrop, EnsureLetterboxBackdrop 참고)가 매 프레임 검게
    /// 지워준다 - 씬 전환 직후 여백에 이전 화면이 잔상처럼 남는 문제를 막기 위함.
    /// PC에서 창 비율이 이미 16:9에 가까우면 Rect가 거의 (0,0,1,1) 그대로라 사실상 아무 영향 없음 -
    /// 그래서 PC/모바일 씬 구분 없이 아무 카메라에나 붙여도 안전하다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AspectRatioLetterbox : MonoBehaviour
    {
        [Tooltip("맞출 기준 비율(가로/세로) - 기본값은 이 프로젝트의 PC 디자인 해상도(1920x1080).")]
        [SerializeField] private float targetAspect = 1920f / 1080f;

        private Camera targetCamera;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            EnsureLetterboxBackdrop();
            Apply();
        }

        /// <summary>레터박스/필러박스로 카메라 Viewport Rect를 줄이면, 그 Rect 밖은 "어떤 카메라도
        /// 안 그리는 영역"이라 원래는 항상 검게 보여야 하는데 - 유니티의 카메라 Clear는 자기 Rect
        /// 안쪽만 지우기 때문에, 그 자리에 이전 프레임(심하면 이전 씬)이 마지막으로 그렸던 픽셀이
        /// 지워지지 않고 그대로 남아있을 수 있다. 씬 전환 직후 그 여백에 방금까지 보이던 이전 씬
        /// 화면 일부가 "유령 잔상"처럼 계속 남아 보이는 실기기 버그로 확인됨(2026-08-13, Galaxy
        /// S21 - BackgroundTest 진입 직후 여백에 IntroCutscene 화면이 계속 비쳐 보임). 아무것도 안
        /// 그리고 화면 전체를 검게 지우기만 하는 카메라를 이 카메라보다 먼저(depth 낮게) 그리게
        /// 해서 근본적으로 막는다 - 자식 오브젝트라 씬이 바뀌면 이 카메라와 함께 자동으로 사라짐.</summary>
        private void EnsureLetterboxBackdrop()
        {
            if (transform.Find("LetterboxBackdrop") != null)
            {
                return;
            }

            var backdrop = new GameObject("LetterboxBackdrop", typeof(Camera));
            backdrop.transform.SetParent(transform, false);
            var backdropCamera = backdrop.GetComponent<Camera>();
            backdropCamera.clearFlags = CameraClearFlags.SolidColor;
            backdropCamera.backgroundColor = Color.black;
            backdropCamera.cullingMask = 0;
            backdropCamera.rect = new Rect(0f, 0f, 1f, 1f);
            backdropCamera.depth = targetCamera.depth - 1f;
            backdropCamera.orthographic = true;
            backdropCamera.orthographicSize = 1f;
            backdropCamera.nearClipPlane = 0.01f;
            backdropCamera.farClipPlane = 1f;
        }

        private void Update()
        {
            // 화면 크기가 실제로 바뀐 프레임에만 다시 계산 - 매 프레임 Rect를 새로 대입할 필요는
            // 없지만, 모바일 회전/PC 창 크기조절에 대응하려면 폴링이 제일 간단하고 확실하다.
            if (Screen.width == lastWidth && Screen.height == lastHeight)
            {
                return;
            }

            Apply();
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (targetCamera == null || lastHeight == 0 || targetAspect <= 0f)
            {
                return;
            }

            var windowAspect = (float)lastWidth / lastHeight;
            var scaleHeight = windowAspect / targetAspect;
            var rect = targetCamera.rect;

            if (scaleHeight < 1f)
            {
                // 화면이 기준보다 좁고 김(세로 긴 폰 등) - 위아래를 검게(레터박스)
                rect.width = 1f;
                rect.height = scaleHeight;
                rect.x = 0f;
                rect.y = (1f - scaleHeight) / 2f;
            }
            else
            {
                // 화면이 기준보다 넓음(초광각 모니터 등) - 좌우를 검게(필러박스)
                var scaleWidth = 1f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1f;
                rect.x = (1f - scaleWidth) / 2f;
                rect.y = 0f;
            }

            targetCamera.rect = rect;
        }
    }
}
