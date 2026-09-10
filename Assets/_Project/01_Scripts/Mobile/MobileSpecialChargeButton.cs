using GameProject.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameProject.Mobile
{
    /// <summary>
    /// 스페셜(차징) 공격 전용 버튼(2026-08-15) - 원래 MobileAttackButton 하나가 탭=일반공격/홀드=차징을
    /// 겸했는데, 실기기에서 탭/홀드 문턱 시간 때문에 판정이 늦고 조작이 갑갑하다는 피드백이 나와
    /// (사용자 확정) 별도 버튼으로 분리했다. 이 버튼은 탭/홀드 구분이 필요 없어 MobileDashButton과
    /// 동일하게 단순함: 누르면 차징 시작, 떼면 그 즉시 발사 판정(키보드 K키/마우스 우클릭과 완전히
    /// 같은 코드 경로 - TriggerSpecialChargeStart/SetTouchSpecialChargeHeld, 최소 충전 시간 체크 등은
    /// 전부 PlayerActionTestController 쪽 기존 로직이 그대로 처리).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class MobileSpecialChargeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
                playerController.TriggerSpecialChargeStart();
                playerController.SetTouchSpecialChargeHeld(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }

            if (playerController != null)
            {
                // 실제 발사 여부/타이밍은 PlayerActionTestController.Update()의 기존 차징 감시
                // 로직이 이 값을 보고 다음 프레임에 알아서 처리한다(TryFireSpecial 등 전부 재사용).
                playerController.SetTouchSpecialChargeHeld(false);
            }
        }

        /// <summary>MobileAttackButton/MobileDashButton/MobileJoystick과 같은 이유 - 매 프레임
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
