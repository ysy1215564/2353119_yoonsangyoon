using UnityEngine;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// Loops a full-screen flip-book animation over the static title_background.png (title_motion01,
    /// 02, 03 ... in order), independent of buttons/popups - keeps playing regardless of modal state,
    /// only stops because the whole Title scene unloads. Same "swap the Sprite on a timer" approach
    /// already used for player/monster frame animation elsewhere in this project - no Animator
    /// Controller needed for something this simple.
    ///
    /// Frame count is meant to grow/shrink freely (프레임 자유도) - the array is populated by
    /// ClassTemplateTitleSceneBuilder's frame scan, not hardcoded here, so adding/removing
    /// title_motion*.png files and rerunning "Refresh Title Motion Frames" is all it takes.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TitleBackgroundMotionPlayer : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("느긋한 느낌 기본값 5 - 숫자만 바꾸면 바로 반영됨, 코드 수정 필요 없음.")]
        [SerializeField] private float framesPerSecond = 5f;

        private int currentFrameIndex;
        private float timer;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            currentFrameIndex = 0;
            timer = 0f;
            ShowCurrentFrame();
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1 || framesPerSecond <= 0f)
            {
                return;
            }

            // unscaledDeltaTime: 타이틀 화면에 별도 일시정지 기능은 없지만, 배경 애니메이션은
            // "버튼/팝업과 무관하게 계속 재생"이 스펙이라 Time.timeScale에 영향받지 않는 게 맞다.
            var frameDuration = 1f / framesPerSecond;

            // 앱 최초 실행 직후(콜드 스타트)나 백그라운드에서 돌아온 직후는 셰이더 컴파일/텍스처
            // 로딩 등으로 첫 Update()의 Time.unscaledDeltaTime이 비정상적으로 크게(수백 ms) 찍힐 수
            // 있다 - 그걸 그대로 timer에 누적하면 "밀린 시간"이 생겨서, 이후 몇 번의 Update()가
            // 정상 프레임 간격인데도 매번 즉시 다음 프레임으로 넘어가며 밀린 걸 몰아서 처리하느라
            // 애니메이션이 순간적으로 빨리감기처럼 보인다(2026-08-13 실기기 확인 - 타이틀 최초 진입
            // 시에만 약 1초간 프레임이 파바박 튀다가 정상 속도로 안정됨). 한 번의 Update()가 timer에
            // 넣을 수 있는 최대량을 프레임 하나 길이로 제한해서, 아무리 큰 delta가 찍혀도 밀린 시간이
            // 쌓이지 않게 막는다.
            timer += Mathf.Min(Time.unscaledDeltaTime, frameDuration);
            if (timer < frameDuration)
            {
                return;
            }

            timer -= frameDuration;
            currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
            ShowCurrentFrame();
        }

        private void ShowCurrentFrame()
        {
            if (targetImage == null || frames == null || frames.Length == 0)
            {
                return;
            }

            targetImage.sprite = frames[currentFrameIndex];
        }
    }
}
