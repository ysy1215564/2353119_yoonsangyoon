using System.Collections;
using UnityEngine;

namespace GameProject.UI
{
    /// <summary>
    /// 스테이지(BackgroundTest) 시작 시 화면 상단 중앙에 "GAME START" 배너를 잠깐 띄웠다가
    /// 자동으로 사라지는 단순 연출. 별도 페이드 없이 즉시 나타났다 즉시 사라짐(2026-08-12).
    /// 배너 오브젝트 자기 자신에 붙어서 스스로 껐다 켜는 방식이라, DeathScreenController/
    /// StageClearController처럼 "항상 켜진 감시용 오브젝트"를 따로 둘 필요가 없다 - 한 번 켜지고
    /// 한 번 꺼지는 게 다라서 다시 지켜볼 상태가 없음.
    /// </summary>
    public sealed class GameStartBannerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float visibleDuration = 2f;

        private void Awake()
        {
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(visibleDuration);
            gameObject.SetActive(false);
        }
    }
}
