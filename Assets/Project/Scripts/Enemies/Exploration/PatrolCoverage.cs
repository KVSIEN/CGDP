using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Where an enemy has walked and when: a grid of cells with the time each was last
    // passed through. Ground never walked, or not for a long while, is stale, and stale ground
    // is what draws an exploring enemy (PatrolScoring). Kin close by swap what they have
    // covered so a squad spreads out rather than all combing the same corner. Plain data, no
    // physics.
    internal sealed class PatrolCoverage
    {
        public const float CellSize = 4f;
        public const int   Capacity = 400;

        private readonly Dictionary<Vector2Int, float> _visited = new();

        public int Count => _visited.Count;

        public void Visit(Vector3 position, float time)
        {
            Vector2Int cell = CellOf(position);
            if (!_visited.ContainsKey(cell) && _visited.Count >= Capacity) ForgetOldest();
            _visited[cell] = time;
        }

        // 1 for ground never visited, falling to 0 for ground visited just now, and back up
        // to 1 over forgetAfter seconds.
        public float Staleness(Vector3 position, float time, float forgetAfter)
        {
            if (!_visited.TryGetValue(CellOf(position), out float last)) return 1f;
            return Mathf.Clamp01((time - last) / Mathf.Max(1f, forgetAfter));
        }

        // Tells `other` what this one has covered; it keeps whichever visit was later.
        public void ShareWith(PatrolCoverage other)
        {
            if (other == this) return;
            foreach (KeyValuePair<Vector2Int, float> cell in _visited)
            {
                if (other._visited.TryGetValue(cell.Key, out float theirs) && theirs >= cell.Value) continue;
                if (!other._visited.ContainsKey(cell.Key) && other._visited.Count >= Capacity) other.ForgetOldest();
                other._visited[cell.Key] = cell.Value;
            }
        }

        public void Forget() => _visited.Clear();

        // The cells it has covered, as the middle of each with the time it was last passed.
        public void CopyTo(List<(Vector3 Centre, float Time)> results)
        {
            results.Clear();
            foreach (KeyValuePair<Vector2Int, float> cell in _visited)
                results.Add((new Vector3(cell.Key.x * CellSize, 0f, cell.Key.y * CellSize), cell.Value));
        }

        private static Vector2Int CellOf(Vector3 position) =>
            new(Mathf.RoundToInt(position.x / CellSize), Mathf.RoundToInt(position.z / CellSize));

        private void ForgetOldest()
        {
            Vector2Int oldest = default;
            float earliest = float.MaxValue;
            foreach (KeyValuePair<Vector2Int, float> cell in _visited)
            {
                if (cell.Value >= earliest) continue;
                earliest = cell.Value;
                oldest   = cell.Key;
            }
            _visited.Remove(oldest);
        }
    }
}
