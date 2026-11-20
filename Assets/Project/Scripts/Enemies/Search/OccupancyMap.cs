using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Where a lost target might be, as Damián Isla's occupancy map: each walkable cell of the
    // grid holds a share of the chance the target is there. Seeded where it was last known,
    // leaning the way it was heading; the chance spreads to joined cells as time passes (it
    // could have moved on) but never through walls; a cell a searcher looks at and finds empty
    // loses its share. The shares are not topped back up, so Remaining — what is left of the
    // chance the target is still round here — falls as the search covers the likely ground,
    // and a search can give up when it is low.
    internal sealed class OccupancyMap
    {
        // Of the seed, the share put where the target was last known; the rest goes ahead of it.
        private const float LastKnownShare = 0.55f;
        private const int   HeadingCells   = 3;
        private const float Negligible     = 1e-4f;

        private readonly OccupancyGrid _grid;
        private readonly float[] _chance;
        private readonly float[] _next;
        private readonly float[] _favour;
        private readonly int[] _neighbours = new int[4];

        public OccupancyMap(OccupancyGrid grid)
        {
            _grid   = grid;
            _chance = new float[grid.Count];
            _next   = new float[grid.Count];
            _favour = new float[grid.Count];
            for (int i = 0; i < _favour.Length; i++) _favour[i] = 1f;
        }

        public OccupancyGrid Grid => _grid;
        public float Remaining { get; private set; }

        public float Chance(int cell) => _chance[cell];

        // Starts over: the whole chance at `lost` and a few cells along `heading` (flat; zero =
        // no lean). False when no walkable cell is near enough to put it in.
        public bool Seed(Vector3 lost, Vector3 heading)
        {
            System.Array.Clear(_chance, 0, _chance.Length);
            Remaining = 0f;
            if (!TryNearestWalkable(lost, out int start)) return false;

            Vector3 lean = new Vector3(heading.x, 0f, heading.z);
            float ahead = lean.sqrMagnitude > 0.01f ? (1f - LastKnownShare) / HeadingCells : 0f;
            _chance[start] = 1f - ahead * HeadingCells;
            for (int i = 1; i <= HeadingCells && ahead > 0f; i++)
            {
                Vector3 point = _grid.Center(start) + lean.normalized * (i * _grid.CellSize);
                int cell = _grid.TryCell(point, out int found) && _grid.Walkable(found) ? found : start;
                _chance[cell] += ahead;
            }
            Remaining = 1f;
            return true;
        }

        // One step of spreading: each cell passes `rate` of its share, split evenly, to the
        // cells joined to it. Nothing is lost; a cell with no open neighbour keeps it all.
        public void Diffuse(float rate)
        {
            System.Array.Clear(_next, 0, _next.Length);
            for (int cell = 0; cell < _chance.Length; cell++)
            {
                float share = _chance[cell];
                if (share <= 0f) continue;

                int count = _grid.Neighbours(cell, _neighbours);
                float given = count > 0 ? share * rate : 0f;
                _next[cell] += share - given;
                for (int i = 0; i < count; i++) _next[_neighbours[i]] += given / count;
            }
            System.Array.Copy(_next, _chance, _chance.Length);
        }

        // A searcher saw this cell and nobody was there.
        public void Clear(int cell)
        {
            Remaining = Mathf.Max(0f, Remaining - _chance[cell]);
            _chance[cell] = 0f;
        }

        // Cells within `radius` of a point (a searcher standing there sees round itself).
        public void ClearAround(Vector3 point, float radius)
        {
            if (!_grid.TryCell(point, out int middle)) return;
            int reach = Mathf.CeilToInt(radius / _grid.CellSize);
            int mx = middle % _grid.Width, mz = middle / _grid.Width;
            for (int z = Mathf.Max(0, mz - reach); z <= Mathf.Min(_grid.Depth - 1, mz + reach); z++)
                for (int x = Mathf.Max(0, mx - reach); x <= Mathf.Min(_grid.Width - 1, mx + reach); x++)
                {
                    int cell = x + z * _grid.Width;
                    if (Flat(_grid.Center(cell) - point).magnitude <= radius) Clear(cell);
                }
        }

        // Somewhere a target would rather be (behind cover): that cell is checked sooner.
        public void Favour(Vector3 point, float factor)
        {
            if (_grid.TryCell(point, out int cell) && _grid.Walkable(cell)) _favour[cell] = Mathf.Max(_favour[cell], factor);
        }

        // The cell most worth walking to from `from`: the likeliest, favoured cover counting
        // extra, nearer cells preferred (a cell `distanceScale` metres off counts half), and
        // none within `skip` metres (already in view from here) or for which `passOver` says so.
        // False when nothing is left.
        public bool TryBest(Vector3 from, float distanceScale, float skip, out int best, System.Predicate<int> passOver = null)
        {
            best = -1;
            float bestScore = 0f;
            for (int cell = 0; cell < _chance.Length; cell++)
            {
                if (_chance[cell] <= Negligible || (passOver != null && passOver(cell))) continue;
                float distance = Flat(_grid.Center(cell) - from).magnitude;
                if (distance < skip) continue;

                float score = _chance[cell] * _favour[cell] / (1f + distance / distanceScale);
                if (score <= bestScore) continue;
                bestScore = score;
                best = cell;
            }
            return best >= 0;
        }

        // The `count` likeliest cells, likeliest first (the ones worth a look this moment).
        public void Likeliest(List<int> into, int count)
        {
            into.Clear();
            for (int cell = 0; cell < _chance.Length; cell++)
            {
                if (_chance[cell] <= Negligible) continue;
                int at = into.Count;
                while (at > 0 && _chance[into[at - 1]] < _chance[cell]) at--;
                if (at >= count) continue;
                into.Insert(at, cell);
                if (into.Count > count) into.RemoveAt(count);
            }
        }

        private bool TryNearestWalkable(Vector3 point, out int nearest)
        {
            nearest = -1;
            float best = float.MaxValue;
            for (int cell = 0; cell < _grid.Count; cell++)
            {
                if (!_grid.Walkable(cell)) continue;
                float distance = Flat(_grid.Center(cell) - point).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = cell;
            }
            return nearest >= 0;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
