using UnityEngine;

namespace CGD.Algorithms
{
    // Simplex noise (Ken Perlin, 2001): Perlin noise's successor, on a grid of triangles instead
    // of squares. Follows Stefan Gustavson's "Simplex noise demystified" (2005).
    //
    // How it works:
    //   1. Skew the plane so a grid of squares, each cut corner to corner, becomes a grid of
    //      equal-sided triangles, and find the triangle the point is in.
    //   2. Each of the triangle's 3 corners (not a square's 4) has a random direction, as in
    //      Perlin noise, and asks how far the point lies along it.
    //   3. Instead of blending, each corner's answer fades out with distance from that corner
    //      (it reaches 0 before the next triangle), and the three are simply added up.
    //
    // Why it's useful: it looks like Perlin noise but with fewer straight-line artefacts (no
    // square grid to show through), it costs less (3 corners, not 4; in 3D 4 instead of 8), and
    // its detail looks the same in every direction. Use it wherever you'd use Perlin, especially
    // in 3D or when you sample a lot (a field per frame, a big map).
    public sealed class SimplexNoise : INoise2D
    {
        // The skew that turns the square grid into triangles, and its inverse.
        private static readonly float Skew   = 0.5f * (Mathf.Sqrt(3f) - 1f);
        private static readonly float Unskew = (3f - Mathf.Sqrt(3f)) / 6f;

        // Twelve directions (Gustavson's set): the four diagonals and, twice each, the four axes.
        private static readonly Vector2[] Gradients =
        {
            new(1f, 1f), new(-1f, 1f), new(1f, -1f), new(-1f, -1f),
            new(1f, 0f), new(-1f, 0f), new(1f, 0f), new(-1f, 0f),
            new(0f, 1f), new(0f, -1f), new(0f, 1f), new(0f, -1f),
        };

        // Scales the sum of the three corners to about -1..1.
        private const float ToFullRange = 70f;

        private readonly uint _seed;

        public SimplexNoise(int seed) => _seed = (uint)seed;

        public float Sample(float x, float y)
        {
            // 1. Which skewed cell the point is in, and where in it.
            float skew = (x + y) * Skew;
            int cellX = Mathf.FloorToInt(x + skew), cellY = Mathf.FloorToInt(y + skew);
            float unskew = (cellX + cellY) * Unskew;
            float x0 = x - (cellX - unskew), y0 = y - (cellY - unskew);   // offset from the first corner

            // Each cell is two triangles; the point is in the lower one when it is further along x than y.
            int middleX = x0 > y0 ? 1 : 0, middleY = x0 > y0 ? 0 : 1;
            float x1 = x0 - middleX + Unskew,     y1 = y0 - middleY + Unskew;       // from the middle corner
            float x2 = x0 - 1f + 2f * Unskew,     y2 = y0 - 1f + 2f * Unskew;       // from the far corner

            // 2 and 3. Each corner's contribution, added up.
            float sum = Corner(cellX, cellY, x0, y0)
                      + Corner(cellX + middleX, cellY + middleY, x1, y1)
                      + Corner(cellX + 1, cellY + 1, x2, y2);
            return Mathf.Clamp(sum * ToFullRange, -1f, 1f);
        }

        // A corner's random direction, dotted with the offset to the point, faded by distance:
        // (0.5 - distance²)⁴, which reaches 0 before the neighbouring triangles.
        private float Corner(int cornerX, int cornerY, float offsetX, float offsetY)
        {
            float falloff = 0.5f - offsetX * offsetX - offsetY * offsetY;
            if (falloff <= 0f) return 0f;

            Vector2 gradient = Gradients[CoordinateHash.Pick(cornerX, cornerY, _seed, Gradients.Length)];
            falloff *= falloff;
            return falloff * falloff * (gradient.x * offsetX + gradient.y * offsetY);
        }
    }
}
