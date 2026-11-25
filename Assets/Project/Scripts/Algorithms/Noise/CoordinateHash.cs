namespace CGD.Algorithms
{
    // A coordinate hash: whole-number coordinates and a seed go in, a random-looking number
    // comes out, and the same input always gives the same output.
    //
    // How it works: the coordinates are folded into one number, which is multiplied by large
    // constants, offset by the seed and shuffled with bit shifts until every input bit has
    // stirred every output bit. Neighbouring inputs, (4, 7) and (5, 7), give unrelated outputs.
    //
    // Why it's useful: it is randomness you can look up. A random number generator has to be
    // stepped through in order; a hash answers for any cell, in any order, as often as you ask,
    // and nothing has to be stored. Every noise in this folder uses it to give each grid corner
    // its random value or direction, and it answers questions like "does tile (12, 40) get a
    // crate?" the same way every time for a given seed.
    //
    // The mixing constants are Squirrel Eiserloh's "Squirrel3" (GDC 2017, "Math for Game
    // Programmers: Noise-Based RNG").
    public static class CoordinateHash
    {
        private const uint BitNoise1 = 0xB5297A4D;
        private const uint BitNoise2 = 0x68E31DA4;
        private const uint BitNoise3 = 0x1B56C4E9;

        // Large primes that fold y and z into the same number as x, so (1, 0) and (0, 1) land
        // far apart instead of on the same value.
        private const int PrimeY = 198491317;
        private const int PrimeZ = 6542989;

        public static uint Hash(int x, uint seed)
        {
            unchecked
            {
                uint bits = (uint)x;
                bits *= BitNoise1;
                bits += seed;
                bits ^= bits >> 8;
                bits += BitNoise2;
                bits ^= bits << 8;
                bits *= BitNoise3;
                bits ^= bits >> 8;
                return bits;
            }
        }

        public static uint Hash(int x, int y, uint seed) => Hash(unchecked(x + PrimeY * y), seed);

        public static uint Hash(int x, int y, int z, uint seed) => Hash(unchecked(x + PrimeY * y + PrimeZ * z), seed);

        // A value from 0 (included) to 1 (not included), from the hash's top 24 bits (a float holds exactly 24).
        public static float Value01(int x, int y, uint seed) => (Hash(x, y, seed) >> 8) * (1f / (1 << 24));

        // A value from -1 to 1.
        public static float ValueSigned(int x, int y, uint seed) => Value01(x, y, seed) * 2f - 1f;

        // One of `count` choices (0 to count - 1), from the hash's top bits, which are the best mixed.
        public static int Pick(int x, int y, uint seed, int count) => (int)((Hash(x, y, seed) >> 8) % (uint)count);
    }
}
