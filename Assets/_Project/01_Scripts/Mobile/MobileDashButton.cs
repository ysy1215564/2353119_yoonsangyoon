using GameProject.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameProject.Mobile
{
    /// <summary>
    /// 탭 한 번으로 즉시 발동하는 전용 대쉬 버튼(2026-08-14) - 조그셔틀 플릭(스와이프) 대쉬 대신
    /// 도입. 이동과 대쉬가 같은 조그셔틀 영역을 공유하다 보니 여러 테스터가 "이동하려다 실수로
    /// 대쉬가 나간다"는 불편을 겪어서(사용자 확정), 완전히 분리된 버튼으로 옮김. 방향은 스와이프
    /// 방향이 아니라 항상 "지금 캐릭터가 바라보는 방향"으로 고정 - 키보드 LeftShift와 완전히
    /// 동일한 동작(TriggerDashInput()을 방향 인자 없이 호출하면 PlayerActionTestController가
    /// 알아서 현재 facing을 그대로 사용함).
    ///
    /// 구조는 MobileTouchButton(점프 버튼)과 동일 - 탭/홀드 구분이 필요 없는 단순 즉시발동 버튼이라
    /// 그대로 재사용 가능했지만, 점프 전용으로 하드코딩돼있어서 별도 클래스로 분리(기존 코드를
    /// 건드리지 않기 위함).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class MobileDashButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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
                playerController.TriggerDashInput();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        /// <summary>MobileAttackButton/MobileTouchButton/MobileJoystick과 같은 이유 - 매 프레임
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
