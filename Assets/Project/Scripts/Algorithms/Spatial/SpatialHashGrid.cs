using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // A spatial hash grid: finds the things near a point quickly, without checking every thing.
    //
    // How it works: the plane is cut into square buckets (cells). Adding a thing drops it in the
    // bucket its position falls in. Asking "what is within this radius?" only looks in the few
    // buckets the circle touches, then checks the actual distance of what is in them. Buckets are
    // kept in a dictionary by cell coordinate, so only cells with something in them take memory
    // and the world can be any size.
    //
    // Why it's useful: "everything near everything" is costly. With 500 boids, birds or
    // swarming enemies, checking each against every other is 250,000 checks a frame; with a hash
    // each checks a handful. Use it for flocking and crowds, separation so agents don't overlap,
    // "which enemies hear this noise?", picking up nearby items, or clustering. Pick a cell size
    // about the size of a typical query radius. For moving things, Clear and re-add them each
    // frame (it keeps its lists, so that makes no garbage).
    //
    // 2D: for a 3D world on flat ground, pass (position.x, position.z).
    public sealed class SpatialHashGrid<T>
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector2Int, List<(T item, Vector2 position)>> _cells = new();

        public SpatialHashGrid(float cellSize) => _cellSize = Mathf.Max(0.0001f, cellSize);

        public void Add(T item, Vector2 position)
        {
            Vector2Int cell = CellOf(position);
            if (!_cells.TryGetValue(cell, out List<(T, Vector2)> bucket)) _cells[cell] = bucket = new List<(T, Vector2)>();
            bucket.Add((item, position));
        }

        // Empties every bucket but keeps them, ready to be filled again.
        public void Clear()
        {
            foreach (List<(T, Vector2)> bucket in _cells.Values) bucket.Clear();
        }

        // Fills `results` (cleared first) with everything within `radius` of `centre`.
        public void Query(Vector2 centre, float radius, List<T> results)
        {
            results.Clear();
            Vector2Int min = CellOf(centre - new Vector2(radius, radius));
            Vector2Int max = CellOf(centre + new Vector2(radius, radius));
            float radiusSquared = radius * radius;
            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    if (!_cells.TryGetValue(new Vector2Int(x, y), out List<(T item, Vector2 position)> bucket)) continue;
                    foreach ((T item, Vector2 position) in bucket)
                        if ((position - centre).sqrMagnitude <= radiusSquared) results.Add(item);
                }
        }

        private Vector2Int CellOf(Vector2 position) =>
            new(Mathf.FloorToInt(position.x / _cellSize), Mathf.FloorToInt(position.y / _cellSize));
    }
}
