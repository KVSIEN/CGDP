using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // The classic three-lane room: two long walls down its length split it into a wide middle
    // lane and two flanking side lanes, joined at both ends and through a gap or two partway
    // along. The middle is the direct, exposed way through; the sides are the way round
    // unseen. Walkways and doorways always get through, and the walls go in through
    // TryAddPartition, so the room stays one connected space.
    [CreateAssetMenu(fileName = "ThreeLanesStructureRule", menuName = "CGD/Level/Structure/Three Lanes")]
    public class ThreeLanesStructureRule : RoomStructureRule
    {
        [Tooltip("Share of the room's width the middle lane takes; the side lanes split the rest")]
        [SerializeField] private FloatRange _middleShare = new(0.34f, 0.45f);
        [Tooltip("Narrowest a lane may be, in tiles: a room too narrow for three is left alone")]
        [SerializeField, Min(1)] private int _minLaneTiles = 2;
        [Tooltip("Tiles left open at each end of a lane wall, so the lanes join before the far walls")]
        [SerializeField, Min(0)] private int _endGap = 1;
        [Tooltip("Gaps partway along each lane wall, where you cross between lanes")]
        [SerializeField] private IntRange _crossings = new(1, 2);
        [SerializeField, Min(1)] private int _crossingTiles = 2;
        [Tooltip("Metres; 0 = full wall height. Low (about 1.1) makes counters you shoot over; high (2.2+) hides the side lanes")]
        [SerializeField, Min(0f)] private float _height = 2.4f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            bool alongX = bounds.width >= bounds.height;
            int across = alongX ? bounds.height : bounds.width;
            int middle = Mathf.RoundToInt(across * _middleShare.Evaluate(random));
            int side = (across - middle) / 2;
            if (side < _minLaneTiles || middle < _minLaneTiles) return;

            int acrossMin = alongX ? bounds.yMin : bounds.xMin;
            int crossings = Mathf.Max(0, _crossings.Evaluate(random));
            float height = CoverHeights.Readable(_height);
            foreach (int line in new[] { acrossMin + side, acrossMin + side + middle })
            {
                List<PartitionEdge> wall = LaneWall(structure, bounds, alongX, line, crossings, height);
                if (wall.Count > 0) structure.TryAddPartition(wall);
            }
        }

        // The wall between rows `line - 1` and `line`, running the room's length, with its ends,
        // crossings and every route through it left open.
        private List<PartitionEdge> LaneWall(RoomStructure structure, RectInt bounds, bool alongX, int line, int crossings, float height)
        {
            int from = (alongX ? bounds.xMin : bounds.yMin) + _endGap;
            int to = (alongX ? bounds.xMax : bounds.yMax) - _endGap;
            int length = to - from;
            var gaps = new HashSet<int>();
            for (int j = 0; j < crossings; j++)
            {
                int centre = from + Mathf.RoundToInt(length * (j + 1f) / (crossings + 1f));
                for (int k = 0; k < _crossingTiles; k++) gaps.Add(centre - _crossingTiles / 2 + k);
            }

            var edges = new List<PartitionEdge>();
            for (int c = from; c < to; c++)
            {
                if (gaps.Contains(c)) continue;
                Vector2Int a = alongX ? new Vector2Int(c, line - 1) : new Vector2Int(line - 1, c);
                Vector2Int b = alongX ? new Vector2Int(c, line)     : new Vector2Int(line, c);
                if (!structure.Contains(a) || !structure.Contains(b) || structure.EdgeMustStayOpen(a, b)) continue;
                edges.Add(new PartitionEdge(a, b, height, dividesZones: false));
            }
            return edges;
        }
    }
}
