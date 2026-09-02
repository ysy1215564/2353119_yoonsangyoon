using GameProject.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameProject.Mobile
{
    /// <summary>
    /// 탭 한 번으로 즉시 발동하는 단순 모바일 버튼(점프용) - 노멀/눌림 2장 스프라이트만 교체.
    /// 누르는 즉시(Down) 발동해야 반응이 빠르게 느껴져서, 떼는 시점이 아니라 누르는 시점에 발동한다
    /// (공격 버튼처럼 탭/홀드를 구분해야 하는 경우는 MobileAttackButton 참고 - 점프는 그럴 필요가
    /// 없어서 더 단순하게 만듦).
    ///
    /// 2026-08-13 수정: 원래 에디터 시점에 UnityEvent persistent listener로 특정 Player 인스턴스에
    /// 고정 연결하는 방식이었는데, ActionTest의 Player_ActionTest_Runtime 복제 패턴 때문에 Play
    /// 시작하면 그 연결이 이미 비활성화된 원본을 계속 붙잡고 있어서 아무 반응이 없던 실제 버그가
    /// 있었음 - UnityEvent persistent listener는 대상이 씬 데이터에 고정 저장돼서 런타임에 다시
    /// 못 바꾼다. `playerController`를 직접 들고 매 프레임 재해결하는 방식(MobileAttackButton과
    /// 동일)으로 바꿈. `onPress`는 그 외 부가 효과(사운드 등)를 걸고 싶을 때 쓰는 보조 훅으로 유지.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Image targetImage;
        [SerializeField] private PlayerActionTestController playerController;
        [Tooltip("점프 트리거 외에 추가로 걸고 싶은 효과(사운드 등)가 있으면 여기 연결 - 점프 자체는 이 이벤트와 무관하게 항상 직접 호출됨.")]
        public UnityEvent onPress = new UnityEvent();

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        private void Update()
        {
            RefreshPlayerControllerReference();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetImage != null && pressedSprite != null)
            {
                targetImage.sprite = pressedSprite;
            }

            if (playerController != null)
            {
                playerController.TriggerJumpInput();
            }

            onPress.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        /// <summary>MobileAttackButton/MobileJoystick과 같은 이유 - 매 프레임 살아있는 진짜
        /// 플레이어로 다시 찾는다(2026-08-13 실사용 중 발견).</summary>
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
    }
}
