using System.Collections;
using GameProject.Core;
using GameProject.Environment;
using GameProject.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameProject.UI
{
    /// <summary>
    /// 레벨클리어 오브젝트(LevelClearObjectController)가 맞고, 플레이어의 마지막 타격 동작이 끝나
    /// IDLE로 넘어가는 순간(IsActionLocked true→false) 클리어 화면을 띄운다.
    ///
    /// 사용자 확정(2026-08-12):
    ///  - 월드BGM만 정지(클리어 BGM과 안 겹치게), 그 외에는 Time.timeScale을 건드리지 않는다.
    ///  - 대신 GameplayFreezeGate를 켜서 플레이어/몬스터A/B/C를 전부 그 자리에서 IDLE로 고정하고
    ///    입력/AI/피격을 무시하게 한다 - 눈 내리는 WeatherFX 같은, 이 게이트를 안 보는 연출은 그대로
    ///    계속 움직인다. 학생들이 몬스터를 스테이지 끝에 무한 리스폰으로 배치해도, 클리어 화면이
    ///    뜬 상태에서 몬스터한테 맞아 사망화면이 겹쳐 뜨는 사고를 막기 위함.
    ///  - 클리어 화면은 일러스트 1장뿐, 버튼 없음. 클리어 사운드(1회) 재생이 끝나면 자동으로
    ///    EndingCutscene 씬으로 이동.
    ///
    /// DeathScreenController와 같은 이유로 컨트롤러(항상 켜짐)와 패널(평소 꺼짐)을 분리 - 패널이
    /// 꺼진 오브젝트에 이 스크립트가 같이 있으면 Update()가 안 돌아서 감시 자체가 멈춰버림.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class StageClearController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private PlayerActionTestController playerController;
        [SerializeField] private LevelClearObjectController levelClearObject;
        [Tooltip("클리어 순간 정지시킬 월드 BGM(BackgroundTest의 'BackgroundMusic' 오브젝트).")]
        [SerializeField] private AudioSource worldBgmSource;
        [Tooltip("클리어 화면이 뜨는 순간 1번 재생되는 사운드 (level_clear_bgm 등) - BGM 믹서 그룹 경유라 설정의 BGM 볼륨 슬라이더 영향을 받는다.")]
        [SerializeField] private AudioSource stingerSource;
        [SerializeField] private AudioClip clearStinger;
        [Tooltip("클리어 사운드 재생이 끝나면 자동으로 이동할 씬.")]
        [SerializeField] private string nextSceneName = "EndingCutscene";

        private bool triggered;

        private void Awake()
        {
            if (stingerSource == null)
            {
                stingerSource = GetComponent<AudioSource>();
            }
        }

        private void Update()
        {
            if (triggered)
            {
                return;
            }

            RefreshPlayerControllerReference();

            if (levelClearObject == null)
            {
                levelClearObject = Object.FindObjectOfType<LevelClearObjectController>();
            }

            if (playerController != null && levelClearObject != null && levelClearObject.WasHit && !playerController.IsActionLocked)
            {
                TriggerClear();
            }
        }

        /// <summary>PlayerHpDisplay/SpecialGaugeDisplay/DeathScreenController와 같은 이유(2026-08-11)
        /// - ActionTest는 Play 중 별도 런타임 복제본을 새로 만들고 원본은 비활성화하므로, 활성 상태인
        /// 진짜 플레이어를 매 프레임 다시 찾는다. BackgroundTest에는 이런 복제 구조가 없지만, 같은
        /// 안전장치를 그대로 맞춰둔다.</summary>
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

        private void TriggerClear()
        {
            triggered = true;
            GameplayFreezeGate.Freeze();
            StopWorldBgm();

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (stingerSource != null && clearStinger != null)
            {
                stingerSource.PlayOneShot(clearStinger);
            }

            StartCoroutine(WaitForStingerThenLoadEndingScene());
        }

        private void StopWorldBgm()
        {
            if (worldBgmSource == null)
            {
                var bgmObject = GameObject.Find("BackgroundMusic");
                if (bgmObject != null)
                {
                    worldBgmSource = bgmObject.GetComponent<AudioSource>();
                }
            }

            if (worldBgmSource != null)
            {
                worldBgmSource.Stop();
            }
        }

        /// <summary>오디오 재생은 Time.timeScale의 영향을 받지 않으므로(이 화면은 timeScale을 아예
        /// 안 건드림) 실제 재생 시간 그대로 기다리면 된다. 클립이 없으면 무한 대기하지 않고 바로
        /// 다음 씬으로 넘어간다.</summary>
        private IEnumerator WaitForStingerThenLoadEndingScene()
        {
            if (stingerSource != null && clearStinger != null)
            {
                yield return null; // PlayOneShot이 실제로 시작될 시간을 한 프레임 준다
                while (stingerSource != null && stingerSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning("StageClearController: 클리어 사운드 클립이 없어 바로 다음 씬으로 이동합니다.");
            }

            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("StageClearController.nextSceneName이 비어 있어 이동하지 않습니다.");
                yield break;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
