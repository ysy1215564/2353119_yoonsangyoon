using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GameProject.UI
{
    /// <summary>
    /// Loops a full-screen video as the title background, playing continuously regardless of
    /// buttons/popups until the whole Title scene unloads - same role as
    /// TitleBackgroundMotionPlayer, but for an actual video clip instead of a PNG flip-book.
    /// Mutually exclusive with TitleBackgroundMotionPlayer: ClassTemplateTitleSceneBuilder picks
    /// exactly one of the two per scene build, based on whether a video file exists in the title
    /// art folder ("파일 존재 여부가 곧 스위치", 사용자 확정 2026-08-14/15). 타이틀 배경은 그림
    /// 아니면 영상, 둘을 섞지 않는다(컷신과 다른 점 - 컷신은 슬라이드 단위 혼합 가능).
    ///
    /// Builds its own RenderTexture at runtime (1920x1080, the project's standard cutscene/title
    /// resolution - 사용자 확정 코덱/해상도 규칙과 동일) instead of requiring a pre-made
    /// RenderTexture asset to be wired by hand per scene.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class TitleBackgroundVideoPlayer : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;

        private RenderTexture renderTexture;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<RawImage>();
            }

            var videoPlayer = GetComponent<VideoPlayer>();

            renderTexture = new RenderTexture(1920, 1080, 0) { name = "TitleBackgroundVideoRT" };
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = true; // 사용자 확정(2026-08-15): 영상 끝나면 처음부터 반복 재생
            videoPlayer.playOnAwake = true;
            // 원본 영상이 정확히 1920x1080(16:9)이 아닐 경우를 대비 - FitInside는 비율을 유지한 채
            // RenderTexture 안에 전부 들어가도록 축소(레터박스)하지, 잘라내거나 늘리지 않는다
            // (2026-08-17, 학생이 실수/의도로 다른 비율 원본을 넣는 경우 대비해서 명시적으로 지정).
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

            if (targetImage != null)
            {
                targetImage.texture = renderTexture;
            }
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
        }
    }
}
