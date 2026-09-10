using UnityEngine;

namespace GameProject.Audio
{
    /// <summary>
    /// Applies the player's saved BGM/SFX volume to the mixer as soon as this scene loads. One of
    /// these sits in every scene that plays sound (added by "Tools/Class Template/Add Audio
    /// Settings Bootstrap To All Scenes") - not just Title - because most scenes in this project
    /// get opened directly for testing rather than always reached by pressing Start from the
    /// title screen, and a scene that never applies the saved volume would just play everything
    /// at full volume regardless of what the player set. Same lesson as the cross-scene SFX sync
    /// story in WorkLog 2026-08-08: reaching only "the scene you tested in" isn't enough.
    /// </summary>
    [DefaultExecutionOrder(-100)] // run before anything that might play a sound this frame
    public class AudioSettingsBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameAudioSettings.RevertToSaved();
        }
    }
}
