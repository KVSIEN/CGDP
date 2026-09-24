using System.Collections.Generic;
using UnityEngine;

namespace CGD.Core
{
    // A deterministic random number sequence: the same Seed always produces the same
    // numbers, on every platform. Use one stream per system or object; Fork gives a
    // child stream that depends on this stream's seed, not on how far it has been read.
    public sealed class RandomStream
    {
        private ulong _state;

        public RandomStream(Seed seed)
        {
            Seed   = seed;
            _state = seed.Value;
        }

        public Seed Seed { get; }

        public ulong NextULong()
        {
            unchecked { _state += 0x9E3779B97F4A7C15UL; }
            return Seed.Mix(_state);
        }

        // [0, 1).
        public float Value => (NextULong() >> 40) * (1f / (1 << 24));

        // [min, max).
        public float Range(float min, float max) => min + (max - min) * Value;

        // [min, maxExclusive), like UnityEngine.Random.Range(int, int).
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            ulong span = (ulong)((long)maxExclusive - min);
            return (int)(min + (long)(NextULong() % span));
        }

        public bool Chance(float probability) => Value < probability;

        // -1 or 1.
        public int Sign() => Chance(0.5f) ? -1 : 1;

        public Vector2 InsideUnitCircle()
        {
            float angle  = Value * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(Value);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        public void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        // The next seed in this stream, for handing each generated child (one loot drop,
        // one weapon) its own seed while keeping the whole sequence reproducible.
        public Seed NextSeed() => new(NextULong());

        public RandomStream Fork(string label) => new(Seed.Derive(label));
    }
}
