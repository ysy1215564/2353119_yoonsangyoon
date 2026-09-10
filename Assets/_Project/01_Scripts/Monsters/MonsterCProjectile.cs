using GameProject.Core;
using GameProject.Player;
using UnityEngine;

namespace GameProject.Monsters
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class MonsterCProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float animationFramesPerSecond = 12f;
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private LayerMask floorMask = 1;
        [SerializeField] private Transform hitTarget;
        [SerializeField] private Collider2D ownerCollider;
        [SerializeField] private PlayerActionTestController playerDamageTarget;
        [SerializeField] private int damage = 1;
        [SerializeField] private float offscreenPadding = 0.25f;

        private SpriteRenderer spriteRenderer;
        private Vector3 velocity;
        private float frameTimer;
        private int frameIndex;

        public void Initialize(
            Sprite[] frames,
            Vector3 direction,
            float speed,
            Transform target,
            LayerMask groundMask,
            float framesPerSecond,
            PlayerActionTestController playerTarget = null,
            int damageAmount = 0,
            Collider2D owner = null)
        {
            animationFrames = frames;
            velocity = direction.normalized * speed;
            hitTarget = target;
            floorMask = groundMask;
            animationFramesPerSecond = Mathf.Max(0.01f, framesPerSecond);
            playerDamageTarget = playerTarget;
            damage = Mathf.Max(0, damageAmount);
            ownerCollider = owner;
            ApplyFrame(0);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            var body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;

            var circle = GetComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.28f;
        }

        private void Update()
        {
            // 스테이지 클리어 순간 이미 날아가고 있던 투사체는 그대로 두지 않고 즉시 제거한다
            // (사용자 확정, 2026-08-12) - 클리어 화면이 뜬 뒤에 뒤늦게 날아와 맞는 걸 방지.
            if (GameplayFreezeGate.IsFrozen)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += velocity * Time.deltaTime;
            UpdateAnimation();
            DestroyIfOffscreen();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsOwnerCollider(other))
            {
                return;
            }

            if (hitTarget != null && other.transform.IsChildOf(hitTarget))
            {
                if (playerDamageTarget != null && damage > 0)
                {
                    var hitDirection = hitTarget.position.x >= transform.position.x ? 1 : -1;
                    playerDamageTarget.TakeDamage(damage, hitDirection);
                }

                Destroy(gameObject);
                return;
            }

            if (((1 << other.gameObject.layer) & floorMask.value) != 0)
            {
                Destroy(gameObject);
            }
        }

        private bool IsOwnerCollider(Collider2D other)
        {
            if (ownerCollider == null || other == null)
            {
                return false;
            }

            return other == ownerCollider || other.transform.IsChildOf(ownerCollider.transform);
        }

        private void UpdateAnimation()
        {
            if (animationFrames == null || animationFrames.Length <= 1)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = 1f / animationFramesPerSecond;
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % animationFrames.Length;
                ApplyFrame(frameIndex);
            }
        }

        private void ApplyFrame(int index)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (animationFrames != null && index >= 0 && index < animationFrames.Length)
            {
                spriteRenderer.sprite = animationFrames[index];
            }
        }

        private void DestroyIfOffscreen()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var viewport = camera.WorldToViewportPoint(transform.position);
            if (viewport.z < 0f
                || viewport.x < -offscreenPadding
                || viewport.x > 1f + offscreenPadding
                || viewport.y < -offscreenPadding
                || viewport.y > 1f + offscreenPadding)
            {
                Destroy(gameObject);
            }
        }
    }
}
