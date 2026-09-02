using GameProject.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameProject.Mobile
{
    /// <summary>
    /// 모바일 가상 조그셔틀(이동) - 2026-08-13 확정 사항.
    ///
    /// 이 스크립트는 실제로 터치를 받는 "감지 영역"(자신의 RectTransform, 눈에는 안 보이거나
    /// 아주 옅음)에 붙는다 - `frame`/`stick`은 화면 고정 위치에 있는 그림일 뿐이고, 감지 영역은
    /// 그보다 훨씬 넓게 잡아서 손가락이 그림 정중앙에 정확히 안 닿아도 반응한다(사용자 확정: "그 영역도
    /// 나중에 조절 가능하게"). 스틱은 `frame` 중심 기준으로 위치가 계산되므로, 넓은 영역 어디를
    /// 누르든 프레임은 고정된 자리에서 그 방향을 가리키게 움직인다.
    ///
    /// 2026-08-14: 플릭(빠르게 끝까지 밀었다 놓기) 대쉬 인식을 제거함 - 이동과 대쉬가 같은 조그셔틀
    /// 영역을 공유해서 여러 테스터가 "이동하려다 실수로 대쉬가 나간다"는 불편을 겪었음(사용자 확정).
    /// 대쉬는 이제 전용 버튼(MobileDashButton, 공격 버튼 좌측)으로 완전히 분리됨.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform frame;
        [SerializeField] private RectTransform stick;
        [SerializeField] private PlayerActionTestController playerController;
        [Tooltip("Canvas가 Screen Space - Overlay면 비워둘 것(null). Screen Space - Camera/World Space면 그 카메라를 지정.")]
        [SerializeField] private Camera uiCamera;

        [Header("Tuning (전부 나중에 조절 가능)")]
        [Tooltip("손잡이가 프레임 중심에서 최대로 벗어날 수 있는 거리(px, 레퍼런스 해상도 1920x1080 기준).")]
        [SerializeField, Min(1f)] private float maxStickDistance = 120f;
        [Tooltip("이 비율(0~1)보다 작은 기울임은 입력으로 안 침 - 손 떨림/오조작 방지.")]
        [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.15f;

        private int activePointerId = -1;

        private void Awake()
        {
            ResetStick();
        }

        private void Update()
        {
            RefreshPlayerControllerReference();
        }

        /// <summary>ActionTest는 Play 시작 시 에디터에 보이던 원본 Player_ActionTest를 비활성화하고
        /// Player_ActionTest_Runtime 복제본을 새로 만들어 그걸로 플레이한다(PlayerHpDisplay 등
        /// 이 프로젝트의 다른 HUD 스크립트들과 같은 이유, 2026-08-11 패턴) - 씬 만들 때 연결해둔
        /// 참조는 그 원본(이제 비활성화됨)을 그대로 붙잡고 있어서, 매 프레임 살아있는 진짜 플레이어로
        /// 다시 찾아준다. 이걸 빠뜨리면 터치를 줘도 죽은 오브젝트한테 신호를 보내는 꼴이라 아무 반응이
        /// 없다(2026-08-13 실사용 중 발견).</summary>
        private void RefreshPlayerControllerReference()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (playerController != null && playerController.isActiveAndEnabled)
            {
                return;
            }

            playerController = Object.FindObjectOfType<PlayerActionTestController>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            activePointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            activePointerId = -1;
            ResetStick();

            if (playerController != null)
            {
                playerController.SetTouchMoveInput(0f);
            }
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (frame == null || stick == null)
            {
                return;
            }

            // frame 중심 기준 로컬 좌표로 변환 - 실제로 어디를 눌렀든(넓은 감지 영역 안 아무 데나)
            // 스틱은 항상 고정된 frame을 기준으로 방향을 계산한다.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, eventData.position, uiCamera, out var localPoint);
            var clamped = Vector2.ClampMagnitude(localPoint, maxStickDistance);
            stick.anchoredPosition = clamped;

            var normalized = clamped / maxStickDistance;
            var horizontal = Mathf.Abs(normalized.x) >= deadZone ? normalized.x : 0f;

            if (playerController != null)
            {
                playerController.SetTouchMoveInput(horizontal);
            }
        }

        private void ResetStick()
        {
            if (stick != null)
            {
                stick.anchoredPosition = Vector2.zero;
            }
        }
    }
}
