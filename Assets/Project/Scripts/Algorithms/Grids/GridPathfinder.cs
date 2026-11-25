using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // A* ("A star") pathfinding on a grid: the shortest way from one tile to another round
    // whatever is in the way.
    //
    // How it works: it explores tiles outward from the start, always next exploring the tile
    // with the lowest  cost so far + estimated cost still to go. The cost so far is exact (the
    // steps taken to reach the tile); the estimate (the "heuristic") is the distance as if
    // nothing were in the way, which can never be more than the real remaining cost. That
    // estimate is what makes A* fast: it pulls the search toward the goal instead of spreading
    // evenly in every direction like Dijkstra's algorithm. A MinHeap hands out the cheapest tile
    // next; each tile remembers which tile it was reached from, and when the goal is reached that
    // trail, read backwards, is the path.
    //
    // Why it's useful: units moving on a tile grid (a tactics map, a top-down game), checking
    // that a generated level can be crossed, routing corridors, pipes or rivers. (Characters in
    // the 3D levels use Unity's NavMesh; the corridor router in Level/Layout is a specialised A*.)
    // A step-cost function lets some tiles cost more (mud, danger, a lit area for a sneaking
    // agent) so paths go round them when it's worth it.
    public static class GridPathfinder
    {
        private const float DiagonalCost = 1.41421356f;

        // The tiles from start to goal, both included, or null when the goal can't be reached.
        //   isWalkable       which tiles may be entered (start excepted);
        //   allowDiagonals   also step corner to corner (never squeezing between two blocked tiles);
        //   stepCost         what entering a tile costs, 1 or more (null: 1 everywhere). Costs under
        //                    1 would make the estimate too high and the path no longer the shortest.
        public static List<Vector2Int> FindPath(int width, int height, Vector2Int start, Vector2Int goal,
                                                Func<Vector2Int, bool> isWalkable, bool allowDiagonals = false,
                                                Func<Vector2Int, float> stepCost = null)
        {
            if (!GridNeighbours.InBounds(start, width, height) || !GridNeighbours.InBounds(goal, width, height)) return null;
            if (!isWalkable(goal)) return null;

            int Index(Vector2Int tile) => tile.y * width + tile.x;
            var costSoFar = new float[width * height];
            var cameFrom  = new int[width * height];
            var done      = new bool[width * height];
            for (int i = 0; i < costSoFar.Length; i++) costSoFar[i] = float.MaxValue;

            var open = new MinHeap<Vector2Int>();
            costSoFar[Index(start)] = 0f;
            cameFrom[Index(start)] = -1;
            open.Push(start, Estimate(start, goal, allowDiagonals));

            IReadOnlyList<Vector2Int> steps = allowDiagonals ? GridNeighbours.Eight : GridNeighbours.Four;
            while (open.TryPop(out Vector2Int tile))
            {
                if (tile == goal) return Trace(cameFrom, goal, width);
                if (done[Index(tile)]) continue;   // reached again more cheaply since it was queued
                done[Index(tile)] = true;

                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = tile + step;
                    if (!GridNeighbours.InBounds(next, width, height) || done[Index(next)] || !isWalkable(next)) continue;
                    // No cutting a corner: both tiles beside a diagonal step must be open.
                    if (GridNeighbours.IsDiagonal(step) && (!isWalkable(new Vector2Int(next.x, tile.y)) || !isWalkable(new Vector2Int(tile.x, next.y)))) continue;

                    float cost = costSoFar[Index(tile)] + (GridNeighbours.IsDiagonal(step) ? DiagonalCost : 1f) * (stepCost?.Invoke(next) ?? 1f);
                    if (cost >= costSoFar[Index(next)]) continue;
                    costSoFar[Index(next)] = cost;
                    cameFrom[Index(next)] = Index(tile);
                    open.Push(next, cost + Estimate(next, goal, allowDiagonals));
                }
            }
            return null;
        }

        // The cost if nothing were in the way: steps along the grid ("Manhattan" distance), or
        // with diagonals, diagonal steps first and straight ones for the rest ("octile" distance).
        private static float Estimate(Vector2Int from, Vector2Int to, bool allowDiagonals)
        {
            int dx = Mathf.Abs(to.x - from.x), dy = Mathf.Abs(to.y - from.y);
            if (!allowDiagonals) return dx + dy;
            return Mathf.Max(dx, dy) + (DiagonalCost - 1f) * Mathf.Min(dx, dy);
        }

        // Follows each tile back to the one it was reached from, then turns the list round.
        private static List<Vector2Int> Trace(int[] cameFrom, Vector2Int goal, int width)
        {
            var path = new List<Vector2Int>();
            for (int index = goal.y * width + goal.x; index >= 0; index = cameFrom[index])
                path.Add(new Vector2Int(index % width, index / width));
            path.Reverse();
            return path;
        }
    }
}
