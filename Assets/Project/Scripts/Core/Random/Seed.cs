using System;
using System.Globalization;

namespace CGD.Core
{
    // A deterministic seed that branches into a tree: Derive("map") → Derive("layout").
    // A child depends only on its parent seed and its label, never on how many numbers
    // anything else drew, so each generated object or system gets its own stable stream.
    // Changing how the loot layer rolls can't shift the map, and rerolling one layer
    // (see SeedVariants) leaves every other layer exactly as it was.
    public readonly struct Seed : IEquatable<Seed>
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        public Seed(ulong value) => Value = value;

        public ulong Value { get; }

        public static Seed From(int value) => new(unchecked((ulong)value));

        // Numbers are used as-is; any other text is hashed, so "banana" is a valid seed too.
        public static Seed Parse(string text)
        {
            text = text?.Trim() ?? string.Empty;
            if (ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong number)) return new Seed(number);
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long signed)) return new Seed(unchecked((ulong)signed));
            return new Seed(Hash(text));
        }

        // A fresh, unpredictable seed for things generated without one. Recorded on the
        // result, so even a "random" roll can be reproduced afterwards.
        public static Seed Random() => new(BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0));

        public Seed Derive(string label) => new(Mix(Value ^ Hash(label)));

        public Seed Derive(int index) => new(Mix(Value + Golden * unchecked((ulong)(index + 1))));

        // Variant 0 is the plain Derive(label), so introducing variants later never
        // changes the seeds existing content was generated from.
        public Seed Derive(string label, int variant) => variant == 0 ? Derive(label) : Derive(label).Derive(variant);

        public RandomStream Stream() => new(this);

        // Handy for Unity APIs and inspector fields that only take an int.
        public int ToInt() => unchecked((int)(Value ^ (Value >> 32)));

        public bool Equals(Seed other) => Value == other.Value;
        public override bool Equals(object obj) => obj is Seed other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        public static bool operator ==(Seed a, Seed b) => a.Value == b.Value;
        public static bool operator !=(Seed a, Seed b) => a.Value != b.Value;

        // SplitMix64 finaliser: spreads every input bit across the output, so
        // neighbouring seeds (1, 2, 3…) give unrelated streams.
        internal static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        // FNV-1a. string.GetHashCode isn't guaranteed stable between runtimes or
        // sessions, and a seed has to mean the same thing forever.
        private static ulong Hash(string text)
        {
            unchecked
            {
                ulong hash = 0xCBF29CE484222325UL;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 0x100000001B3UL;
                }
                return hash;
            }
        }
    }
}
