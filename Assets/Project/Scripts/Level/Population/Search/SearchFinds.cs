using System.Collections.Generic;
using CGD.Core;

namespace CGD.Level
{
    // Which searchable pieces of a level hold its finds. The level holds as many as the pieces add
    // between them (SearchSpot.FindsAdded, its fraction drawn once), so making more furniture
    // searchable spreads the same loot over more places rather than adding to it. The finds go
    // to that many different pieces, drawn by weight without replacement (each piece's key is
    // u^(1/weight), the highest win), so a heavier piece is likelier to hold one but none holds
    // two. Pure: the same spots and stream give the same choice.
    public static class SearchFinds
    {
        public static bool[] Choose(IReadOnlyList<SearchSpot> spots, RandomStream random)
        {
            var holds = new bool[spots.Count];
            float total = 0f;
            int candidates = 0;
            foreach (SearchSpot spot in spots)
            {
                total += spot.FindsAdded;
                if (spot.Weight > 0f) candidates++;
            }

            int count = (int)total;
            if (random.Value < total - count) count++;
            count = System.Math.Min(count, candidates);
            if (count == 0) return holds;

            var keys = new List<(double key, int index)>(candidates);
            for (int i = 0; i < spots.Count; i++)
            {
                double u = System.Math.Max(1e-12, random.Value);
                if (spots[i].Weight > 0f) keys.Add((System.Math.Pow(u, 1.0 / spots[i].Weight), i));
            }
            keys.Sort((a, b) => b.key.CompareTo(a.key));
            for (int i = 0; i < count; i++) holds[keys[i].index] = true;
            return holds;
        }
    }
}
