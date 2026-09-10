using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameProject.UI
{
    /// <summary>
    /// Drives the title screen: Start/Controls/Settings/Credits/Quit buttons, and the popup +
    /// modal-blocker behavior shared by all three popups.
    ///
    /// Modal rule (사용자 확정): while any popup is open, nothing else on screen is clickable -
    /// only that popup's own close control works. This is enforced by modalBlocker, a full-screen
    /// transparent Image with raycastTarget on, sitting above the five main buttons but below the
    /// popup itself in draw order - it simply eats every click aimed at anything behind it, no
    /// script logic needed for "which buttons are disabled".
    /// </summary>
    public class TitleMenuController : MonoBehaviour
    {
        [Tooltip("시작하기를 눌렀을 때 로드할 씬. 컷신 파트가 오기 전까지는 BackgroundTest로 임시 연결.")]
        [SerializeField] private string startSceneName = "BackgroundTest";

        [SerializeField] private GameObject controlsPopup;
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private GameObject creditsPopup;

        [Tooltip("팝업이 열려 있는 동안 다른 모든 버튼 클릭을 막는 전체 화면 투명 패널.")]
        [SerializeField] private GameObject modalBlocker;

        public void StartGame()
        {
            CloseAllPopups();

            if (string.IsNullOrEmpty(startSceneName))
            {
                Debug.LogWarning("TitleMenuController.startSceneName이 비어 있어 시작하기 버튼이 아무 씬도 로드하지 않습니다.");
                return;
            }

            SceneManager.LoadScene(startSceneName);
        }

        public void OpenControls() => OpenPopup(controlsPopup);

        public void OpenSettings() => OpenPopup(settingsPopup);

        public void OpenCredits() => OpenPopup(creditsPopup);

        /// <summary>Plain close for popups with no save/cancel distinction (조작설명, 크레딧).</summary>
        public void CloseCurrentPopup()
        {
            CloseAllPopups();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("종료 버튼 클릭 - 에디터에서는 실제로 종료되지 않음 (빌드에서는 게임이 닫힘).");
#else
            Application.Quit();
#endif
        }

        private void OpenPopup(GameObject popup)
        {
            if (popup == null)
            {
                return;
            }

            CloseAllPopups();
            popup.SetActive(true);
            SetBlocker(true);
        }

        private void CloseAllPopups()
        {
            SetActive(controlsPopup, false);
            SetActive(settingsPopup, false);
            SetActive(creditsPopup, false);
            SetBlocker(false);
        }

        private void SetBlocker(bool active) => SetActive(modalBlocker, active);

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
