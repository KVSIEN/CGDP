using NUnit.Framework;
using UnityEngine;
using CGD.Algorithms;

namespace CGD.Tests
{
    // Every noise stays in -1..1, changes smoothly, repeats for a seed and differs between seeds.
    public class NoiseTests
    {
        private static INoise2D[] Noises(int seed) => new INoise2D[]
        {
            new ValueNoise(seed), new PerlinNoise(seed), new SimplexNoise(seed), new WorleyNoise(seed),
            new FractalNoise(new PerlinNoise(seed)),
            new FractalNoise(new SimplexNoise(seed), mode: FractalMode.Billow),
            new FractalNoise(new PerlinNoise(seed), mode: FractalMode.Ridged),
            new DomainWarpNoise(new FractalNoise(new PerlinNoise(seed)), new SimplexNoise(seed + 1), 1.5f),
        };

        [Test]
        public void EveryNoiseStaysInRangeAndChangesSmoothly()
        {
            foreach (INoise2D noise in Noises(3))
            {
                float lowest = 1f, highest = -1f;
                for (float x = -10f; x < 10f; x += 0.137f)
                    for (float y = -10f; y < 10f; y += 0.173f)
                    {
                        float value = noise.Sample(x, y);
                        Assert.That(value, Is.InRange(-1f, 1f), $"{noise.GetType().Name} at ({x}, {y})");
                        Assert.That(Mathf.Abs(noise.Sample(x + 0.001f, y) - value), Is.LessThan(0.05f), $"{noise.GetType().Name} jumps at ({x}, {y})");
                        lowest  = Mathf.Min(lowest, value);
                        highest = Mathf.Max(highest, value);
                    }
                Assert.That(highest - lowest, Is.GreaterThan(0.8f), $"{noise.GetType().Name} uses its range");
            }
        }

        [Test]
        public void TheSameSeedGivesTheSameNoiseAndAnotherSeedDoesNot()
        {
            INoise2D[] first = Noises(7), again = Noises(7), other = Noises(8);
            for (int i = 0; i < first.Length; i++)
            {
                Assert.That(again[i].Sample(1.3f, 2.7f), Is.EqualTo(first[i].Sample(1.3f, 2.7f)));
                Assert.That(other[i].Sample(1.3f, 2.7f), Is.Not.EqualTo(first[i].Sample(1.3f, 2.7f)), first[i].GetType().Name);
            }
        }

        [Test]
        public void PerlinNoiseIsZeroOnTheGridCorners()
        {
            var perlin = new PerlinNoise(5);
            for (int x = -3; x <= 3; x++)
                for (int y = -3; y <= 3; y++)
                    Assert.That(perlin.Sample(x, y), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void WorleyFindsTheNearestFeaturePoint()
        {
            var worley = new WorleyNoise(2);
            for (float x = 0f; x < 6f; x += 0.31f)
                for (float y = 0f; y < 6f; y += 0.29f)
                {
                    WorleySample sample = worley.Evaluate(x, y);
                    Assert.That(sample.Nearest, Is.LessThanOrEqualTo(sample.SecondNearest));
                    Assert.That(Vector2.Distance(new Vector2(x, y), worley.FeaturePoint(sample.Cell)), Is.EqualTo(sample.Nearest).Within(1e-5f));
                }
            Assert.That(new WorleyNoise(2, jitter: 0f).FeaturePoint(new Vector2Int(3, 4)), Is.EqualTo(new Vector2(3.5f, 4.5f)));
        }

        [Test]
        public void TheHashRepeatsAndSpreadsEvenly()
        {
            Assert.That(CoordinateHash.Hash(4, 7, 1u), Is.EqualTo(CoordinateHash.Hash(4, 7, 1u)));
            Assert.That(CoordinateHash.Hash(4, 7, 1u), Is.Not.EqualTo(CoordinateHash.Hash(7, 4, 1u)));
            double sum = 0;
            for (int x = 0; x < 100; x++)
                for (int y = 0; y < 100; y++) sum += CoordinateHash.Value01(x, y, 9u);
            Assert.That(sum / 10000, Is.EqualTo(0.5).Within(0.02));
        }
    }
}
