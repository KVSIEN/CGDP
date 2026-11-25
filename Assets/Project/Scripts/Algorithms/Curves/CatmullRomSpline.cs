using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // A Catmull-Rom spline (Edwin Catmull and Raphael Rom, 1974): a smooth curve that passes
    // through every one of its points.
    //
    // How it works: the curve between two points, p1 and p2, also looks at the point before (p0)
    // and the point after (p3). At p1 it heads in the direction from p0 to p2, and at p2 in the
    // direction from p1 to p3. Since neighbouring pieces agree on the direction at the point they
    // share, the whole path flows through each point without a kink. Each piece is a cubic curve
    // whose formula follows from those two directions.
    //
    // Why it's useful: Bézier curves need control points you have to place; a Catmull-Rom spline
    // only needs the points you want to pass through. Smooth patrol routes from a list of
    // waypoints, camera rails, a moving platform's track, rivers, roads and vines drawn through a
    // few points, smoothing a jagged A* path.
    //
    // This is the "uniform" kind: simplest, but it can loop or overshoot where points are spaced
    // very unevenly; space them roughly evenly.
    public static class CatmullRomSpline
    {
        // The point at t (0..1) on the piece between p1 and p2.
        public static Vector3 Point(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1
                         + (p2 - p0) * t
                         + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                         + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        // The point at t (0..1) along the whole path through `points` (two or more). Each piece
        // between two points gets an equal share of t. `loop` joins the last point back to the first.
        public static Vector3 Evaluate(IReadOnlyList<Vector3> points, float t, bool loop = false)
        {
            int count = points.Count;
            if (count == 1) return points[0];

            int pieces = loop ? count : count - 1;
            float along = Mathf.Clamp01(t) * pieces;
            int piece = Mathf.Min(Mathf.FloorToInt(along), pieces - 1);

            return Point(At(points, piece - 1, loop), At(points, piece, loop), At(points, piece + 1, loop), At(points, piece + 2, loop),
                         along - piece);
        }

        // A point by index: wrapped round on a loop, held at the first or last point past the ends.
        private static Vector3 At(IReadOnlyList<Vector3> points, int index, bool loop)
        {
            int count = points.Count;
            if (loop) return points[((index % count) + count) % count];
            return points[Mathf.Clamp(index, 0, count - 1)];
        }
    }
}
