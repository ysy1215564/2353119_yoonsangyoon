using GameProject.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace GameProject.UI
{
    /// <summary>
    /// Owns the settings popup's two volume sliders. 사용자 확정 스펙:
    ///  - 슬라이더를 드래그하는 동안은 실제 BGM/효과음 소리 크기에 바로 반영된다(라이브 프리뷰).
    ///  - 닫기(X)로 닫으면 그 변경은 저장되지 않고, 마지막 저장값으로 되돌아간다.
    ///  - "저장 후 닫기" 버튼을 눌러야 PlayerPrefs에 실제로 저장된다.
    ///  - 최초 실행 시 초기값은 BGM/효과음 모두 최대치(GameAudioSettings의 기본값과 동일).
    /// </summary>
    public class SettingsPopupController : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TitleMenuController menuController;

        [Tooltip("효과음 슬라이더를 클릭/드래그할 때마다 들려주는 미리듣기 소리 (SFX 믹서 그룹에 연결돼 있어 슬라이더로 조절한 크기가 그대로 반영됨).")]
        [SerializeField] private AudioSource sfxPreviewSource;
        [SerializeField] private AudioClip sfxPreviewClip;
        private const float SfxPreviewCooldown = 0.15f; // 드래그 중 매 프레임 재생되면 연사음처럼 겹쳐 들려서, 너무 잦은 재생만 걸러냄
        private float lastSfxPreviewTime = -999f;

        private void Awake()
        {
            // 런타임 리스너로 직접 붙인다(에디터 시점의 UnityEvent persistent listener로 float 값을
            // 그대로 전달하려면 reflection으로 EventDefined 모드를 조작해야 해서 불안정함) - 이 방식이
            // 훨씬 간단하고 표준적이다.
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }
        }

        private void OnEnable()
        {
            // 팝업이 열릴 때마다(꺼져 있다가 켜질 때) 저장된 값으로 슬라이더를 되돌린다 - X로 닫은
            // 뒤 다시 열었을 때 직전에 취소했던 드래그가 남아있지 않도록.
            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(GameAudioSettings.BgmVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
            }

            GameAudioSettings.RevertToSaved();
        }

        /// <summary>Slider의 On Value Changed에 연결 - 드래그 중 실시간으로 소리 크기만 바꾸고
        /// PlayerPrefs에는 아직 저장하지 않는다.</summary>
        public void OnBgmSliderChanged(float value) => GameAudioSettings.ApplyLiveBgm(value);

        public void OnSfxSliderChanged(float value)
        {
            GameAudioSettings.ApplyLiveSfx(value);
            PlaySfxPreview();
        }

        private void PlaySfxPreview()
        {
            if (sfxPreviewSource == null || sfxPreviewClip == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - lastSfxPreviewTime < SfxPreviewCooldown)
            {
                return;
            }

            lastSfxPreviewTime = now;
            sfxPreviewSource.PlayOneShot(sfxPreviewClip);
        }

        /// <summary>"저장 후 닫기" 버튼용 - 현재 슬라이더 값을 PlayerPrefs에 저장하고 팝업을 닫는다.</summary>
        public void SaveAndClose()
        {
            if (bgmSlider != null)
            {
                GameAudioSettings.BgmVolume = bgmSlider.value;
            }

            if (sfxSlider != null)
            {
                GameAudioSettings.SfxVolume = sfxSlider.value;
            }

            ClosePopup();
        }

        /// <summary>닫기(X) 버튼용 - 드래그로 미리 들어본 값은 버리고 마지막 저장값으로 되돌린 뒤 닫는다.</summary>
        public void CancelAndClose()
        {
            GameAudioSettings.RevertToSaved();
            ClosePopup();
        }

        private void ClosePopup()
        {
            if (menuController != null)
            {
                menuController.CloseCurrentPopup();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
