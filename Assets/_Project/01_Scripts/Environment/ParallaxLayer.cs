using UnityEngine;

namespace GameProject.Environment
{
    /// <summary>
    /// Scrolls a background layer relative to the camera to create a parallax depth
    /// effect, and (optionally) rebuilds its own tile row at Start to cover the level's
    /// actual width - whatever that is at Play time - so it never runs short after the
    /// ground layout is extended, and never needs re-baking by hand.
    ///
    /// The layer's existing first child is used as the tile "template" (its sprite,
    /// tint, sorting order and scale get copied onto every generated tile) - place at
    /// least one tile as a child in the prefab/scene before Play; if groundParent isn't
    /// assigned, this script only handles scrolling and leaves whatever tiles already
    /// exist untouched.
    ///
    /// parallaxFactor:
    ///   0   = moves like normal level geometry (scrolls at the same speed as the camera pans - no lag)
    ///   1   = moves exactly with the camera, so it looks fixed on screen (e.g. a distant sky)
    ///   0-1 = lags behind the camera, giving the classic "farther layers scroll slower" depth illusion
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.5f;

        [Header("Auto Tiling (optional)")]
        [SerializeField] private Transform groundParent; // if set, tiles are rebuilt at Start to cover this parent's width

        private Vector3 lastCameraPosition;
        private bool initialized;

        public float ParallaxFactor
        {
            get => parallaxFactor;
            set => parallaxFactor = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Points this layer at a "Ground" parent whose children's combined renderer
        /// bounds define the level's playable width. Tiles are rebuilt from this every
        /// time the scene loads (Start), so this is a "wire once" call.
        /// </summary>
        public void SetGroundParent(Transform newGroundParent)
        {
            groundParent = newGroundParent;
        }

        private void OnEnable()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            initialized = false;
        }

        private void Start()
        {
            if (groundParent != null)
            {
                RebuildTilesToCoverGround();
            }
        }

        private void RebuildTilesToCoverGround()
        {
            if (!GroundBoundsUtility.TryComputeBounds(groundParent, out var minX, out var maxX))
            {
                return;
            }

            if (transform.childCount == 0)
            {
                return; // nothing to use as a template tile
            }

            var template = transform.GetChild(0);
            var templateRenderer = template.GetComponent<SpriteRenderer>();
            if (templateRenderer == null)
            {
                return;
            }

            var tileWidth = templateRenderer.bounds.size.x;
            if (tileWidth <= 0.001f)
            {
                return;
            }

            var sprite = templateRenderer.sprite;
            var color = templateRenderer.color;
            var sortingOrder = templateRenderer.sortingOrder;
            var scale = template.localScale;

            var camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
            var halfViewWidth = camera != null && camera.orthographic
                ? camera.orthographicSize * camera.aspect
                : tileWidth;

            // Cover the camera's full view at both extremes, plus one extra tile of slack
            // on each side so floating point/aspect-ratio edge cases never show a gap.
            var coverMin = minX - halfViewWidth - tileWidth;
            var coverMax = maxX + halfViewWidth + tileWidth;
            var tileCount = Mathf.Max(1, Mathf.CeilToInt((coverMax - coverMin) / tileWidth) + 1);
            var centerX = (coverMin + coverMax) / 2f;
            var startX = centerX - (tileCount - 1) / 2f * tileWidth;
            var rootX = transform.position.x;

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            for (var i = 0; i < tileCount; i++)
            {
                var worldX = startX + i * tileWidth;
                var tile = new GameObject($"Tile_{i}", typeof(SpriteRenderer));
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = new Vector3(worldX - rootX, 0f, 0f);
                tile.transform.localScale = scale;

                var renderer = tile.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = sortingOrder;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main == null)
                {
                    return;
                }

                cameraTransform = Camera.main.transform;
            }

            if (!initialized)
            {
                lastCameraPosition = cameraTransform.position;
                initialized = true;
                return;
            }

            var delta = cameraTransform.position - lastCameraPosition;
            if (delta.sqrMagnitude > 0f)
            {
                transform.position += new Vector3(delta.x, delta.y, 0f) * parallaxFactor;
            }

            lastCameraPosition = cameraTransform.position;
        }
    }
}
