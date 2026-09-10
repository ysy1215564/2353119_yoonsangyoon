using UnityEngine;

namespace GameProject.Audio
{
    /// <summary>
    /// One assignable sound effect slot: which clip plays, how loud, and how much its pitch
    /// should randomize each time it plays. Shared by the player and monster action test
    /// controllers so every character configures its SFX the same way in the Inspector.
    /// </summary>
    [System.Serializable]
    public sealed class SfxCue
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch variation applied each time this sound plays, e.g. 0.05 = +/-5%.")]
        [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
    }
}
