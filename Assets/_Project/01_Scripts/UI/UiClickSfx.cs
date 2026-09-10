using UnityEngine;

namespace GameProject.UI
{
    /// <summary>
    /// One shared click sound for every clickable thing on the title screen (5 main buttons,
    /// the 3 popup X close buttons, 저장후닫기) - wired as a second persistent listener on each
    /// button's onClick alongside whatever that button actually does, so this component only
    /// needs to exist once per scene.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UiClickSfx : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip clip;

        private void Awake()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }

        public void PlayClick()
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}
