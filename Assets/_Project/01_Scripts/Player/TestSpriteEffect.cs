using System.Collections;
using UnityEngine;

namespace GameProject.Player
{
    public sealed class TestSpriteEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Coroutine current;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Hide();
        }

        public void Play(Sprite[] frames, float framesPerSecond, Vector3 localOffset, Vector3 localScale)
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null)
            {
                return;
            }

            StopAndHide();

            transform.localPosition = localOffset;
            transform.localScale = localScale;
            current = StartCoroutine(PlayRoutine(frames, framesPerSecond));
        }

        public void PlayLoop(Sprite[] frames, float framesPerSecond, Vector3 localOffset, Vector3 localScale)
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null)
            {
                return;
            }

            StopAndHide();

            transform.localPosition = localOffset;
            transform.localScale = localScale;
            current = StartCoroutine(LoopRoutine(frames, framesPerSecond));
        }

        public void StopAndHide()
        {
            if (current != null)
            {
                StopCoroutine(current);
                current = null;
            }

            Hide();
        }

        public void Hide()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private IEnumerator PlayRoutine(Sprite[] frames, float framesPerSecond)
        {
            var delay = 1f / Mathf.Max(1f, framesPerSecond);
            spriteRenderer.enabled = true;

            foreach (var frame in frames)
            {
                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(delay);
            }

            spriteRenderer.enabled = false;
            current = null;
        }

        private IEnumerator LoopRoutine(Sprite[] frames, float framesPerSecond)
        {
            var delay = 1f / Mathf.Max(1f, framesPerSecond);
            var index = 0;
            spriteRenderer.enabled = true;

            while (frames.Length > 0)
            {
                spriteRenderer.sprite = frames[index % frames.Length];
                index++;
                yield return new WaitForSeconds(delay);
            }
        }
    }
}
