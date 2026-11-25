using UnityEngine;

namespace CGD.Algorithms
{
    // Bézier curves: smooth curves shaped by control points that pull the curve toward them
    // without it passing through them.
    //
    // How they work (De Casteljau's algorithm): a Bézier curve is lerps of lerps. For a quadratic
    // curve from A to B with one control point C, at progress t: lerp A→C and C→B by t, then lerp
    // between those two points by t. A cubic curve has two control points and one more round of
    // lerping. The curve leaves A heading toward the first control point and arrives at B coming
    // from the last one, which makes them easy to shape by hand.
    //
    // Why they're useful: arcs and smooth paths between two known points. A grenade or loot
    // arc (start, apex, landing), a homing missile's swerve, a camera swoop, an enemy's leap,
    // cables and vines hanging between two anchors, UI lines between nodes (the map editor draws
    // its connections this way). For a smooth path through many points, see CatmullRomSpline.
    //
    // Note: t is progress along the curve's shape, not along its length: equal steps of t can
    // cover unequal distances where the curve bends.
    public static class BezierCurve
    {
        // From `start` to `end`, bent toward `control`.
        public static Vector3 Quadratic(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            Vector3 towardControl = Vector3.Lerp(start, control, t);
            Vector3 fromControl   = Vector3.Lerp(control, end, t);
            return Vector3.Lerp(towardControl, fromControl, t);
        }

        // From `start` to `end`, leaving toward `control1` and arriving from `control2`.
        public static Vector3 Cubic(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end, float t)
        {
            Vector3 first  = Quadratic(start, control1, control2, t);
            Vector3 second = Quadratic(control1, control2, end, t);
            return Vector3.Lerp(first, second, t);
        }

        // The direction (and speed) the cubic curve is moving at t: face along it, or orient
        // something travelling on it. Not normalised.
        public static Vector3 CubicTangent(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end, float t)
        {
            float u = 1f - t;
            return 3f * u * u * (control1 - start) + 6f * u * t * (control2 - control1) + 3f * t * t * (end - control2);
        }
    }
}
