using UnityEngine;

namespace GameProject.Audio
{
    /// <summary>
    /// A small round-robin pool of AudioSources parented to a character. Using more than one
    /// source avoids overlapping one-shot sounds fighting over a single shared pitch value
    /// (e.g. a hit sound cutting into an attack swing that's still playing).
    /// </summary>
    public sealed class SfxPlayer
    {
        private readonly Transform parent;
        private readonly AudioSource[] sources;
        private int nextIndex;
        private AudioSource loopSource;

        public SfxPlayer(Transform parent, int poolSize = 4)
        {
            this.parent = parent;
            sources = new AudioSource[Mathf.Max(1, poolSize)];
        }

        public void Play(SfxCue cue)
        {
            if (cue == null || cue.clip == null)
            {
                return;
            }

            EnsureSources();
            var source = sources[nextIndex];
            nextIndex = (nextIndex + 1) % sources.Length;
            source.pitch = 1f + Random.Range(-cue.pitchVariance, cue.pitchVariance);
            source.PlayOneShot(cue.clip, cue.volume);
        }

        /// <summary>
        /// Starts a looping sound on its own dedicated source (e.g. a charge-up hum). Calling
        /// this again while already playing the same clip does nothing, so it's safe to call
        /// every frame while a state is active. Use <see cref="StopLoop"/> to end it.
        /// </summary>
        public void PlayLoop(SfxCue cue)
        {
            if (cue == null || cue.clip == null)
            {
                return;
            }

            EnsureLoopSource();
            if (loopSource.isPlaying && loopSource.clip == cue.clip)
            {
                return;
            }

            loopSource.clip = cue.clip;
            loopSource.volume = cue.volume;
            loopSource.pitch = 1f + Random.Range(-cue.pitchVariance, cue.pitchVariance);
            loopSource.loop = true;
            loopSource.Play();
        }

        public void StopLoop()
        {
            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        private void EnsureSources()
        {
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                {
                    continue;
                }

                var sourceObject = new GameObject($"SfxSource_{i}");
                sourceObject.transform.SetParent(parent, false);
                var source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = GameAudioSettings.SfxGroup; // null until GameAudioMixer exists - fine, AudioSource just falls back to unrouted
                sources[i] = source;
            }
        }

        private void EnsureLoopSource()
        {
            if (loopSource != null)
            {
                return;
            }

            var sourceObject = new GameObject("SfxLoopSource");
            sourceObject.transform.SetParent(parent, false);
            loopSource = sourceObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.spatialBlend = 0f;
            loopSource.outputAudioMixerGroup = GameAudioSettings.SfxGroup;
        }
    }
}
