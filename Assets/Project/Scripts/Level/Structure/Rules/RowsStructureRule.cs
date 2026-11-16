using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Parallel rows of inner wall with aisles between them: shelving, server racks, grow racks
    // (tall), or seat rows, pews and bed bays (low). Rows run along the room's longer side
    // unless set across it, stop short of the walls, and break every few tiles so the aisles
    // can be crossed. They never cross a walkway or crowd a doorway or pillar, and never divide
    // the room into zones.
    [CreateAssetMenu(fileName = "RowsStructureRule", menuName = "CGD/Level/Structure/Rows")]
    public class RowsStructureRule : RoomStructureRule
    {
        [Tooltip("Tiles from one row to the next: the width of each aisle")]
        [SerializeField, Min(2)] private int _pitch = 2;
        [Tooltip("Tiles kept free between the rows' ends and the walls")]
        [SerializeField, Min(0)] private int _wallMargin = 1;
        [Tooltip("Longest unbroken run of a row, in tiles, before a gap to cross the aisles through")]
        [SerializeField, Min(1)] private int _maxRun = 4;
        [SerializeField, Min(1)] private int _gapTiles = 1;
        [Tooltip("Metres; 0 = full wall height. Pulled into low (0.9–1.2) or high (2.2+) cover")]
        [SerializeField, Min(0f)] private float _height = 2.4f;
        [Tooltip("Rows run across the room's longer side instead of along it (seats facing a stage on the short wall)")]
        [SerializeField] private bool _across;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            bool alongX = bounds.width >= bounds.height != _across;
            // Rows stand on the edges between tile rows `line - 1` and `line`.
            int lineFrom = (alongX ? bounds.yMin : bounds.xMin) + _pitch;
            int lineTo   = (alongX ? bounds.yMax : bounds.xMax) - _pitch;
            int from = (alongX ? bounds.xMin : bounds.yMin) + _wallMargin;
            int to   = (alongX ? bounds.xMax : bounds.yMax) - _wallMargin;
            float height = CoverHeights.Readable(_height);

            for (int line = lineFrom; line <= lineTo; line += _pitch)
            {
                // Each row breaks at a different place, so the gaps don't line up into a lane.
                int phase = random.Range(0, _maxRun + _gapTiles);
                var run = new List<PartitionEdge>();
                for (int c = from; c < to; c++)
                {
                    Vector2Int a = alongX ? new Vector2Int(c, line - 1) : new Vector2Int(line - 1, c);
                    Vector2Int b = alongX ? new Vector2Int(c, line)     : new Vector2Int(line, c);
                    bool inGap = (c - from + phase) % (_maxRun + _gapTiles) >= _maxRun;
                    if (inGap || !CanStand(structure, a, b))
                    {
                        Close(structure, run);
                        continue;
                    }
                    run.Add(new PartitionEdge(a, b, height, dividesZones: false));
                }
                Close(structure, run);
            }
        }

        // A row may run beside a walkway, just not across one (EdgeMustStayOpen).
        private static bool CanStand(RoomStructure structure, Vector2Int a, Vector2Int b) =>
            structure.Footprint.Contains(a) && structure.Footprint.Contains(b) && !structure.IsPartitioned(a, b) && !structure.EdgeMustStayOpen(a, b);

        // Stands the run of wall collected so far (if the room stays whole) and starts a new one.
        private static void Close(RoomStructure structure, List<PartitionEdge> run)
        {
            if (run.Count > 0) structure.TryAddPartition(run.ToArray());
            run.Clear();
        }
    }
}
