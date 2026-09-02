using System.Collections;
using GameProject.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameProject.UI
{
    /// <summary>
    /// Shows the death screen (deathcut.png + 재도전/타이틀화면으로/종료) after the player's death
    /// animation (deathFrames) has fully played out, plus a short extra delay - covers both HP-based
    /// death and fall death, since both already funnel through the same HandleDeath() on the player side.
    ///
    /// 사용자 확정(2026-08-11): 사망 화면이 뜬 동안 월드(몬스터/BGM)는 그대로 계속 움직여도 됨 -
    /// Time.timeScale을 건드리지 않는다.
    /// 사용자 확정(2026-08-11): 죽음 모션이 즉시 끊기고 메뉴가 뜨면 너무 가벼워 보여서, 죽음 모션의
    /// 마지막 프레임까지 재생을 끝낸 뒤 delayAfterDeathAnimation만큼 더 기다렸다가 패널을 띄운다.
    /// 모션 프레임 수가 바뀌어도(학생 작업 중) PlayerActionTestController.IsDeathAnimationFinished가
    /// 실제 재생 완료 시점을 알려주므로 여기서 프레임 수를 따로 알 필요는 없다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class DeathScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private PlayerActionTestController playerController;
        [Tooltip("죽음 모션의 마지막 프레임까지 다 재생된 후, 사망 화면(이미지+메뉴)이 뜨기까지 추가로 기다리는 시간(초).")]
        [SerializeField, Min(0f)] private float delayAfterDeathAnimation = 1f;
        [Tooltip("사망 화면(이미지+메뉴)이 뜨는 순간 1번만 재생되는 사운드 (deathscreen_bgm 등).")]
        [SerializeField] private AudioSource stingerSource;
        [SerializeField] private AudioClip deathScreenStinger;

        private Coroutine showRoutine;

        private void Awake()
        {
            if (stingerSource == null)
            {
                stingerSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            RefreshPlayerControllerReference();

            if (playerController != null && playerController.IsDead && panel != null &&
                !panel.activeSelf && showRoutine == null)
            {
                showRoutine = StartCoroutine(ShowDeathScreenAfterDelay());
            }
        }

        private IEnumerator ShowDeathScreenAfterDelay()
        {
            while (playerController != null && !playerController.IsDeathAnimationFinished)
            {
                yield return null;
            }

            yield return new WaitForSeconds(delayAfterDeathAnimation);

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (stingerSource != null && deathScreenStinger != null)
            {
                stingerSource.PlayOneShot(deathScreenStinger);
            }

            showRoutine = null;
        }

        /// <summary>
        /// PlayerHpDisplay/SpecialGaugeDisplay와 같은 이유(2026-08-11) - ActionTest는 Play 중 별도
        /// 런타임 복제본을 새로 만들고 원본은 비활성화하므로, 활성 상태인 진짜 플레이어를 매 프레임
        /// 다시 찾는다. 참조가 새 인스턴스로 바뀌면(예: 몬스터 선택 버튼으로 테스트를 리셋) 사망 화면도
        /// 같이 초기화해서, 죽었다가 다시 테스트를 리셋했을 때 화면이 계속 떠 있지 않도록 한다.
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

            var found = Object.FindObjectOfType<PlayerActionTestController>();
            if (found != playerController)
            {
                playerController = found;
                if (panel != null)
                {
                    panel.SetActive(false);
                }

                if (showRoutine != null)
                {
                    StopCoroutine(showRoutine);
                    showRoutine = null;
                }
            }
        }

        public void OnRetryClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnTitleClicked()
        {
            SceneManager.LoadScene("Title");
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("종료 버튼 클릭 - 에디터에서는 실제로 종료되지 않음 (빌드에서는 게임이 닫힘).");
#else
            Application.Quit();
#endif
        }
    }
}
