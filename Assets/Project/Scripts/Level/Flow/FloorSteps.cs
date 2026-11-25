using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Steps from one floor tile to every other over a room's floor plan, around inner walls and
    // whatever the caller counts as in the way. An array over the footprint's bounds, read with
    // IndexOf; Unreached where no way leads.
    internal sealed class FloorSteps
    {
        public const int Unreached = int.MaxValue;

        private readonly RectInt _bounds;
        private readonly int[] _steps;

        // `solid`: tiles carrying any of these tags can't be walked (Structure alone measures the
        // floor plan, which props don't change). `blocked`: further tiles to walk round (null = none).
        public FloorSteps(RoomStructure structure, Vector2Int from, RoomTileTags solid, ICollection<Vector2Int> blocked = null)
        {
            _bounds = structure.Footprint.Bounds;
            _steps  = new int[_bounds.width * _bounds.height];
            System.Array.Fill(_steps, Unreached);
            if (!structure.Contains(from)) return;

            var open = new Queue<Vector2Int>();
            _steps[IndexOf(from)] = 0;
            open.Enqueue(from);
            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                int next = _steps[IndexOf(tile)] + 1;
                foreach (Vector2Int side in GridSides.All)
                {
                    Vector2Int neighbour = tile + side;
                    if (!structure.Contains(neighbour) || _steps[IndexOf(neighbour)] != Unreached) continue;
                    if (structure.Has(neighbour, solid) || !structure.CanStep(tile, neighbour)) continue;
                    if (blocked != null && blocked.Contains(neighbour)) continue;
                    _steps[IndexOf(neighbour)] = next;
                    open.Enqueue(neighbour);
                }
            }
        }

        public int At(Vector2Int tile) => _bounds.Contains(tile) ? _steps[IndexOf(tile)] : Unreached;

        public bool Reaches(Vector2Int tile) => At(tile) != Unreached;

        // The farthest any reached tile lies (0 when only the start was reached).
        public int Farthest()
        {
            int farthest = 0;
            foreach (int steps in _steps)
                if (steps != Unreached && steps > farthest) farthest = steps;
            return farthest;
        }

        // A shortest way from the start to `to`, `to` first: each step a neighbour one step nearer.
        public List<Vector2Int> PathTo(Vector2Int to, RoomStructure structure)
        {
            var path = new List<Vector2Int>();
            if (!Reaches(to)) return path;

            Vector2Int tile = to;
            path.Add(tile);
            while (At(tile) > 0 && TryStepBack(tile, structure, out tile)) path.Add(tile);
            return path;
        }

        private bool TryStepBack(Vector2Int tile, RoomStructure structure, out Vector2Int previous)
        {
            foreach (Vector2Int side in GridSides.All)
            {
                previous = tile + side;
                if (At(previous) == At(tile) - 1 && structure.CanStep(previous, tile)) return true;
            }
            previous = tile;
            return false;
        }

        private int IndexOf(Vector2Int tile) => (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);
    }
}
