using UnityEngine;

namespace CGD.Algorithms
{
    // Value noise: the simplest smooth noise, and the easiest way to see how noise works.
    //
    // How it works: picture a grid over the plane. Each grid corner gets a random value (from
    // CoordinateHash, so the same corner always gets the same value). To sample a point, find the
    // four corners of the cell it is in and blend their values, weighted by how close the point is
    // to each, easing the blend (NoiseMath.Fade) so the cell edges don't show.
    //
    // Why it's useful: cheap and easy to follow. Its weakness is that it looks blocky, with its
    // highs and lows sitting on the grid corners; Perlin and simplex noise look more natural.
    // Good for things where that doesn't show: flickering lights, a value that wanders over time
    // (sample along one axis), rough variation per tile.
    public sealed class ValueNoise : INoise2D
    {
        private readonly uint _seed;

        public ValueNoise(int seed) => _seed = (uint)seed;

        public float Sample(float x, float y)
        {
            // The cell's bottom-left corner, and how far into the cell the point is (0..1 each way).
            int left = Mathf.FloorToInt(x), bottom = Mathf.FloorToInt(y);
            float acrossX = NoiseMath.Fade(x - left), acrossY = NoiseMath.Fade(y - bottom);

            float bottomEdge = Mathf.Lerp(Corner(left, bottom),     Corner(left + 1, bottom),     acrossX);
            float topEdge    = Mathf.Lerp(Corner(left, bottom + 1), Corner(left + 1, bottom + 1), acrossX);
            return Mathf.Lerp(bottomEdge, topEdge, acrossY);
        }

        private float Corner(int x, int y) => CoordinateHash.ValueSigned(x, y, _seed);
    }
}
