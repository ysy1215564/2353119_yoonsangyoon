using UnityEngine;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// CanvasScaler.matchWidthOrHeight을 실행 시점의 실제 화면 비율을 보고 자동으로 고른다 - Title/
    /// IntroCutscene/EndingCutscene 세 씬 모두 배경/영상이 스트레치 없이 1920x1080 고정 크기로 캔버스
    /// 중앙에 놓이는 방식이라, 화면 비율에 따라 필요한 매치 축이 반대라서 상수 하나로는 모든 기기를
    /// 동시에 만족시킬 수 없다.
    ///
    /// - 2026-08-13: matchWidthOrHeight을 0.5 -> 1(세로 기준 고정)로 바꿔서 16:9보다 "넓은" 화면
    ///   (Galaxy S21 등 폰 landscape, 약 20:9)에서의 상하 크롭을 해결.
    /// - 2026-08-17: 그 고정값이 반대로 16:9보다 "좁은" 화면(Galaxy Tab S10 등 태블릿, 약 16:10)에서는
    ///   좌우가 잘리는 걸 실기기로 확인 - 화면마다 필요한 축이 반대라 상수로는 둘 다 해결 불가능,
    ///   실행 시점에 실제 화면 비율을 보고 선택하도록 이 컴포넌트를 도입.
    ///
    /// 규칙: 화면이 기준 비율(1920x1080, 16:9)보다 넓으면 세로 기준(1), 좁으면 가로 기준(0) - 항상
    /// "더 작게 맞춰지는 축"을 선택해서 배경 박스가 화면 밖으로 못 나가게 한다(= 크롭 없음). 남는
    /// 여백은 카메라가 이미 검정(SolidColor)이라 자동으로 검정 레터박스/필러박스가 된다. 버튼/팝업 등
    /// 나머지 UI는 앵커 기반 배치라 이 값이 바뀌어도 배치 로직 자체는 그대로 유지된다.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasContainScaler : MonoBehaviour
    {
        private void Awake()
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || Screen.height <= 0)
            {
                return;
            }

            var referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            var screenAspect = (float)Screen.width / Screen.height;
            scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;
        }
    }
}
