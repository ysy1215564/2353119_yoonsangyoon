using GameProject.Environment;
using UnityEngine;

namespace GameProject.Cameras
{
    /// <summary>
    /// Keeps the camera tracking a target transform (usually the player) so the
    /// character stays roughly centered on screen while the world scrolls past.
    /// The world itself never moves - only the camera does - so physics, gravity
    /// and collision all keep working normally in world space.
    ///
    /// Clamps the camera so it never scrolls past the level's horizontal bounds -
    /// without this, a centered camera shows empty space past the start/end of a stage
    /// as soon as the player reaches the edge. If groundParent is assigned, the bounds
    /// are (re)computed automatically from its children every time the scene loads, so
    /// adding/removing/reordering ground pieces never requires re-baking anything by hand.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;

        [Header("Level Bounds (optional)")]
        [SerializeField] private Transform groundParent; // if set, bounds are recomputed from this every Awake (source of truth)
        [SerializeField] private bool clampToBounds;
        [SerializeField] private float minX;
        [SerializeField] private float maxX;

        private Vector3 velocity;
        private Camera cachedCamera;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Points the camera at a "Ground" parent whose children's combined renderer
        /// bounds define the level's playable width. Bounds are recomputed from this
        /// every time the scene loads (Awake), so this is a "wire once" call - it does
        /// not need to be re-run when the ground layout changes later.
        /// </summary>
        public void SetGroundParent(Transform newGroundParent)
        {
            groundParent = newGroundParent;
        }

        /// <summary>
        /// Sets an explicit fixed bound instead of computing one from a ground parent.
        /// Mainly useful for scenes that don't have a "Ground" hierarchy to scan.
        /// </summary>
        public void SetHorizontalBounds(float minWorldX, float maxWorldX)
        {
            minX = minWorldX;
            maxX = maxWorldX;
            clampToBounds = true;
        }

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();

            if (groundParent != null && GroundBoundsUtility.TryComputeBounds(groundParent, out var lo, out var hi))
            {
                minX = lo;
                maxX = hi;
                clampToBounds = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = transform.position;
            if (followX)
            {
                desired.x = target.position.x + offset.x;
            }

            if (followY)
            {
                desired.y = target.position.y + offset.y;
            }

            if (clampToBounds)
            {
                desired.x = ClampToBounds(desired.x);
            }

            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
                : desired;
        }

        private float ClampToBounds(float x)
        {
            if (cachedCamera == null || !cachedCamera.orthographic)
            {
                return x;
            }

            var halfWidth = cachedCamera.orthographicSize * cachedCamera.aspect;
            var lo = minX + halfWidth;
            var hi = maxX - halfWidth;

            if (lo > hi)
            {
                // Level is narrower than one screen - just center it instead of clamping to a backwards range.
                return (minX + maxX) / 2f;
            }

            return Mathf.Clamp(x, lo, hi);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws the computed level bounds in the Scene view whenever the camera is
        /// selected - no Play required. Lets you catch a stray/leftover GroundN instance
        /// (from moving, deleting, or duplicating pieces) before it ever shows up as
        /// unexplained empty space at runtime: if the magenta boundary line sits well
        /// past your last visible ground piece, or a wire box floats disconnected from
        /// the rest, something under groundParent shouldn't be there.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (groundParent == null || !GroundBoundsUtility.TryComputeBounds(groundParent, out var lo, out var hi))
            {
                return;
            }

            const float lineHeight = 20f;

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(new Vector3(lo, -lineHeight, 0f), new Vector3(lo, lineHeight, 0f));
            Gizmos.DrawLine(new Vector3(hi, -lineHeight, 0f), new Vector3(hi, lineHeight, 0f));

            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.Label(new Vector3(lo, lineHeight + 0.5f, 0f), "Level Left Edge (camera clamp)");
            UnityEditor.Handles.Label(new Vector3(hi, lineHeight + 0.5f, 0f), "Level Right Edge (camera clamp)");

            // One marker per counted ground child, so a stray instance shows up as an
            // obviously disconnected box instead of an invisible contributor to lo/hi.
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
            foreach (Transform child in groundParent)
            {
                var renderer = child.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var b = renderer.bounds;
                Gizmos.DrawWireCube(new Vector3(b.center.x, 0f, 0f), new Vector3(b.size.x, 1f, 0f));
            }
        }
#endif
    }
}
