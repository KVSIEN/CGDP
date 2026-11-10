using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Every wall of a LevelLayout as a line in tile units (tile corners are whole numbers),
    // with the floor on its left: what LevelWallBuilder turns into boxes and LevelBlueprint
    // draws, so the plan and the built level always agree.
    //   - Tile-edge walls between walkable and solid ground, merged into one segment per
    //     unbroken run (SquareEnds: the runs overlap at corners).
    //   - Curved and chamfered rooms follow their outline (LevelRoom.Outline).
    //   - With ChamferCorridorBends, the outside corner of each corridor bend is cut at 45°,
    //     half a tile back along both walls.
    // Cuts are the triangles a chamfer removes (between the diagonal and the old corner).
    public class LevelWallPlan
    {
        public readonly struct Segment
        {
            public Segment(Vector2 from, Vector2 to, float height, bool squareEnds, LevelRoom room)
            {
                From = from; To = to; Height = height; SquareEnds = squareEnds; Room = room;
            }

            public Vector2 From       { get; }
            public Vector2 To         { get; }
            public float   Height     { get; }   // metres
            // Tile-edge runs reach a full wall thickness past each end so corners close;
            // outline pieces meet at angles and only need half.
            public bool    SquareEnds { get; }
            // The room the wall bounds; null for a corridor wall.
            public LevelRoom Room     { get; }
        }

        private const float CorridorCut = 0.5f;

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public List<Segment> Segments { get; } = new();
        public List<(Vector2 a, Vector2 corner, Vector2 b)> Cuts { get; } = new();

        public static LevelWallPlan Create(LevelLayout layout, LevelBuildSettings settings)
        {
            var plan = new LevelWallPlan();
            var bends = settings.ChamferCorridorBends ? CorridorBends(layout) : new Dictionary<Vector2Int, (Vector2Int, Vector2Int)>();

            plan.AddTileEdgeRuns(layout, settings.WallHeight, bends);
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Outline == null) continue;
                foreach ((Vector2 from, Vector2 to) in room.Outline.Segments)
                    plan.Segments.Add(new Segment(from, to, room.WallHeight, squareEnds: false, room));
                plan.Cuts.AddRange(room.Outline.Cuts);
            }
            foreach (var (tile, (a, b)) in bends)
                plan.AddBendChamfer(tile, a, b, settings.WallHeight);
            return plan;
        }

        // Corridor tiles with exactly two open sides at right angles, keyed to those sides'
        // opposites: the two walls that meet at the bend's outside corner.
        private static Dictionary<Vector2Int, (Vector2Int, Vector2Int)> CorridorBends(LevelLayout layout)
        {
            var bends = new Dictionary<Vector2Int, (Vector2Int, Vector2Int)>();
            foreach (Vector2Int tile in layout.WalkableTiles)
            {
                if (!layout.IsCorridor(tile)) continue;

                Vector2Int open = Vector2Int.zero;
                int count = 0;
                foreach (Vector2Int side in Sides)
                    if (layout.IsWalkable(tile + side)) { open += side; count++; }

                // Two open sides at right angles sum to a diagonal.
                if (count == 2 && open.x != 0 && open.y != 0)
                    bends[tile] = (new Vector2Int(-open.x, 0), new Vector2Int(0, -open.y));
            }
            return bends;
        }

        private void AddTileEdgeRuns(LevelLayout layout, float corridorHeight, Dictionary<Vector2Int, (Vector2Int, Vector2Int)> bends)
        {
            // Keyed by room too (null = corridor), so each run belongs to one room.
            var edges = new Dictionary<(int side, int line, float height, LevelRoom room), List<int>>();
            foreach (Vector2Int t in layout.WalkableTiles)
            {
                LevelRoom room = layout.RoomAt(t);
                if (room?.Outline != null) continue;
                float height = room != null ? room.WallHeight : corridorHeight;
                bool isBend = bends.TryGetValue(t, out var cut);

                for (int side = 0; side < Sides.Length; side++)
                {
                    Vector2Int dir = Sides[side];
                    if (layout.IsWalkable(t + dir)) continue;
                    if (isBend && (dir == cut.Item1 || dir == cut.Item2)) continue;

                    bool alongY = dir.x != 0;
                    var key = (side, alongY ? t.x : t.y, height, room);
                    if (!edges.TryGetValue(key, out List<int> positions)) edges[key] = positions = new List<int>();
                    positions.Add(alongY ? t.y : t.x);
                }
            }

            foreach (var ((side, line, height, room), positions) in edges)
            {
                positions.Sort();
                int runStart = positions[0];
                for (int i = 1; i <= positions.Count; i++)
                {
                    if (i < positions.Count && positions[i] == positions[i - 1] + 1) continue;
                    AddEdgeRun(Sides[side], line, runStart, positions[i - 1], height, room);
                    if (i < positions.Count) runStart = positions[i];
                }
            }
        }

        // The outer edge of tiles first..last on one line, travelling with the floor on the left.
        private void AddEdgeRun(Vector2Int side, int line, int first, int last, float height, LevelRoom room)
        {
            Vector2 from, to;
            if (side == Vector2Int.down)       { from = new(first, line);       to = new(last + 1, line); }
            else if (side == Vector2Int.right) { from = new(line + 1, first);   to = new(line + 1, last + 1); }
            else if (side == Vector2Int.up)    { from = new(last + 1, line + 1); to = new(first, line + 1); }
            else                               { from = new(line, last + 1);    to = new(line, first); }
            Segments.Add(new Segment(from, to, height, squareEnds: true, room));
        }

        // The bend tile's two outer walls, each stopping half a tile short of their shared
        // corner, joined by a diagonal.
        private void AddBendChamfer(Vector2Int tile, Vector2Int sideA, Vector2Int sideB, float height)
        {
            Vector2 centre = tile + Vector2.one * 0.5f;
            Vector2 corner = centre + ((Vector2)(sideA + sideB)) * 0.5f;

            // Walk the outline with the floor on the left: along sideA's wall toward the
            // corner, or along sideB's — whichever direction keeps the floor on the left.
            Vector2 alongA = Left(sideA);   // travel direction on sideA's wall
            bool aFirst = Vector2.Dot(alongA, sideB) > 0f;
            Vector2Int first = aFirst ? sideA : sideB, second = aFirst ? sideB : sideA;
            Vector2 dirFirst = Left(first), dirSecond = Left(second);

            Vector2 firstStart = corner - dirFirst;                      // far end of the first wall
            Vector2 cutA = corner - dirFirst * CorridorCut;
            Vector2 cutB = corner + dirSecond * CorridorCut;
            Vector2 secondEnd = corner + dirSecond;

            Segments.Add(new Segment(firstStart, cutA, height, squareEnds: true, room: null));
            Segments.Add(new Segment(cutA, cutB, height, squareEnds: false, room: null));
            Segments.Add(new Segment(cutB, secondEnd, height, squareEnds: true, room: null));
            Cuts.Add((cutA, corner, cutB));
        }

        // Direction of travel along a wall on `side` of a tile with the floor on the left.
        private static Vector2 Left(Vector2Int side) => new(-side.y, side.x);
    }
}
