using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Splits the room with an inner wall across its longer side — a kitchen off a dining
    // room, cabins off a corridor, or (low) a bar counter. Walkways and doorways always get
    // through, and each stretch of wall gets at least one opening, so the room stays one
    // connected space.
    [CreateAssetMenu(fileName = "DividerWallStructureRule", menuName = "CGD/Level/Structure/Partition")]
    public class PartitionStructureRule : RoomStructureRule
    {
        [Tooltip("Where the wall crosses the longer side, as a fraction of it (mirrored at random)")]
        [SerializeField] private FloatRange _position = new(0.3f, 0.4f);
        [SerializeField, Min(1)] private int _openingTiles = 2;
        [Tooltip("Metres; 0 = full wall height, about 1.1 = a counter")]
        [SerializeField, Min(0f)] private float _height;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            bool splitAlongX = bounds.width >= bounds.height;
            int length = splitAlongX ? bounds.width : bounds.height;
            if (length < 4) return;

            // The wall runs between rows `line - 1` and `line`, at least two tiles from the ends.
            int offset = Mathf.Clamp(Mathf.RoundToInt(length * _position.Evaluate(random)), 2, length - 2);
            if (random.Chance(0.5f)) offset = length - offset;
            int line = (splitAlongX ? bounds.xMin : bounds.yMin) + offset;

            var edges = new List<PartitionEdge>();
            foreach (List<(Vector2Int a, Vector2Int b)> stretch in Stretches(structure, bounds, splitAlongX, line))
                AddWithOpening(structure, stretch, random, edges);

            if (edges.Count > 0) structure.TryAddPartition(edges);
        }

        // The wall line cut into unbroken pieces of floor on both sides.
        private static List<List<(Vector2Int, Vector2Int)>> Stretches(RoomStructure structure, RectInt bounds, bool splitAlongX, int line)
        {
            var stretches = new List<List<(Vector2Int, Vector2Int)>>();
            List<(Vector2Int, Vector2Int)> current = null;
            int from = splitAlongX ? bounds.yMin : bounds.xMin;
            int to   = splitAlongX ? bounds.yMax : bounds.xMax;

            for (int c = from; c < to; c++)
            {
                Vector2Int a = splitAlongX ? new Vector2Int(line - 1, c) : new Vector2Int(c, line - 1);
                Vector2Int b = splitAlongX ? new Vector2Int(line, c)     : new Vector2Int(c, line);
                if (!structure.Footprint.Contains(a) || !structure.Footprint.Contains(b))
                {
                    current = null;
                    continue;
                }
                if (current == null) stretches.Add(current = new List<(Vector2Int, Vector2Int)>());
                current.Add((a, b));
            }
            return stretches;
        }

        // Walls the stretch except where a walkway or doorway crosses it; with none crossing,
        // a random gap is left instead. Stretches too short to hold wall and gap stay open.
        private void AddWithOpening(RoomStructure structure, List<(Vector2Int a, Vector2Int b)> stretch, RandomStream random, List<PartitionEdge> edges)
        {
            if (stretch.Count <= _openingTiles) return;

            var open = new bool[stretch.Count];
            bool crossed = false;
            for (int i = 0; i < stretch.Count; i++)
            {
                if (!MustStayOpen(structure, stretch[i].a, stretch[i].b)) continue;
                open[i] = crossed = true;
            }

            if (!crossed)
            {
                int start = random.Range(0, stretch.Count - _openingTiles + 1);
                for (int i = start; i < start + _openingTiles; i++) open[i] = true;
            }

            for (int i = 0; i < stretch.Count; i++)
                if (!open[i]) edges.Add(new PartitionEdge(stretch[i].a, stretch[i].b, _height));
        }

        // A walkway crossing the line, or a doorway or pillar right beside it.
        private static bool MustStayOpen(RoomStructure structure, Vector2Int a, Vector2Int b)
        {
            const RoomTileTags Beside = RoomTileTags.NearDoor | RoomTileTags.Structure;
            return structure.Has(a, RoomTileTags.Walkway) && structure.Has(b, RoomTileTags.Walkway)
                || structure.Has(a, Beside) || structure.Has(b, Beside);
        }
    }
}
