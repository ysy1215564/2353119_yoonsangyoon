using UnityEngine;

namespace GameProject.Mobile
{
    /// <summary>
    /// 터치 컨트롤(조그셔틀+점프+공격)을 실제 모바일 기기에서만 자동으로 보이게 하고, PC 빌드에서는
    /// 자동으로 숨긴다(사용자 확정, 2026-08-13 - "기기가 모바일이면 자동으로 뜨고 PC에선 자동으로
    /// 안 뜨게"). 감지는 `Application.isMobilePlatform`(Android/iOS 빌드에서만 true) 기준.
    ///
    /// 에디터 Play 모드에서는 `Application.isMobilePlatform`이 항상 false라(에디터는 데스크톱
    /// 앱이라서, Build 플랫폼을 Android로 바꿔놔도 마찬가지) - previewInEditor를 켜두면 에디터
    /// 안에서 마우스 클릭으로 터치 시뮬레이션하며 바로 테스트할 수 있다(매번 안드로이드로 빌드 안
    /// 해도 됨). 빌드에는 전혀 영향 없음(#if UNITY_EDITOR로 완전히 제외).
    /// </summary>
    public sealed class MobileControlsVisibility : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("에디터에서 테스트할 때 플랫폼과 무관하게 항상 터치 컨트롤을 보이게 함 - 빌드에는 영향 없음(에디터 전용 필드).")]
        [SerializeField] private bool previewInEditor = true;
#endif

        private void Awake()
        {
            var shouldShow = Application.isMobilePlatform;
#if UNITY_EDITOR
            shouldShow |= previewInEditor;
#endif
            gameObject.SetActive(shouldShow);
        }
    }
}
