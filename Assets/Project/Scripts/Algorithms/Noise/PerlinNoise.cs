using UnityEngine;

namespace CGD.Algorithms
{
    // Perlin noise (Ken Perlin, 1985): the classic smooth, natural-looking noise.
    //
    // How it works: like value noise there is a grid, but each corner gets a random direction
    // (a gradient) instead of a value. To sample a point, each of its cell's four corners asks
    // "how far does the point lie along my direction?" (the dot product of the direction and the
    // corner-to-point offset). Those four answers are blended with the eased weights, as in value
    // noise. Because every corner's answer is 0 at the corner itself, the hills and valleys fall
    // between the corners, which hides the grid much better than value noise does.
    //
    // Why it's useful: it is the workhorse of procedural content. Terrain and dirt height,
    // clouds, camera shake, wobbling idle motion, wind, where grass grows thick or thin. Layer
    // several sizes with FractalNoise for detail; bend it with DomainWarpNoise for organic shapes.
    //
    // Unlike Unity's Mathf.PerlinNoise this one is seeded (different seeds give different worlds),
    // returns -1..1, and doesn't repeat.
    public sealed class PerlinNoise : INoise2D
    {
        // Eight directions, evenly spread round the circle, each one long.
        private const float Diagonal = 0.70710678f;
        private static readonly Vector2[] Gradients =
        {
            new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
            new(Diagonal, Diagonal), new(-Diagonal, Diagonal), new(Diagonal, -Diagonal), new(-Diagonal, -Diagonal),
        };

        // 2D Perlin noise with unit gradients peaks at ±√½; this stretches it to ±1.
        private const float ToFullRange = 1.41421356f;

        private readonly uint _seed;

        public PerlinNoise(int seed) => _seed = (uint)seed;

        public float Sample(float x, float y)
        {
            int left = Mathf.FloorToInt(x), bottom = Mathf.FloorToInt(y);
            float insideX = x - left, insideY = y - bottom;

            // How far the point lies along each corner's direction.
            float bottomLeft  = Influence(left,     bottom,     insideX,      insideY);
            float bottomRight = Influence(left + 1, bottom,     insideX - 1f, insideY);
            float topLeft     = Influence(left,     bottom + 1, insideX,      insideY - 1f);
            float topRight    = Influence(left + 1, bottom + 1, insideX - 1f, insideY - 1f);

            float acrossX = NoiseMath.Fade(insideX), acrossY = NoiseMath.Fade(insideY);
            float bottomEdge = Mathf.Lerp(bottomLeft, bottomRight, acrossX);
            float topEdge    = Mathf.Lerp(topLeft, topRight, acrossX);
            return Mathf.Clamp(Mathf.Lerp(bottomEdge, topEdge, acrossY) * ToFullRange, -1f, 1f);
        }

        // The dot product of the corner's random direction and the offset from the corner to the point.
        private float Influence(int cornerX, int cornerY, float offsetX, float offsetY)
        {
            Vector2 gradient = Gradients[CoordinateHash.Pick(cornerX, cornerY, _seed, Gradients.Length)];
            return gradient.x * offsetX + gradient.y * offsetY;
        }
    }
}
