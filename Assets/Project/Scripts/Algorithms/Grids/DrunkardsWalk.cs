using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Algorithms
{
    // The drunkard's walk (a random walk): start on a tile and stagger a random step at a time,
    // remembering every tile you stand on.
    //
    // How it works: each step picks one of the four directions at random and moves there (unless
    // `isInside` says that tile is off limits, in which case it tries again next step). The tiles
    // visited form a winding, blobby, always-connected shape, because every tile was reached by
    // walking from the one before.
    //
    // Why it's useful: the simplest way to grow something connected and organic. Carve caves or
    // tunnels out of rock (every carved tile is reachable from the start), spread creep, corruption
    // or rubble out from a point, scatter a trail of footprints or blood, or let a "drunk" agent
    // wander. Several shorter walks from different starts give a more even spread than one long one.
    public static class DrunkardsWalk
    {
        // The tiles visited by a walk of `steps` steps from `start` (the start included).
        public static HashSet<Vector2Int> Walk(Vector2Int start, int steps, RandomStream random, Func<Vector2Int, bool> isInside = null)
        {
            var visited = new HashSet<Vector2Int> { start };
            Vector2Int current = start;
            for (int step = 0; step < steps; step++)
            {
                Vector2Int next = current + random.Pick(GridNeighbours.Four);
                if (isInside != null && !isInside(next)) continue;
                current = next;
                visited.Add(current);
            }
            return visited;
        }
    }
}
