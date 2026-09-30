using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The wall line of a room with curved walls, in tile units (tile corners are whole
    // numbers). The tile outline is traced into loops of straight runs; runs shorter than
    // LongRun are the stair-steps of a curve and are replaced by diagonals through their
    // midpoints, so a dome gets 45° walls instead of stairs. Long runs stay on the tile
    // edges — that's where doorways sit — minus the openings `isOpening` reports.
    public static class RoomOutline
    {
        private const int LongRun = 3;

        public static List<(Vector2 from, Vector2 to)> Build(RoomFootprint footprint, Func<Vector2Int, Vector2Int, bool> isOpening)
        {
            var segments = new List<(Vector2, Vector2)>();
            foreach (List<Run> loop in TraceLoops(footprint))
            {
                var loopSegments = new List<(Vector2, Vector2)>();
                AddLoop(loop, isOpening, loopSegments);
                segments.AddRange(MergeCollinear(loopSegments));
            }
            return segments;
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

        private static void AddLoop(List<Run> runs, Func<Vector2Int, Vector2Int, bool> isOpening, List<(Vector2, Vector2)> segments)
        {
            int count = runs.Count;
            for (int i = 0; i < count; i++)
            {
                Run run = runs[i], next = runs[(i + 1) % count], previous = runs[(i + count - 1) % count];

                float trimStart = run.IsLong && !previous.IsLong ? 0.5f : 0f;
                float trimEnd   = run.IsLong && !next.IsLong     ? 0.5f : 0f;
                if (run.IsLong) AddStraight(run, trimStart, trimEnd, isOpening, segments);

                Vector2 end   = run.IsLong  ? run.End - (Vector2)run.Dir * trimEnd : run.Mid;
                Vector2 start = next.IsLong ? next.Start + (Vector2)next.Dir * (!run.IsLong ? 0.5f : 0f) : next.Mid;
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
