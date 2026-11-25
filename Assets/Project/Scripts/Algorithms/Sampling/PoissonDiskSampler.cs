using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Algorithms
{
    // Poisson disk sampling: scatters points at random, but never two closer than a set distance.
    // Uses Robert Bridson's fast method ("Fast Poisson Disk Sampling in Arbitrary Dimensions", 2007).
    //
    // How it works:
    //   1. Start from one random point and put it on an "active" list.
    //   2. Take a random active point and try a few random spots in the ring around it, between
    //      one and two minimum distances away. Keep the first spot that is far enough from every
    //      point placed so far, and make it active too.
    //   3. If none of the tries fits, the area round that point is full: drop it from the list.
    //   4. Repeat until no point is active.
    // A background grid with cells just small enough to hold one point each makes "far enough
    // from every point" cheap: only the few cells around a spot can hold a point that close.
    //
    // Why it's useful: plain random scatter clumps and leaves holes, and a grid looks artificial.
    // Poisson disk points look natural and even ("blue noise"): trees in a forest, rocks, grass
    // tufts, bushes, pickups, enemy spawn spots, stars, the scattered decor of an overgrown room.
    // Pass `isAllowed` to keep points off walls, paths or water.
    public static class PoissonDiskSampler
    {
        // How many spots to try round each point before calling its area full. 30 is Bridson's;
        // fewer is faster but leaves gaps.
        public const int DefaultAttempts = 30;

        // Points in the rectangle from (0, 0) to (width, height), each at least minDistance from the others.
        public static List<Vector2> Sample(float width, float height, float minDistance, RandomStream random,
                                           Func<Vector2, bool> isAllowed = null, int attempts = DefaultAttempts)
        {
            var points = new List<Vector2>();
            if (width <= 0f || height <= 0f || minDistance <= 0f) return points;

            // A cell's diagonal equals minDistance, so a cell can hold one point at most.
            float cellSize = minDistance / Mathf.Sqrt(2f);
            int columns = Mathf.CeilToInt(width / cellSize), rows = Mathf.CeilToInt(height / cellSize);
            var grid = new int[columns, rows];                  // index into `points`, or -1 for empty
            for (int x = 0; x < columns; x++)
                for (int y = 0; y < rows; y++) grid[x, y] = -1;

            bool Fits(Vector2 spot)
            {
                if (spot.x < 0f || spot.y < 0f || spot.x >= width || spot.y >= height) return false;
                if (isAllowed != null && !isAllowed(spot)) return false;

                // A point closer than minDistance can only be within two cells either way.
                int cellX = (int)(spot.x / cellSize), cellY = (int)(spot.y / cellSize);
                for (int x = Mathf.Max(0, cellX - 2); x <= Mathf.Min(columns - 1, cellX + 2); x++)
                    for (int y = Mathf.Max(0, cellY - 2); y <= Mathf.Min(rows - 1, cellY + 2); y++)
                    {
                        int other = grid[x, y];
                        if (other >= 0 && (points[other] - spot).sqrMagnitude < minDistance * minDistance) return false;
                    }
                return true;
            }

            var active = new List<int>();
            void Place(Vector2 spot)
            {
                grid[(int)(spot.x / cellSize), (int)(spot.y / cellSize)] = points.Count;
                active.Add(points.Count);
                points.Add(spot);
            }

            // 1. The first point: a few random tries, in case `isAllowed` rules much of the area out.
            for (int attempt = 0; attempt < attempts && points.Count == 0; attempt++)
            {
                var spot = new Vector2(random.Range(0f, width), random.Range(0f, height));
                if (Fits(spot)) Place(spot);
            }

            // 2 to 4.
            while (active.Count > 0)
            {
                int which = random.Range(0, active.Count);
                Vector2 centre = points[active[which]];

                bool placed = false;
                for (int attempt = 0; attempt < attempts && !placed; attempt++)
                {
                    float angle = random.Range(0f, 2f * Mathf.PI);
                    float distance = random.Range(minDistance, 2f * minDistance);
                    var spot = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (!Fits(spot)) continue;
                    Place(spot);
                    placed = true;
                }

                if (placed) continue;
                // Full round here: swap it with the last active point and drop it.
                active[which] = active[active.Count - 1];
                active.RemoveAt(active.Count - 1);
            }
            return points;
        }
    }
}
