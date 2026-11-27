using System.Collections.Generic;
using CGD.Algorithms;
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
    //   - With diagonal corridors (DiagonalCorridorSteps), a staircase of bends (CorridorCorners)
    //     is walled by one straight 45° wall each side, through those same chamfers: the
    //     corridor runs diagonally, 1.4 tiles wide across, its walls meeting the straight
    //     stretches either end at 45°.
    // Cuts are the triangles a chamfer removes (between the diagonal and the old corner); Fills
    // the triangles a diagonal wall adds to the floor, in the notches of the tile staircase.
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

        public List<Segment> Segments { get; } = new();
        public List<(Vector2 a, Vector2 corner, Vector2 b)> Cuts { get; } = new();
        public List<(Vector2 a, Vector2 corner, Vector2 b)> Fills { get; } = new();

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
            var straight = new HashSet<Vector2Int>();
            if (settings.DiagonalCorridorSteps > 0)
                foreach (List<Vector2Int> line in DiagonalLines(bends))
                {
                    plan.AddDiagonal(line, bends, settings.WallHeight);
                    straight.UnionWith(line);
                }
            foreach (var (tile, (a, b)) in bends)
                if (!straight.Contains(tile)) plan.AddBendChamfer(tile, a, b, settings.WallHeight);
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
                foreach (Vector2Int side in GridNeighbours.Four)
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

                for (int side = 0; side < GridNeighbours.Four.Length; side++)
                {
                    Vector2Int dir = GridNeighbours.Four[side];
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
                    AddEdgeRun(GridNeighbours.Four[side], line, runStart, positions[i - 1], height, room);
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

        // Bends whose chamfers line up on one 45° wall, in order along it, two or more to a line:
        // each the next's neighbour corner to corner, both cut on the same side, with the bend
        // between them (the stair's riser) cut on the other. A lone bend keeps its own chamfer.
        private static List<List<Vector2Int>> DiagonalLines(Dictionary<Vector2Int, (Vector2Int, Vector2Int)> bends)
        {
            bool Continues(Vector2Int tile, Vector2Int along, Vector2Int outward)
            {
                Vector2Int next = tile + along;
                if (!bends.TryGetValue(next, out var cut) || cut.Item1 + cut.Item2 != outward) return false;
                // The riser: the neighbour the line steps through, away from the cut side.
                Vector2Int riser = tile + (along.x == outward.x ? new Vector2Int(0, along.y) : new Vector2Int(along.x, 0));
                return bends.TryGetValue(riser, out var riserCut) && riserCut.Item1 + riserCut.Item2 == -outward;
            }

            var lines = new List<List<Vector2Int>>();
            // Bends in a fixed order (by tile), so the plan never depends on a set's order.
            var tiles = new List<Vector2Int>(bends.Keys);
            tiles.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            foreach (Vector2Int tile in tiles)
            {
                var (sideA, sideB) = bends[tile];
                Vector2Int outward = sideA + sideB;
                var along = new Vector2Int(-outward.y, outward.x);
                if (Continues(tile, -along, outward)) continue;   // not the line's first

                var line = new List<Vector2Int> { tile };
                while (Continues(line[line.Count - 1], along, outward)) line.Add(line[line.Count - 1] + along);
                if (line.Count >= 2) lines.Add(line);
            }
            return lines;
        }

        // One line of bends walled as a single diagonal: the first chamfer's stub along the
        // straight wall before it, a wall from there to the last chamfer's end, and the last's stub
        // after it. Each chamfer still cuts its corner; each notch the wall crosses is filled.
        private void AddDiagonal(List<Vector2Int> line, Dictionary<Vector2Int, (Vector2Int, Vector2Int)> bends, float height)
        {
            var chamfers = new List<(Vector2 start, Vector2 a, Vector2 corner, Vector2 b, Vector2 end)>(line.Count);
            foreach (Vector2Int tile in line)
                chamfers.Add(Chamfer(tile, bends[tile].Item1, bends[tile].Item2));

            // In the direction the wall runs, the floor on its left.
            Vector2 travel = chamfers[0].b - chamfers[0].a;
            if (Vector2.Dot(travel, line[line.Count - 1] - line[0]) < 0f) chamfers.Reverse();

            var first = chamfers[0];
            var last  = chamfers[chamfers.Count - 1];
            Segments.Add(new Segment(first.start, first.a, height, squareEnds: true, room: null));
            Segments.Add(new Segment(first.a, last.b, height, squareEnds: false, room: null));
            Segments.Add(new Segment(last.b, last.end, height, squareEnds: true, room: null));
            for (int i = 0; i < chamfers.Count; i++)
            {
                Cuts.Add((chamfers[i].a, chamfers[i].corner, chamfers[i].b));
                // The notch between two chamfers: its inside corner is where their tile edges meet.
                if (i == 0) continue;
                Vector2 from = chamfers[i - 1].b, to = chamfers[i].a;
                var notch = new Vector2(from.x, to.y);
                Fills.Add((from, IsCorner(notch) ? notch : new Vector2(to.x, from.y), to));
            }
        }

        private static bool IsCorner(Vector2 point) =>
            Mathf.Approximately(point.x, Mathf.Round(point.x)) && Mathf.Approximately(point.y, Mathf.Round(point.y));

        // The bend tile's two outer walls, each stopping half a tile short of their shared
        // corner, joined by a diagonal.
        private void AddBendChamfer(Vector2Int tile, Vector2Int sideA, Vector2Int sideB, float height)
        {
            var (firstStart, cutA, corner, cutB, secondEnd) = Chamfer(tile, sideA, sideB);
            Segments.Add(new Segment(firstStart, cutA, height, squareEnds: true, room: null));
            Segments.Add(new Segment(cutA, cutB, height, squareEnds: false, room: null));
            Segments.Add(new Segment(cutB, secondEnd, height, squareEnds: true, room: null));
            Cuts.Add((cutA, corner, cutB));
        }

        // A bend's chamfer, in the order the wall runs with the floor on its left: the far end of
        // the first outer wall, where the cut leaves it, the corner it cuts off, where it meets the
        // second wall, and that wall's far end.
        private static (Vector2 start, Vector2 a, Vector2 corner, Vector2 b, Vector2 end) Chamfer(Vector2Int tile, Vector2Int sideA, Vector2Int sideB)
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
            return (firstStart, cutA, corner, cutB, secondEnd);
        }

        // Direction of travel along a wall on `side` of a tile with the floor on the left.
        private static Vector2 Left(Vector2Int side) => new(-side.y, side.x);
    }
}
