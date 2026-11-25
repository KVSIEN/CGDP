using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // The lumpy shape of overgrowth: fractal noise (broad lumps with finer ones laid over them),
    // its sample point first pushed about by two more noises (domain warping), which turns
    // regular blobs into winding, creeping shapes. Sampled in metres, so a lump is the same
    // size in every room, and offset by the seed, so each room grows its own.
    public readonly struct OvergrowthNoise
    {
        // Each finer layer is this much smaller and this much fainter than the one under it.
        private const float Lacunarity  = 2f;
        private const float Persistence = 0.5f;
        // Layered Perlin noise keeps close to its middle; stretched this much about it, lumps
        // reach from near nothing to their full height.
        private const float Contrast = 2.2f;
        // At full warp a point moves up to this share of a lump.
        private const float WarpReach = 0.6f;
        // Unity's Perlin noise repeats far out; offsets stay well inside it.
        private const float OffsetRange = 10000f;

        private readonly float _frequency;
        private readonly int _layers;
        private readonly float _warp;
        private readonly Vector2 _offset, _warpOffsetX, _warpOffsetY;

        public OvergrowthNoise(float lumpSize, int layers, float warp, RandomStream random)
        {
            _frequency   = 1f / Mathf.Max(0.01f, lumpSize);
            _layers      = Mathf.Max(1, layers);
            _warp        = Mathf.Clamp01(warp) * WarpReach * lumpSize;
            _offset      = RandomOffset(random);
            _warpOffsetX = RandomOffset(random);
            _warpOffsetY = RandomOffset(random);
        }

        // 0..1 at a point in metres.
        public float Sample(Vector2 point)
        {
            if (_warp > 0f)
                point += new Vector2(Fractal(point + _warpOffsetX, 1) - 0.5f, Fractal(point + _warpOffsetY, 1) - 0.5f) * (2f * _warp);
            return Fractal(point + _offset, _layers);
        }

        // Layered Perlin noise, scaled back to 0..1 and stretched about its middle.
        private float Fractal(Vector2 point, int layers)
        {
            float sum = 0f, weight = 1f, total = 0f, frequency = _frequency;
            for (int layer = 0; layer < layers; layer++)
            {
                sum   += Mathf.PerlinNoise(point.x * frequency, point.y * frequency) * weight;
                total += weight;
                weight    *= Persistence;
                frequency *= Lacunarity;
            }
            return Mathf.Clamp01((sum / total - 0.5f) * Contrast + 0.5f);
        }

        private static Vector2 RandomOffset(RandomStream random) =>
            new(random.Range(0f, OffsetRange), random.Range(0f, OffsetRange));
    }
}
