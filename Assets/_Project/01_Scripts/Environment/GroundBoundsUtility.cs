using UnityEngine;

namespace GameProject.Environment
{
    /// <summary>
    /// Scans a parent transform's children for renderer bounds and returns the overall
    /// left/right world-space extent. Used at runtime by both CameraFollow2D (to clamp the
    /// camera to the level's edges) and ParallaxLayer (to know how far background tiles
    /// need to stretch) so neither one needs a baked-in, easily-stale width value -
    /// whatever ground pieces exist under the parent at Play time is what gets measured,
    /// so adding/removing/reordering GroundN instances always "just works" without rerunning
    /// any editor command.
    /// </summary>
    public static class GroundBoundsUtility
    {
        public static bool TryComputeBounds(Transform parent, out float minX, out float maxX)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;

            if (parent == null)
            {
                return false;
            }

            foreach (Transform child in parent)
            {
                var renderer = child.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                minX = Mathf.Min(minX, renderer.bounds.min.x);
                maxX = Mathf.Max(maxX, renderer.bounds.max.x);
            }

            return !float.IsPositiveInfinity(minX);
        }
    }
}
