using UnityEngine;
using UnityEngine.Video;

namespace GameProject.UI
{
    /// <summary>
    /// One entry in IntroCutsceneController's slide sequence - either a static image or a video
    /// clip, never both. 어느 쪽인지는 video 필드가 채워져 있는지로 판단(IsVideo) - 별도 enum/토글
    /// 없이, 폴더 스캔 시점에 파일 확장자(.png vs .mp4)로 자동 결정됨(IntroCutsceneSceneBuilder /
    /// EndingCutsceneSceneBuilder의 LoadSlides 참고). 슬라이드 단위로 그림/영상 자유 혼합 가능하고
    /// 순서는 파일명 뒤 번호로 학생이 직접 정함(사용자 확정, 2026-08-15).
    /// </summary>
    [System.Serializable]
    public class CutsceneSlide
    {
        public Sprite image;
        public VideoClip video;

        public bool IsVideo => video != null;
    }
}
