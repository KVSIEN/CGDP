using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The wall line of a room whose walls leave the tile edges, in tile units (tile corners
    // are whole numbers). The tile outline is traced into loops of straight runs.
    //   Curved rooms: runs shorter than LongRun are the stair-steps of a curve and are
    //   replaced by diagonals through their midpoints, so a dome gets 45° walls, not stairs.
    //   `smoothing` rounds them further: each pass cuts every bend in two equal halves
    //   (corner cutting, at most MaxSmoothingCut tiles back), so the walls become an arc of
    //   short pieces — two passes turn 45° kinks into 11° ones. Wall ends at a doorway never move.
    //   Chamfered rooms: each outside corner where two long runs meet is cut at 45°,
    //   `chamfer` tiles back along both — unless a doorway is that close to it.
    // Long runs otherwise stay on the tile edges (that's where doorways sit), minus the
    // openings `isOpening` reports.
    public static class RoomOutline
    {
        private const int LongRun = 3;
        public const int MaxChamfer = 2;
        public const int MaxSmoothing = 3;
        private const float MaxSmoothingCut = 0.5f;

        public static List<(Vector2 from, Vector2 to)> Build(RoomFootprint footprint, Func<Vector2Int, Vector2Int, bool> isOpening,
                                                             int chamfer = 0, int smoothing = 0) =>
            Plan(footprint, isOpening, chamfer, smoothing).Segments;

        public static RoomOutlinePlan Plan(RoomFootprint footprint, Func<Vector2Int, Vector2Int, bool> isOpening,
                                           int chamfer = 0, int smoothing = 0)
        {
            chamfer   = Mathf.Clamp(chamfer, 0, MaxChamfer);
            smoothing = footprint.CurvedWalls ? Mathf.Clamp(smoothing, 0, MaxSmoothing) : 0;
            var plan = new RoomOutlinePlan();
            foreach (List<Run> loop in TraceLoops(footprint))
            {
                var cuts = CornerCuts(loop, chamfer, isOpening, plan);
                var loopSegments = new List<(Vector2, Vector2)>();
                AddLoop(loop, cuts, isOpening, loopSegments);
                List<(Vector2, Vector2)> merged = MergeCollinear(loopSegments);
                plan.Segments.AddRange(smoothing > 0 ? Smooth(merged, smoothing) : merged);
            }
            return plan;
        }

        // Corner cutting over each joined chain of a loop's segments. A loop broken
        // by doorways is several open chains whose ends stay put; an unbroken loop is closed.
        private static List<(Vector2, Vector2)> Smooth(List<(Vector2 from, Vector2 to)> segments, int passes)
        {
            var result = new List<(Vector2, Vector2)>();
            foreach (List<Vector2> chain in Chains(segments, out bool closed))
            {
                List<Vector2> points = chain;
                for (int pass = 0; pass < passes; pass++)
                    points = CutCorners(points, closed);

                int edges = closed ? points.Count : points.Count - 1;
                for (int i = 0; i < edges; i++)
                    result.Add((points[i], points[(i + 1) % points.Count]));
            }
            return result;
        }

        // The segments as point chains, split wherever one doesn't start at the previous end.
        // Starts at a chain's first segment so none is split across the list's ends.
        private static List<List<Vector2>> Chains(List<(Vector2 from, Vector2 to)> segments, out bool closed)
        {
            int count = segments.Count, start = 0;
            for (int i = 0; i < count; i++)
                if (!Joins(segments[(i + count - 1) % count], segments[i])) { start = i; break; }

            var chains = new List<List<Vector2>>();
            for (int k = 0; k < count; k++)
            {
                var segment = segments[(start + k) % count];
                if (k == 0 || !Joins(segments[(start + k - 1) % count], segment))
                    chains.Add(new List<Vector2> { segment.from });
                chains[chains.Count - 1].Add(segment.to);
            }

            // One chain whose end meets its start: a closed loop (drop the repeated point).
            closed = chains.Count == 1 && count > 1 && Joins(segments[(start + count - 1) % count], segments[start]);
            if (closed) chains[0].RemoveAt(chains[0].Count - 1);
            return chains;
        }

        private static bool Joins((Vector2 from, Vector2 to) a, (Vector2 from, Vector2 to) b) => (a.to - b.from).sqrMagnitude < 1e-6f;

        // Replaces each corner with two points the same distance back along both of its
        // walls (a quarter of the shorter one, at most MaxSmoothingCut), so every pass
        // splits a bend into two equal halves. An open chain's ends stay where they are.
        private static List<Vector2> CutCorners(List<Vector2> points, bool closed)
        {
            var cut = new List<Vector2>();
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                bool end = !closed && (i == 0 || i == count - 1);
                if (end)
                {
                    cut.Add(points[i]);
                    continue;
                }

                Vector2 p = points[i], previous = points[(i + count - 1) % count], next = points[(i + 1) % count];
                float distance = Mathf.Min(MaxSmoothingCut, 0.25f * Mathf.Min((previous - p).magnitude, (next - p).magnitude));
                cut.Add(p + (previous - p).normalized * distance);
                cut.Add(p + (next - p).normalized * distance);
            }
            return cut;
        }

        // cuts[i]: tiles cut off the corner between run i and the next (0 = none). Records
        // each cut's triangle and the tiles it reaches into.
        private static int[] CornerCuts(List<Run> runs, int chamfer, Func<Vector2Int, Vector2Int, bool> isOpening, RoomOutlinePlan plan)
        {
            var cuts = new int[runs.Count];
            if (chamfer == 0) return cuts;

            for (int i = 0; i < runs.Count; i++)
            {
                Run run = runs[i], next = runs[(i + 1) % runs.Count];
                if (!CanCut(run, next, chamfer, isOpening)) continue;

                cuts[i] = chamfer;
                Vector2 corner = run.End;
                plan.Cuts.Add((corner - (Vector2)run.Dir * chamfer, corner, corner + (Vector2)next.Dir * chamfer));

                Vector2Int tile = run.Edges[run.Edges.Count - 1].Tile;
                for (int back = 0; back < chamfer; back++)
                    for (int inward = 0; back + inward < chamfer; inward++)
                        plan.CutTiles.Add(tile - run.Dir * back + next.Dir * inward);
                plan.CornerTiles.Add(tile - run.Dir * chamfer);
                plan.CornerTiles.Add(tile + next.Dir * chamfer);
            }
            return cuts;
        }

        // An outside corner (the outline turns toward the floor, which is on the left)
        // between two long runs, with no doorway on the edges the cut removes.
        private static bool CanCut(Run run, Run next, int chamfer, Func<Vector2Int, Vector2Int, bool> isOpening)
        {
            if (!run.IsLong || !next.IsLong) return false;
            if (next.Dir != new Vector2Int(-run.Dir.y, run.Dir.x)) return false;
            if (run.Edges.Count <= chamfer || next.Edges.Count <= chamfer) return false;

            for (int k = 0; k < chamfer; k++)
            {
                Edge a = run.Edges[run.Edges.Count - 1 - k], b = next.Edges[k];
                if (isOpening(a.Tile, a.Side) || isOpening(b.Tile, b.Side)) return false;
            }
            return true;
        }

        private readonly struct Edge
        {
            public Edge(Vector2Int tile, Vector2Int side, Vector2Int start, Vector2Int dir)
            {
                Tile = tile; Side = side; Start = start; Dir = dir;
            }
            public Vector2Int Tile  { get; }
            public Vector2Int Side  { get; }   // outward, from the tile
            public Vector2Int Start { get; }   // tile-corner the edge starts at
            public Vector2Int Dir   { get; }   // travel direction; the floor is on the left
            public Vector2Int End => Start + Dir;
        }

        private class Run
        {
            public readonly List<Edge> Edges = new();
            public Vector2Int Dir => Edges[0].Dir;
            public Vector2 Start => Edges[0].Start;
            public Vector2 End   => Edges[Edges.Count - 1].End;
            public Vector2 Mid   => (Start + End) * 0.5f;
            public bool IsLong   => Edges.Count >= LongRun;
        }

        private static void AddLoop(List<Run> runs, int[] cuts, Func<Vector2Int, Vector2Int, bool> isOpening, List<(Vector2, Vector2)> segments)
        {
            int count = runs.Count;
            for (int i = 0; i < count; i++)
            {
                Run run = runs[i], next = runs[(i + 1) % count], previous = runs[(i + count - 1) % count];
                int cutBefore = cuts[(i + count - 1) % count], cutAfter = cuts[i];

                float trimStart = (run.IsLong && !previous.IsLong ? 0.5f : 0f) + cutBefore;
                float trimEnd   = (run.IsLong && !next.IsLong     ? 0.5f : 0f) + cutAfter;
                if (run.IsLong) AddStraight(run, trimStart, trimEnd, isOpening, segments);

                Vector2 end   = run.IsLong  ? run.End - (Vector2)run.Dir * trimEnd : run.Mid;
                Vector2 start = next.IsLong ? next.Start + (Vector2)next.Dir * ((!run.IsLong ? 0.5f : 0f) + cutAfter) : next.Mid;
                if (end != start) segments.Add((end, start));
            }
        }

        // A curve's diagonals come out one stair-step at a time; joins pieces that continue
        // in the same direction (the loop's last piece may continue into its first).
        private static List<(Vector2, Vector2)> MergeCollinear(List<(Vector2 from, Vector2 to)> pieces)
        {
            var merged = new List<(Vector2 from, Vector2 to)>();
            foreach (var piece in pieces)
            {
                if (merged.Count > 0 && Continues(merged[merged.Count - 1], piece))
                    merged[merged.Count - 1] = (merged[merged.Count - 1].from, piece.to);
                else
                    merged.Add(piece);
            }
            if (merged.Count > 1 && Continues(merged[merged.Count - 1], merged[0]))
            {
                merged[0] = (merged[merged.Count - 1].from, merged[0].to);
                merged.RemoveAt(merged.Count - 1);
            }
            return merged;
        }

        private static bool Continues((Vector2 from, Vector2 to) a, (Vector2 from, Vector2 to) b)
        {
            if ((a.to - b.from).sqrMagnitude > 1e-6f) return false;
            Vector2 da = (a.to - a.from).normalized, db = (b.to - b.from).normalized;
            return (da - db).sqrMagnitude < 1e-6f;
        }

        // The run's edges from trimStart to (length - trimEnd), skipping openings, merged
        // into as few segments as possible.
        private static void AddStraight(Run run, float trimStart, float trimEnd, Func<Vector2Int, Vector2Int, bool> isOpening, List<(Vector2, Vector2)> segments)
        {
            Vector2 dir = run.Dir;
            float length = run.Edges.Count;
            float from = -1f;
            for (int k = 0; k <= run.Edges.Count; k++)
            {
                bool wall = k < run.Edges.Count && !isOpening(run.Edges[k].Tile, run.Edges[k].Side);
                if (wall && from < 0f) from = Mathf.Max(k, trimStart);
                if (wall || from < 0f) continue;

                float to = Mathf.Min(k, length - trimEnd);
                if (to > from) segments.Add((run.Start + dir * from, run.Start + dir * to));
                from = -1f;
            }
        }

        private static List<List<Run>> TraceLoops(RoomFootprint footprint)
        {
            var byStart = new Dictionary<Vector2Int, List<Edge>>();
            foreach (Vector2Int tile in footprint.Tiles)
                foreach (Edge edge in BoundaryEdges(footprint, tile))
                {
                    if (!byStart.TryGetValue(edge.Start, out List<Edge> list)) byStart[edge.Start] = list = new List<Edge>(1);
                    list.Add(edge);
                }

            var loops = new List<List<Run>>();
            var used = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (Vector2Int tile in footprint.Tiles)
                foreach (Edge first in BoundaryEdges(footprint, tile))
                {
                    if (used.Contains((first.Start, first.Dir))) continue;

                    var edges = new List<Edge>();
                    Edge edge = first;
                    while (used.Add((edge.Start, edge.Dir)))
                    {
                        edges.Add(edge);
                        edge = NextEdge(byStart[edge.End], edge.Dir);
                    }
                    loops.Add(ToRuns(edges));
                }
            return loops;
        }

        // Where two tiles touch only at a corner, two edges leave the same point; turning
        // left keeps each loop around its own tiles.
        private static Edge NextEdge(List<Edge> candidates, Vector2Int dir)
        {
            if (candidates.Count == 1) return candidates[0];
            var left = new Vector2Int(-dir.y, dir.x);
            foreach (Edge candidate in candidates)
                if (candidate.Dir == left) return candidate;
            foreach (Edge candidate in candidates)
                if (candidate.Dir == dir) return candidate;
            return candidates[0];
        }

        private static IEnumerable<Edge> BoundaryEdges(RoomFootprint footprint, Vector2Int t)
        {
            if (!footprint.Contains(t + Vector2Int.down))  yield return new Edge(t, Vector2Int.down,  t,                          Vector2Int.right);
            if (!footprint.Contains(t + Vector2Int.right)) yield return new Edge(t, Vector2Int.right, t + Vector2Int.right,      Vector2Int.up);
            if (!footprint.Contains(t + Vector2Int.up))    yield return new Edge(t, Vector2Int.up,    t + Vector2Int.one,        Vector2Int.left);
            if (!footprint.Contains(t + Vector2Int.left))  yield return new Edge(t, Vector2Int.left,  t + Vector2Int.up,         Vector2Int.down);
        }

        // Groups a loop's edges into straight runs, starting at a corner so no run is split
        // across the loop's ends.
        private static List<Run> ToRuns(List<Edge> edges)
        {
            int offset = 0;
            for (int i = 0; i < edges.Count; i++)
                if (edges[i].Dir != edges[(i + edges.Count - 1) % edges.Count].Dir) { offset = i; break; }

            var runs = new List<Run>();
            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge = edges[(i + offset) % edges.Count];
                if (runs.Count == 0 || runs[runs.Count - 1].Dir != edge.Dir) runs.Add(new Run());
                runs[runs.Count - 1].Edges.Add(edge);
            }
            return runs;
        }
    }
}
