using GameProject.Core;
using GameProject.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameProject.UI
{
    /// <summary>
    /// Esc 키 또는 톱니바퀴 버튼으로 여는 일시정지 메뉴. `Time.timeScale = 0`으로 실제로 게임을
    /// 멈춘다 - `GameplayFreezeGate`(스테이지 클리어용, 진행 중이던 동작을 끊고 IDLE로 스냅)와
    /// 달리 진행 중이던 애니메이션/투사체가 그 상태 그대로 유지되다가 재개하면 이어서 진행된다.
    /// 일시정지는 다시 게임으로 돌아와야 하니 이쪽이 더 자연스럽다는 사용자 확정(2026-08-12).
    ///
    /// 사용자 확정 규칙:
    ///  - 톱니바퀴는 "열기" 전용. ESC는 토글(열려 있으면 계속하기와 동일하게 닫음).
    ///  - 사망 화면이 떠 있거나(`PlayerActionTestController.IsDead`) 스테이지 클리어가 발동된
    ///    상태(`GameplayFreezeGate.IsFrozen`)면 Esc/톱니바퀴 둘 다 무시 - 이미 게임이 끝난 상태.
    ///  - ActionTest/BackgroundTest 둘 다 적용.
    ///
    /// DeathScreenController/StageClearController와 같은 이유로 컨트롤러(항상 켜짐)와
    /// 패널(평소 꺼짐)을 분리 - 패널이 꺼진 오브젝트에 이 스크립트가 같이 있으면 Update()가 안
    /// 돌아서 Esc 감시 자체가 멈춰버림.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject controlsPopup;
        [SerializeField] private GameObject settingsPopup;
        [Tooltip("조작설명/설정 팝업이 열려 있는 동안 뒤의 6개 버튼 클릭을 막는 전체 화면 투명 패널.")]
        [SerializeField] private GameObject popupModalBlocker;
        [SerializeField] private PlayerActionTestController playerController;

        private void Update()
        {
            RefreshPlayerControllerReference();
            SyncPopupBlocker();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsOpen())
                {
                    Resume();
                }
                else if (CanOpen())
                {
                    Open();
                }
            }
        }

        private bool IsOpen()
        {
            return pausePanel != null && pausePanel.activeSelf;
        }

        private bool CanOpen()
        {
            return playerController != null && !playerController.IsDead && !GameplayFreezeGate.IsFrozen;
        }

        /// <summary>PlayerHpDisplay 등과 같은 이유(2026-08-11) - ActionTest는 Play 중 별도 런타임
        /// 복제본을 새로 만들고 원본은 비활성화하므로, 활성 상태인 진짜 플레이어를 매 프레임 다시
        /// 찾는다.</summary>
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

        /// <summary>조작설명/설정 팝업이 자기 자신의 닫기(X)/저장 버튼으로 닫혀도(SettingsPopupController
        /// 는 menuController가 없으면 그냥 자기 자신을 SetActive(false)) 차단막이 항상 정확한 상태를
        /// 유지하도록, 매 프레임 팝업 활성 여부로부터 다시 계산한다 - "닫을 때 차단막도 같이 끄기"를
        /// 여러 닫기 경로마다 중복해서 챙길 필요가 없다.</summary>
        private void SyncPopupBlocker()
        {
            if (popupModalBlocker == null)
            {
                return;
            }

            var anyPopupOpen = (controlsPopup != null && controlsPopup.activeSelf) ||
                                (settingsPopup != null && settingsPopup.activeSelf);
            if (popupModalBlocker.activeSelf != anyPopupOpen)
            {
                popupModalBlocker.SetActive(anyPopupOpen);
            }
        }

        public void OnGearClicked()
        {
            if (!IsOpen() && CanOpen())
            {
                Open();
            }
        }

        public void OnResumeClicked()
        {
            Resume();
        }

        public void OnRetryClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnTitleClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }

        public void OnQuitClicked()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            Debug.Log("종료 버튼 클릭 - 에디터에서는 실제로 종료되지 않음 (빌드에서는 게임이 닫힘).");
#else
            Application.Quit();
#endif
        }

        public void OnControlsClicked()
        {
            OpenSubPopup(controlsPopup);
        }

        public void OnSettingsClicked()
        {
            OpenSubPopup(settingsPopup);
        }

        /// <summary>조작설명 팝업 전용 닫기(X) - 설정 팝업은 SettingsPopupController가 스스로 닫는다.</summary>
        public void OnCloseControlsPopupClicked()
        {
            if (controlsPopup != null)
            {
                controlsPopup.SetActive(false);
            }
        }

        private void OpenSubPopup(GameObject popup)
        {
            CloseSubPopups();
            if (popup != null)
            {
                popup.SetActive(true);
            }
        }

        private void Open()
        {
            CloseSubPopups();
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private void Resume()
        {
            CloseSubPopups();
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private void CloseSubPopups()
        {
            if (controlsPopup != null)
            {
                controlsPopup.SetActive(false);
            }

            if (settingsPopup != null)
            {
                settingsPopup.SetActive(false);
            }
        }
    }
}
