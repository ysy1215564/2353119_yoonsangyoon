using GameProject.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameProject.Mobile
{
    /// <summary>
    /// 일반 공격 전용 버튼(2026-08-15) - 원래는 이 버튼 하나로 탭=공격/홀드=차징을 겸했는데, 실기기
    /// 테스트에서 둘을 구분하려고 문턱 시간(holdThreshold)만큼 판정을 미루다 보니 반응이 느리고
    /// 조작이 갑갑하다는 피드백이 나와(사용자 확정, 2026-08-15) 차징 쪽을 MobileSpecialChargeButton으로
    /// 완전히 분리했다. 이제 이 버튼은 탭/홀드를 구분할 필요가 없어졌으므로 MobileDashButton/
    /// MobileTouchButton과 동일하게 "누르는 즉시(Down) 발동" 방식으로 바꿈 - PC의 J키/좌클릭
    /// (Input.GetKeyDown/GetMouseButtonDown, 둘 다 누르는 순간 발동)과도 동작이 일치하고, 예전처럼
    /// 뗄 때까지 기다릴 이유가 없어 반응 속도가 더 빠르다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class MobileAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Image targetImage;
        [SerializeField] private PlayerActionTestController playerController;

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
                playerController.TriggerAttackTap();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        /// <summary>MobileSpecialChargeButton/MobileDashButton/MobileJoystick과 같은 이유 - 매 프레임
        /// 살아있는 진짜 플레이어로 다시 찾는다(ActionTest의 _Runtime 클론 패턴, 2026-08-13 실사용
        /// 중 발견).</summary>
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
