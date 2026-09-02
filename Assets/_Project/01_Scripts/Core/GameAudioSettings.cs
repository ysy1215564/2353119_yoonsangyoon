using UnityEngine;
using UnityEngine.Audio;

namespace GameProject.Audio
{
    /// <summary>
    /// Single source of truth for the game's BGM/SFX volume: reads and writes PlayerPrefs so the
    /// setting survives closing and reopening the game, and pushes the value onto the shared
    /// AudioMixer so every AudioSource routed through it (BGM sources, SfxPlayer's pooled
    /// sources) actually gets quieter/louder in real time.
    ///
    /// Loaded via Resources.Load rather than a per-scene serialized reference on purpose - this
    /// project has many scenes that get opened directly for testing (ActionTest, BackgroundTest,
    /// three monster motion test scenes, Title), and wiring a mixer reference by hand in each one
    /// is exactly the kind of per-scene step that's easy to forget (see WorkLog 2026-08-08's
    /// cross-scene SFX sync story). Resources.Load needs nothing wired per scene - it just works
    /// the moment GameAudioMixer.mixer sits under a "Resources" folder anywhere in Assets.
    ///
    /// One-time manual setup this class assumes already exists (Unity doesn't expose creating
    /// AudioMixer assets/groups/exposed parameters through the public scripting API - only editor
    /// UI can do that part):
    ///   1. Create the mixer: Assets > Create > Audio Mixer, name it "GameAudioMixer", move it to
    ///      Assets/_Project/06_Audio/Resources/GameAudioMixer.mixer (create the Resources folder).
    ///   2. In the Audio Mixer window, right-click the "Master" group's child list and add two
    ///      child groups named exactly "BGM" and "SFX".
    ///   3. Select "BGM", right-click its Volume slider in the Inspector, "Expose 'Volume (of BGM)'
    ///      to script", then in the mixer's Exposed Parameters list rename it to "BgmVolume".
    ///   4. Do the same for "SFX" -> expose its Volume -> rename to "SfxVolume".
    /// Everything else (routing AudioSources to these groups, reading/writing the saved value,
    /// applying it on every scene load) is automated - see AudioSettingsBootstrap and the
    /// "Add Audio Settings Bootstrap To All Scenes" editor command.
    /// </summary>
    // 이름을 AudioSettings가 아니라 GameAudioSettings로 지은 이유: UnityEngine에 이미
    // UnityEngine.AudioSettings(샘플레이트 등 엔진 전역 오디오 설정)라는 static 클래스가 있어서,
    // using UnityEngine + using GameProject.Audio를 동시에 쓰는 파일(에디터 스크립트 등)에서
    // "AudioSettings"만 쓰면 어느 쪽인지 모호하다는 컴파일 에러가 남.
    public static class GameAudioSettings
    {
        private const string MixerResourcePath = "GameAudioMixer";
        private const string BgmGroupName = "BGM";
        private const string SfxGroupName = "SFX";
        private const string BgmParam = "BgmVolume";
        private const string SfxParam = "SfxVolume";

        private const string BgmVolumeKey = "Audio.BgmVolume";
        private const string SfxVolumeKey = "Audio.SfxVolume";
        private const float DefaultVolume = 1f; // 사용자 결정: 초기값은 BGM/효과음 모두 최대치
        private const float MinDecibel = -80f; // Unity AudioMixer's practical silence floor

        private static AudioMixer cachedMixer;
        private static AudioMixerGroup cachedBgmGroup;
        private static AudioMixerGroup cachedSfxGroup;
        private static bool loggedMissingMixer;

        /// <summary>Saved BGM volume (0..1), read from PlayerPrefs (defaults to max on first run).</summary>
        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
            set
            {
                var clamped = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(BgmVolumeKey, clamped);
                PlayerPrefs.Save();
                ApplyLiveBgm(clamped);
            }
        }

        /// <summary>Saved SFX volume (0..1), read from PlayerPrefs (defaults to max on first run).</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
            set
            {
                var clamped = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
                PlayerPrefs.Save();
                ApplyLiveSfx(clamped);
            }
        }

        /// <summary>The mixer group every BGM AudioSource should route through, or null if the
        /// mixer hasn't been set up yet (see the setup steps above) - callers should null-check
        /// and simply leave outputAudioMixerGroup unset rather than throwing.</summary>
        public static AudioMixerGroup BgmGroup => GetGroup(BgmGroupName, ref cachedBgmGroup);

        /// <summary>The mixer group every SFX AudioSource (SfxPlayer's pooled sources) should
        /// route through, or null if the mixer hasn't been set up yet.</summary>
        public static AudioMixerGroup SfxGroup => GetGroup(SfxGroupName, ref cachedSfxGroup);

        /// <summary>
        /// Pushes the mixer's live volume to match the given value without touching PlayerPrefs -
        /// used while a settings slider is being dragged, so the change is heard immediately but
        /// isn't committed until "저장 후 닫기" is pressed (닫기(X) discards it instead, see
        /// RevertToSaved).
        /// </summary>
        public static void ApplyLiveBgm(float value) => SetMixerVolume(BgmParam, value);

        public static void ApplyLiveSfx(float value) => SetMixerVolume(SfxParam, value);

        /// <summary>
        /// Applies the PlayerPrefs-saved volumes to the mixer. Called once per scene on Awake by
        /// AudioSettingsBootstrap so every scene starts at the player's saved volume - and called
        /// again when a settings popup is closed via 닫기(X) to throw away whatever the slider was
        /// previewing live and snap the mixer back to what's actually saved.
        /// </summary>
        public static void RevertToSaved()
        {
            ApplyLiveBgm(BgmVolume);
            ApplyLiveSfx(SfxVolume);
        }

        private static void SetMixerVolume(string exposedParam, float linearValue)
        {
            var mixer = GetMixer();
            if (mixer == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(linearValue);
            var decibel = clamped <= 0.0001f ? MinDecibel : Mathf.Log10(clamped) * 20f;
            mixer.SetFloat(exposedParam, decibel);
        }

        private static AudioMixerGroup GetGroup(string groupName, ref AudioMixerGroup cache)
        {
            if (cache != null)
            {
                return cache;
            }

            var mixer = GetMixer();
            if (mixer == null)
            {
                return null;
            }

            var matches = mixer.FindMatchingGroups(groupName);
            if (matches == null || matches.Length == 0)
            {
                Debug.LogWarning($"GameAudioMixer has no '{groupName}' group - see GameAudioSettings.cs for one-time setup steps.");
                return null;
            }

            cache = matches[0];
            return cache;
        }

        private static AudioMixer GetMixer()
        {
            if (cachedMixer != null)
            {
                return cachedMixer;
            }

            cachedMixer = Resources.Load<AudioMixer>(MixerResourcePath);
            if (cachedMixer == null && !loggedMissingMixer)
            {
                loggedMissingMixer = true;
                Debug.LogWarning(
                    $"No AudioMixer found at Resources/{MixerResourcePath}. Volume slider will move but " +
                    "won't change anything until it's created - see the setup steps at the top of GameAudioSettings.cs.");
            }

            return cachedMixer;
        }
    }
}
