using UnityEngine;

namespace GameProject.Core
{
    /// <summary>
    /// Keeps every IMGUI (OnGUI) HUD/test element - HP text, monster/motion test buttons,
    /// status labels - the same relative size regardless of the actual Game view resolution.
    /// <para>
    /// All of that OnGUI code uses fixed pixel <see cref="Rect"/>/fontSize values tuned by eye
    /// at one specific Game view resolution (<see cref="ReferenceHeight"/> tall, measured via a
    /// temporary on-screen "Screen: WxH" debug label - Free Aspect Game view panels do NOT
    /// render at the project's design resolution, they render at whatever pixel size the panel
    /// itself is). Without this utility, the exact same pixel values look bigger or smaller on a
    /// different monitor / editor window size, which is why the project looked noticeably larger
    /// on a classroom PC than on a laptop.
    /// </para>
    /// <para>
    /// Call <see cref="Begin"/> as the very first line of every OnGUI method that draws fixed
    /// pixel Rects/fontSizes, then keep writing those Rects/fontSizes as if the screen were
    /// always exactly <see cref="ReferenceHeight"/> pixels tall - use <see cref="ReferenceWidth"/>
    /// instead of <c>Screen.width</c> and <see cref="ReferenceHeight"/> instead of
    /// <c>Screen.height</c> anywhere inside that OnGUI call. <c>GUI.matrix</c> takes care of
    /// scaling everything (position, size, and text) to the real resolution.
    /// </para>
    /// </summary>
    internal static class GuiScaleUtility
    {
        // Game view height (in pixels) the HP text / test buttons were last tuned against.
        // Measured directly off a "Screen: WxH" debug label on the reference machine/monitor -
        // update this constant (and nothing else) if that reference changes.
        public const float ReferenceHeight = 514f;

        /// <summary>Current Game view height expressed in reference-space pixels (always equals ReferenceHeight).</summary>
        public static float CurrentScale => Screen.height / ReferenceHeight;

        /// <summary>Current Game view width expressed in reference-space pixels (varies with aspect ratio).</summary>
        public static float ReferenceWidth => Screen.width / CurrentScale;

        /// <summary>Applies the reference-resolution scale to all GUI drawing for the rest of this OnGUI call.</summary>
        public static void Begin()
        {
            var scale = CurrentScale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        /// <summary>
        /// Widest button width that still lets a row of <paramref name="count"/> buttons (plus the
        /// gaps between them and a small margin on each side) fit inside <see cref="ReferenceWidth"/>,
        /// never wider than <paramref name="preferredWidth"/>.
        /// <para>
        /// Needed because <see cref="ReferenceWidth"/> is decided by the Game view's *aspect ratio*
        /// alone - at 16:9 it is always ~914 regardless of resolution or the Scale slider - so a row
        /// sized by eye can overflow with no way for the user to zoom out of it. MonsterB's six
        /// 170-wide buttons came to 1070 and were clipped ~78px on each side; MonsterA's five fit
        /// with only 24px to spare. Sizing the row to the space available removes that whole class
        /// of problem, including on wider/narrower aspects than the ones tested here.
        /// </para>
        /// </summary>
        public static float FitButtonWidth(float preferredWidth, int count, float gap, float sideMargin = 12f)
        {
            if (count <= 0)
            {
                return preferredWidth;
            }

            var available = ReferenceWidth - sideMargin * 2f - gap * (count - 1);
            return available <= 0f ? preferredWidth : Mathf.Min(preferredWidth, available / count);
        }

        /// <summary>
        /// Converts a world position (e.g. a monster's head) into the reference-space GUI
        /// coordinate to draw at after <see cref="Begin"/> - use this for per-instance floating
        /// labels (HP dots above each monster) instead of a fixed corner Rect, since multiple
        /// instances of the same monster type can't all draw to the same fixed screen position.
        /// Returns false (and leaves <paramref name="referencePoint"/> undefined) if the point is
        /// behind the camera and shouldn't be drawn this frame.
        /// </summary>
        public static bool TryWorldToReferencePoint(Camera camera, Vector3 worldPosition, out Vector2 referencePoint)
        {
            if (camera == null)
            {
                referencePoint = default;
                return false;
            }

            var screenPoint = camera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f)
            {
                referencePoint = default;
                return false;
            }

            var scale = CurrentScale;
            referencePoint = new Vector2(screenPoint.x / scale, (Screen.height - screenPoint.y) / scale);
            return true;
        }
    }
}
