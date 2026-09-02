using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GameProject.UI
{
    /// <summary>
    /// Drives the intro/ending cutscene: a full-screen slide sequence plus Next/Prev/Skip buttons.
    /// Each slide (CutsceneSlide) is either a static image or a video clip - freely mixed, in
    /// whatever order the folder scan finds them (IntroCutsceneSceneBuilder / EndingCutsceneScene-
    /// Builder). Same class drives both Intro and Ending cutscenes (IntroCutsceneController가 씬
    /// 이름을 필드로만 갖고 있어서 그대로 재사용됨).
    ///
    /// Rules (사용자 확정):
    ///  - Prev는 첫 장(index 0)에서 자동으로 숨겨짐 - 별도 "1장짜리 컷신" 처리 없이, 이 규칙 하나로
    ///    1장짜리 컷신(처음이자 마지막 장)도 자연스럽게 커버됨 (Prev 없음, Next=Skip과 동일하게 다음 씬行).
    ///  - 마지막 장에서 Next를 누르면 다음 장 대신 nextSceneName 씬으로 이동.
    ///  - Skip은 몇 장이든 상관없이 항상 바로 nextSceneName 씬으로 이동.
    ///  - 영상 슬라이드는 다 재생돼도 자동으로 다음 장/씬으로 안 넘어감 - 그림 슬라이드와 동일하게
    ///    Next를 눌러야 진행(입력 방식 일관성 유지, 2026-08-15 확정 - VideoPlayer+RenderTexture는
    ///    재생이 끝나도 검은 화면이 아니라 마지막 프레임이 그대로 멈춰있으므로 어색하지 않음).
    ///    재생 중이어도 Next/Skip은 항상 바로 작동(대기 없이 인터럽트 가능).
    ///  - 영상에 소리가 있으면 BGM은 학생이 그 화면의 BGM 파일을 직접 지우는 것으로 처리(별도 음소거
    ///    기능 없음) - VideoPlayer 기본 오디오 출력(Direct)을 그대로 사용.
    ///  - 설정(볼륨 등) 조절 UI 없음 - 타이틀에서 미리 맞추고 들어오는 구조.
    /// </summary>
    public class IntroCutsceneController : MonoBehaviour
    {
        [SerializeField] private Image slideImage;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private CutsceneSlide[] slides = new CutsceneSlide[0];
        [SerializeField] private GameObject prevButtonObject;

        [Tooltip("마지막 장에서 Next를 누르거나, 아무 장에서나 Skip을 눌렀을 때 이동할 씬 이름.")]
        [SerializeField] private string nextSceneName = "BackgroundTest";

        private RenderTexture videoRenderTexture;
        private int currentIndex;

        private void Awake()
        {
            SetupVideoPlayer();
            currentIndex = 0;
            ShowSlide(currentIndex);
        }

        private void OnDestroy()
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
            }
        }

        private void SetupVideoPlayer()
        {
            if (videoPlayer == null)
            {
                return;
            }

            // 1920x1080 고정 - 프로젝트 영상 해상도 규칙(사용자 확정)과 동일해서 클립 메타데이터를
            // 기다릴 필요 없이 미리 만들어둘 수 있다(타이틀 배경의 TitleBackgroundVideoPlayer와 동일 방식).
            videoRenderTexture = new RenderTexture(1920, 1080, 0) { name = "CutsceneVideoRT" };
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = false; // 슬라이드 종료 시 자동 진행 없음(사용자 확정) - Next를 눌러야 넘어감
            videoPlayer.playOnAwake = false; // ShowSlide가 장이 바뀔 때마다 명시적으로 Play() 호출
            // 원본 영상이 정확히 1920x1080(16:9)이 아닐 경우를 대비 - FitInside는 비율을 유지한 채
            // RenderTexture 안에 전부 들어가도록 축소(레터박스)하지, 잘라내거나 늘리지 않는다
            // (2026-08-17, 학생이 실수/의도로 다른 비율 원본을 넣는 경우 대비해서 명시적으로 지정).
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

            if (videoImage != null)
            {
                videoImage.texture = videoRenderTexture;
            }
        }

        public void OnNext()
        {
            if (slides == null || slides.Length == 0)
            {
                return;
            }

            if (currentIndex < slides.Length - 1)
            {
                JumpToSlide(currentIndex + 1);
            }
            else
            {
                GoToNextScene();
            }
        }

        public void OnPrev()
        {
            if (currentIndex > 0)
            {
                JumpToSlide(currentIndex - 1);
            }
        }

        public void OnSkip()
        {
            GoToNextScene();
        }

        /// <summary>
        /// Play 모드 여부와 무관하게 항상 안전하게 동작하는 순수 이동 함수 - 에디터의 "장 미리보기"
        /// 도구(IntroCutsceneControllerEditor)도 Play 모드 없이 이 함수를 그대로 호출해서 씬 뷰에
        /// 특정 장을 바로 띄워본다. (영상 장은 Edit 모드에서는 VideoPlayer가 프레임을 디코딩하지
        /// 않는 Unity 자체 한계로 실제 재생 화면이 안 보일 수 있음 - Play 모드에서 확인할 것.)
        /// </summary>
        public void JumpToSlide(int index)
        {
            if (slides == null || slides.Length == 0)
            {
                return;
            }

            currentIndex = Mathf.Clamp(index, 0, slides.Length - 1);
            ShowSlide(currentIndex);
        }

        private void ShowSlide(int index)
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            var slide = slides != null && index >= 0 && index < slides.Length ? slides[index] : null;
            var showVideo = slide != null && slide.IsVideo;

            if (slideImage != null)
            {
                slideImage.gameObject.SetActive(!showVideo);
                if (!showVideo && slide != null)
                {
                    slideImage.sprite = slide.image;
                }
            }

            if (videoImage != null)
            {
                videoImage.gameObject.SetActive(showVideo);
            }

            if (showVideo && videoPlayer != null)
            {
                videoPlayer.clip = slide.video;
                videoPlayer.Play();
            }

            if (prevButtonObject != null)
            {
                prevButtonObject.SetActive(index > 0);
            }
        }

        private void GoToNextScene()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("IntroCutsceneController.nextSceneName이 비어 있어 이동하지 않습니다.");
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
