using UnityEngine;

namespace CGD.Algorithms
{
    // Fractal noise: several layers ("octaves") of one noise added together, each smaller and
    // fainter than the last. With plain layers this is fractal Brownian motion (fBm).
    //
    // How it works: the first layer gives the big shapes. Each further layer samples the same
    // noise at a higher frequency (lacunarity times the last: smaller features) and a lower
    // amplitude (persistence times the last: weaker), and is added on top. The total is divided
    // by the sum of the amplitudes, so the result stays about -1..1 however many layers you add.
    // Nature looks like this: a coastline, a mountain or a cloud has big shapes with smaller
    // versions of them on top, and smaller ones on those.
    //
    // Why it's useful: one layer of noise looks smooth and artificial; three to six look like
    // terrain, rock, bark, clouds or a ragged floor of overgrowth. FractalMode switches between
    // rolling (Standard), puffy (Billow) and sharp-crested (Ridged) shapes.
    //
    // Settings:
    //   octaves     how many layers: more is more detail, and costs one sample each;
    //   lacunarity  how much smaller each layer is: 2 halves the feature size each time;
    //   persistence how much weaker each layer is: 0.5 is natural, higher is rougher.
    public sealed class FractalNoise : INoise2D
    {
        // Each layer is shifted by this much more than the last, so the layers' grids don't line
        // up at the origin (where every layer would otherwise share a corner and a seam).
        private const float LayerShift = 17.31f;

        private readonly INoise2D _source;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _persistence;
        private readonly FractalMode _mode;

        public FractalNoise(INoise2D source, int octaves = 4, float lacunarity = 2f, float persistence = 0.5f,
                            FractalMode mode = FractalMode.Standard)
        {
            _source      = source;
            _octaves     = Mathf.Max(1, octaves);
            _lacunarity  = lacunarity;
            _persistence = persistence;
            _mode        = mode;
        }

        public float Sample(float x, float y)
        {
            float sum = 0f, totalAmplitude = 0f;
            float amplitude = 1f, frequency = 1f;
            for (int octave = 0; octave < _octaves; octave++)
            {
                float shift = octave * LayerShift;
                float layer = _source.Sample(x * frequency + shift, y * frequency + shift);
                sum            += Shape(layer) * amplitude;
                totalAmplitude += amplitude;

                frequency *= _lacunarity;
                amplitude *= _persistence;
            }
            return sum / totalAmplitude;
        }

        // Every shape keeps a layer within -1..1, so the total does too.
        private float Shape(float value) => _mode switch
        {
            FractalMode.Billow => Mathf.Abs(value) * 2f - 1f,
            FractalMode.Ridged => Square(1f - Mathf.Abs(value)) * 2f - 1f,
            _                  => value,
        };

        private static float Square(float value) => value * value;
    }
}
