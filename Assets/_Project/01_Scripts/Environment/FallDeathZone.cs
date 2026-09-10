using GameProject.Player;
using UnityEngine;

namespace GameProject.Environment
{
    /// <summary>
    /// A wide, invisible trigger positioned well below the stage that kills the player if
    /// they fall through a gap in the ground. Sized/positioned automatically from whatever
    /// Ground pieces are under groundParent every time the scene loads (same principle as
    /// CameraFollow2D/ParallaxLayer - see GroundBoundsUtility), so it never needs manual
    /// resizing when the ground layout or gap positions change later.
    ///
    /// Monsters never reach here - MonsterGroundGuard stops MonsterA/B at the edge of any gap
    /// instead, whether walking or knocked back - so this only ever needs to handle the player.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FallDeathZone : MonoBehaviour
    {
        [SerializeField] private Transform groundParent; // if set, width/position are recomputed from this every Awake (source of truth)
        [SerializeField] private float triggerWorldY = -11f; // below the camera's visible bottom edge (~-5.4 at this project's orthographicSize 5.4 convention)
        [SerializeField] private float triggerHeight = 4f;
        [SerializeField] private float horizontalPadding = 20f; // generous - covers dash/knockback overshoot past the camera-clamped range

        private BoxCollider2D triggerCollider;

        /// <summary>
        /// Points the zone at a "Ground" parent whose children's combined renderer bounds
        /// define how wide the zone needs to be. Recomputed from this every time the scene
        /// loads (Awake), so this is a "wire once" call - it does not need to be re-run when
        /// the ground layout changes later.
        /// </summary>
        public void SetGroundParent(Transform newGroundParent)
        {
            groundParent = newGroundParent;
        }

        private void Awake()
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;

            var width = 200f; // fallback if no ground bounds are found (e.g. groundParent not wired yet)
            var centerX = transform.position.x;
            if (groundParent != null && GroundBoundsUtility.TryComputeBounds(groundParent, out var minX, out var maxX))
            {
                width = (maxX - minX) + horizontalPadding * 2f;
                centerX = (minX + maxX) / 2f;
            }

            transform.position = new Vector3(centerX, triggerWorldY, 0f);
            triggerCollider.size = new Vector2(width, triggerHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerActionTestController>();
            if (player != null)
            {
                player.KillByFalling();
            }
        }
    }
}
