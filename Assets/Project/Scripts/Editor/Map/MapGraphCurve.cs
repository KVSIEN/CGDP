using CGD.Map;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // A connection drawn as a cubic Bézier in canvas space: drawing, hit distance and
    // the midpoint where non-normal link labels sit.
    public readonly struct MapGraphCurve
    {
        private readonly Vector2 _start;
        private readonly Vector2 _end;
        private readonly Vector2 _startTangent;
        private readonly Vector2 _endTangent;

        public MapGraphCurve(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
        {
            _start        = start;
            _end          = end;
            _startTangent = startTangent;
            _endTangent   = endTangent;
        }

        // Screen-space curve for one connection. Always runs left → right so tangents
        // follow the flow of the map; shortcuts join nodes on the same row, so they arc
        // over the nodes they skip.
        public static MapGraphCurve Between(Vector2 a, Vector2 b, ConnectionType type, float zoom)
        {
            if (a.x > b.x) (a, b) = (b, a);

            float pull = Mathf.Max(Mathf.Abs(b.x - a.x) * 0.5f, 30f * zoom);
            float arc  = type == ConnectionType.Shortcut ? -Mathf.Abs(b.x - a.x) * 0.35f : 0f;

            return new MapGraphCurve(a, b, a + new Vector2(pull, arc), b + new Vector2(-pull, arc));
        }

        public Vector2 Midpoint =>
            0.125f * (_start + _end) + 0.375f * (_startTangent + _endTangent);

        public void Draw(Color color, float width) =>
            Handles.DrawBezier(_start, _end, _startTangent, _endTangent, color, null, width);

        public float Distance(Vector2 point) =>
            HandleUtility.DistancePointBezier(point, _start, _end, _startTangent, _endTangent);
    }
}
