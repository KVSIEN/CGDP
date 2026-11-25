using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // A distance map (in roguelikes, a "Dijkstra map"): for every tile of a grid, how many steps
    // it is from the nearest of some source tiles, walking round anything in the way.
    //
    // How it works: a breadth-first search. The sources start at distance 0 in a queue. Take the
    // first tile off the queue; each walkable neighbour not reached yet is one step further, and
    // joins the end of the queue. Because the queue is handled in order, tiles are reached in
    // order of distance, so the first time a tile is reached is by its shortest way.
    //
    // Why it's useful: one map answers questions for every tile at once, which is cheaper than a
    // path search per agent.
    //   - Chase: from any tile, step to the neighbour with a lower number (TryStepToward) and you
    //     follow a shortest path to the nearest source (the player, an exit, a loot pile).
    //   - Flee: step to a higher number (TryStepAway).
    //   - Design: "how far is each tile from a doorway?", so spawns, loot or growth can keep away
    //     from it or gather near it. Unreachable tiles show up as cut-off floor.
    // Several sources at once give the distance to whichever is nearest.
    public sealed class GridDistanceMap
    {
        public const int Unreachable = -1;

        private readonly int[] _distance;

        private GridDistanceMap(int width, int height)
        {
            Width     = width;
            Height    = height;
            _distance = new int[width * height];
            for (int i = 0; i < _distance.Length; i++) _distance[i] = Unreachable;
        }

        public int Width  { get; }
        public int Height { get; }

        // Tiles run from (0, 0) to (width - 1, height - 1); the four side neighbours count as one step.
        public static GridDistanceMap Build(int width, int height, IEnumerable<Vector2Int> sources, Func<Vector2Int, bool> isWalkable)
        {
            var map = new GridDistanceMap(width, height);
            var queue = new Queue<Vector2Int>();
            foreach (Vector2Int source in sources)
            {
                if (!GridNeighbours.InBounds(source, width, height) || map.DistanceAt(source) == 0) continue;
                map.Set(source, 0);
                queue.Enqueue(source);
            }

            while (queue.Count > 0)
            {
                Vector2Int tile = queue.Dequeue();
                int next = map.DistanceAt(tile) + 1;
                foreach (Vector2Int step in GridNeighbours.Four)
                {
                    Vector2Int neighbour = tile + step;
                    if (!GridNeighbours.InBounds(neighbour, width, height) || map.DistanceAt(neighbour) != Unreachable) continue;
                    if (!isWalkable(neighbour)) continue;
                    map.Set(neighbour, next);
                    queue.Enqueue(neighbour);
                }
            }
            return map;
        }

        // Steps from the tile to the nearest source, or Unreachable (also off the grid).
        public int DistanceAt(Vector2Int tile) =>
            GridNeighbours.InBounds(tile, Width, Height) ? _distance[tile.y * Width + tile.x] : Unreachable;

        // The neighbour one step closer to a source; false at a source or where nothing is reachable.
        public bool TryStepToward(Vector2Int from, out Vector2Int next) => TryStep(from, closer: true, out next);

        // The neighbour one step further from every source; false where none is further.
        public bool TryStepAway(Vector2Int from, out Vector2Int next) => TryStep(from, closer: false, out next);

        private bool TryStep(Vector2Int from, bool closer, out Vector2Int next)
        {
            next = from;
            int best = DistanceAt(from);
            if (best == Unreachable) return false;

            foreach (Vector2Int step in GridNeighbours.Four)
            {
                int distance = DistanceAt(from + step);
                if (distance == Unreachable) continue;
                if (closer ? distance < best : distance > best)
                {
                    best = distance;
                    next = from + step;
                }
            }
            return next != from;
        }

        private void Set(Vector2Int tile, int distance) => _distance[tile.y * Width + tile.x] = distance;
    }
}
