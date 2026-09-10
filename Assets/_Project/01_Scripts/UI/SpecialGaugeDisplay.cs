using GameProject.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// Special-attack charge gauge - fillImage.fillAmount follows
    /// PlayerActionTestController.SpecialChargeProgress01 (0 = not charging, 1 = fully charged).
    /// The frame stays visible even at 0 so the gauge has a stable, always-present spot on screen
    /// (same reasoning as PlayerHpDisplay always showing its dots).
    /// </summary>
    public class SpecialGaugeDisplay : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private PlayerActionTestController playerController;

        private void Update()
        {
            RefreshPlayerControllerReference();

            if (fillImage != null)
            {
                fillImage.fillAmount = playerController != null ? playerController.SpecialChargeProgress01 : 0f;
            }
        }

        /// <summary>
        /// PlayerHpDisplay와 같은 이유(2026-08-11) - ActionTest는 Play 중 별도 런타임 복제본
        /// ("Player_ActionTest_Runtime")을 새로 만들고 에디터 시점 원본은 비활성화하므로, 활성 상태인
        /// 진짜 플레이어를 매 프레임 다시 찾는다(FindObjectOfType 기본값은 활성 오브젝트만 찾음).
        /// </summary>
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
