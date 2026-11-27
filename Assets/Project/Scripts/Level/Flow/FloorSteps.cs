using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Steps from a floor tile (or the nearest of several) to every other over a room's floor
    // plan, around inner walls and whatever the caller counts as in the way. An array over the
    // footprint's bounds, read with IndexOf; Unreached where no way leads. Shared by the room's
    // flow (from the arrival), the furnishing's routes (from every walkway) and the Room Lab's
    // reachability check (from every doorway).
    internal sealed class FloorSteps
    {
        public const int Unreached = int.MaxValue;

        private readonly TileGrid _grid;
        private readonly int[] _steps;

        // `solid`: tiles carrying any of these tags can't be walked (Structure alone measures the
        // floor plan, which props don't change). `blocked`: further tiles to walk round (null = none).
        public FloorSteps(RoomStructure structure, Vector2Int from, RoomTileTags solid, ICollection<Vector2Int> blocked = null)
            : this(structure, new[] { from }, solid, blocked) { }

        // From whichever of `sources` is nearest; sources off the floor are skipped.
        public FloorSteps(RoomStructure structure, IEnumerable<Vector2Int> sources, RoomTileTags solid, ICollection<Vector2Int> blocked = null)
        {
            _grid   = new TileGrid(structure.Footprint.Bounds);
            _steps  = new int[_grid.Count];
            System.Array.Fill(_steps, Unreached);

            var open = new Queue<Vector2Int>();
            foreach (Vector2Int source in sources)
            {
                if (!structure.Contains(source) || _steps[IndexOf(source)] == 0) continue;
                _steps[IndexOf(source)] = 0;
                open.Enqueue(source);
            }
            HasSources = open.Count > 0;

            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                int next = _steps[IndexOf(tile)] + 1;
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int neighbour = tile + side;
                    if (!structure.Contains(neighbour) || _steps[IndexOf(neighbour)] != Unreached) continue;
                    if (structure.Has(neighbour, solid) || !structure.CanStep(tile, neighbour)) continue;
                    if (blocked != null && blocked.Contains(neighbour)) continue;
                    _steps[IndexOf(neighbour)] = next;
                    open.Enqueue(neighbour);
                }
                // Round a corner through a diagonal wall's open half, as RoomStructure.StaysConnected walks.
                if (structure.Diagonals.Count == 0) continue;
                foreach (Vector2Int corner in GridNeighbours.Eight)
                {
                    Vector2Int neighbour = tile + corner;
                    if (!GridNeighbours.IsDiagonal(corner) || !structure.Contains(neighbour) || _steps[IndexOf(neighbour)] != Unreached) continue;
                    if (structure.Has(neighbour, solid) || !structure.CanPassHalf(tile, neighbour)) continue;
                    if (blocked != null && blocked.Contains(neighbour)) continue;
                    _steps[IndexOf(neighbour)] = next;
                    open.Enqueue(neighbour);
                }
            }
        }

        // Whether any source was on the floor to measure from.
        public bool HasSources { get; }

        public int At(Vector2Int tile) => _grid.Contains(tile) ? _steps[IndexOf(tile)] : Unreached;

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
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                previous = tile + side;
                if (At(previous) == At(tile) - 1 && structure.CanStep(previous, tile)) return true;
            }
            previous = tile;
            return false;
        }

        private int IndexOf(Vector2Int tile) => _grid.IndexOf(tile);
    }
}
