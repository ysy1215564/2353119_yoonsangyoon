using UnityEngine;

namespace GameProject.Monsters
{
    /// <summary>
    /// Shared helper that stops a monster from walking or being knocked off the edge of a
    /// Ground segment into a fall-through gap. Used by MonsterA/B for both their own patrol
    /// movement and their hit-knockback - per the class's design decision, a monster never
    /// falls into a gap no matter how it would have moved (even under repeated hits), so both
    /// call sites route through this same clamp instead of having separate "avoid gap while
    /// walking" and "stop at edge when knocked" logic.
    ///
    /// Checks physically via Physics2D against groundMask, using whatever Ground/Collision
    /// colliders are actually placed in the scene at that moment - same principle as
    /// CameraFollow2D/ParallaxLayer's auto-bounds (see GroundBoundsUtility), so moving,
    /// resizing, or adding a gap later never needs a code change here.
    /// </summary>
    public static class MonsterGroundGuard
    {
        private const float SampleStep = 0.05f;

        /// <summary>
        /// Returns how much of desiredDeltaX the monster can actually move along (same sign,
        /// magnitude reduced to stop right at the last point solid ground was found under the
        /// monster's foot). currentPosition is the monster's current transform.position;
        /// footCheckOffsetY lifts the ray start slightly above ground level (a monster's own Y
        /// already sits exactly on GroundTopY, so starting the ray from Y=0 would begin inside
        /// the ground collider itself); groundCheckDistance is how far below that point to
        /// probe for ground. Sweeps in small steps rather than checking only the destination,
        /// so a single large knockback can't "jump over" a gap narrower than the knockback
        /// distance - it still stops precisely at the edge.
        /// </summary>
        public static float ClampHorizontalMove(
            Vector3 currentPosition,
            float desiredDeltaX,
            float footCheckOffsetY,
            float groundCheckDistance,
            LayerMask groundMask)
        {
            if (Mathf.Approximately(desiredDeltaX, 0f))
            {
                return 0f;
            }

            var direction = Mathf.Sign(desiredDeltaX);
            var totalDistance = Mathf.Abs(desiredDeltaX);
            var allowedDistance = 0f;

            while (allowedDistance < totalDistance)
            {
                var nextDistance = Mathf.Min(allowedDistance + SampleStep, totalDistance);
                var footPoint = currentPosition + new Vector3(direction * nextDistance, footCheckOffsetY, 0f);
                if (!Physics2D.Raycast(footPoint, Vector2.down, groundCheckDistance, groundMask))
                {
                    break; // ground ends here - stop at allowedDistance instead of walking/knocking off it
                }

                allowedDistance = nextDistance;
            }

            return direction * allowedDistance;
        }
    }
}
