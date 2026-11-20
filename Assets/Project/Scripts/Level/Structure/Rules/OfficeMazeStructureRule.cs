using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // An office floor: narrow halls running the length of the room with rows of offices on
    // either side, each office with its own doorway onto a hall, split off from its neighbours by
    // full-height walls, and a zone of its own. Every wall leaves the room walkable (walls over a route or beside a
    // doorway become doorways instead, and one that would cut the room in two is dropped), and
    // each doorway gets a lintel over it. Rooms too small for halls and offices are left alone.
    [CreateAssetMenu(fileName = "OfficeMazeStructureRule", menuName = "CGD/Level/Structure/Office Maze")]
    public class OfficeMazeStructureRule : RoomStructureRule
    {
        private const int MinAlong = 6, MinAcross = 5;

        [Tooltip("Tiles wide a hall is (1 = 3 m with the default tiles)")]
        [SerializeField, Min(1)] private int _hallTiles = 1;
        [Tooltip("Tiles deep each block of offices is, hall to hall")]
        [SerializeField] private IntRange _officeDepth = new(2, 3);
        [Tooltip("Tiles wide each office is, along the hall")]
        [SerializeField] private IntRange _officeWidth = new(2, 3);
        [Tooltip("Chance a wall between two offices has a doorway of its own, making a suite")]
        [SerializeField, Range(0f, 1f)] private float _suiteChance = 0.25f;
        [Tooltip("Chance an office has a doorway onto the hall on its far side too, when it has one")]
        [SerializeField, Range(0f, 1f)] private float _throughChance = 0.3f;
        [Tooltip("Metres of clear height under the lintel over every doorway")]
        [SerializeField, Min(1f)] private float _doorHeight = 2.6f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            bool alongX = bounds.width >= bounds.height;
            int length = alongX ? bounds.width : bounds.height;
            int width  = alongX ? bounds.height : bounds.width;
            if (length < MinAlong || width < MinAcross) return;

            // Tile at `along` steps down the long axis and `across` steps over the short one.
            Vector2Int TileAt(int along, int across) => alongX
                ? new Vector2Int(bounds.xMin + along, bounds.yMin + across)
                : new Vector2Int(bounds.xMin + across, bounds.yMin + along);

            List<Block> blocks = LayBlocks(width, random);
            List<int> moduleStarts = LayModules(length, random);   // where each office starts along the hall

            for (int b = 0; b < blocks.Count; b++)
            {
                Block block = blocks[b];
                if (block.IsHall) continue;

                bool hallBelow = b > 0 && blocks[b - 1].IsHall;
                bool hallAbove = b + 1 < blocks.Count && blocks[b + 1].IsHall;
                for (int m = 0; m + 1 < moduleStarts.Count; m++)
                {
                    int from = moduleStarts[m], to = moduleStarts[m + 1];
                    bool below = hallBelow && (!hallAbove || random.Chance(0.5f));
                    bool above = hallAbove && (!below || random.Chance(_throughChance));

                    if (hallBelow) WallAcross(structure, TileAt, from, to, block.Start, door: below, random);
                    if (hallAbove) WallAcross(structure, TileAt, from, to, block.End, door: above, random);
                    if (m > 0) WallBetweenOffices(structure, TileAt, from, block, random);
                }
            }
        }

        // --- Layout -----------------------------------------------------------------------

        private readonly struct Block
        {
            public Block(bool isHall, int start, int end)
            {
                IsHall = isHall;
                Start  = start;
                End    = end;
            }

            public bool IsHall { get; }
            public int  Start  { get; }   // first row, inclusive
            public int  End    { get; }   // one past the last row
        }

        // Halls and blocks of offices side by side across the room, from one side to the other.
        private List<Block> LayBlocks(int width, RandomStream random)
        {
            var blocks = new List<Block>();
            bool hall = random.Chance(0.5f);
            for (int row = 0; row < width;)
            {
                int size = hall ? _hallTiles : Mathf.Max(1, _officeDepth.Evaluate(random));
                // A sliver left over at the far side joins the last block.
                if (width - (row + size) < (hall ? 1 : 2)) size = width - row;
                blocks.Add(new Block(hall, row, row + size));
                row += size;
                hall = !hall;
            }
            return blocks;
        }

        // Where each office along a hall starts, ending with the room's length.
        private List<int> LayModules(int length, RandomStream random)
        {
            var starts = new List<int> { 0 };
            while (starts[^1] < length)
            {
                int next = starts[^1] + Mathf.Max(2, _officeWidth.Evaluate(random));
                if (length - next < 2) next = length;
                starts.Add(next);
            }
            return starts;
        }

        // --- Walls ------------------------------------------------------------------------

        // A wall along the hall at row boundary `row`, from `from` to `to`, with one doorway in
        // it when `door`.
        private void WallAcross(RoomStructure structure, System.Func<int, int, Vector2Int> tileAt, int from, int to, int row, bool door, RandomStream random)
        {
            if (row <= 0 || !HasBothSides(structure, tileAt, from, to, row)) return;

            int doorway = door ? random.Range(from, to) : -1;
            var wall = new Wall();
            for (int along = from; along < to; along++)
            {
                Vector2Int a = tileAt(along, row - 1);
                Vector2Int b = tileAt(along, row);
                if (structure.Contains(a) && structure.Contains(b)) wall.Add(structure, a, b, along == doorway, _doorHeight);
            }
            wall.Commit(structure);
        }

        // The wall between this office and the next, from the hall side to the far side.
        private void WallBetweenOffices(RoomStructure structure, System.Func<int, int, Vector2Int> tileAt, int along, Block block, RandomStream random)
        {
            int doorway = random.Chance(_suiteChance) ? random.Range(block.Start, block.End) : -1;
            var wall = new Wall();
            for (int row = block.Start; row < block.End; row++)
            {
                Vector2Int a = tileAt(along - 1, row);
                Vector2Int b = tileAt(along, row);
                if (structure.Contains(a) && structure.Contains(b)) wall.Add(structure, a, b, row == doorway, _doorHeight);
            }
            wall.Commit(structure);
        }

        private static bool HasBothSides(RoomStructure structure, System.Func<int, int, Vector2Int> tileAt, int from, int to, int row)
        {
            for (int along = from; along < to; along++)
                if (structure.Contains(tileAt(along, row - 1)) && structure.Contains(tileAt(along, row))) return true;
            return false;
        }

        // One run of wall and the openings in it. An office's own doorway keeps the office a zone
        // of its own, so it is furnished as one (a bunk in every cabin, a desk in every cubicle).
        // A gap left where a route crosses joins what it crosses, so the halls and the offices the
        // routes run through stay the room's main space.
        private sealed class Wall
        {
            private readonly List<PartitionEdge> _walls = new();
            private readonly List<PartitionEdge> _doors = new();
            private readonly List<PartitionEdge> _gaps  = new();

            public void Add(RoomStructure structure, Vector2Int a, Vector2Int b, bool door, float doorHeight)
            {
                if (structure.EdgeMustStayOpen(a, b)) _gaps.Add(new PartitionEdge(a, b, doorHeight, dividesZones: false));
                else if (door) _doors.Add(new PartitionEdge(a, b, doorHeight, dividesZones: false));
                else _walls.Add(new PartitionEdge(a, b, 0f, dividesZones: false));
            }

            // Doorways only where the walls beside them were accepted.
            public void Commit(RoomStructure structure)
            {
                if (_walls.Count == 0 || !structure.TryAddPartition(_walls)) return;
                foreach (PartitionEdge door in _doors) structure.AddOpening(door, separatesZones: true);
                foreach (PartitionEdge gap in _gaps) structure.AddOpening(gap);
            }
        }
    }
}
