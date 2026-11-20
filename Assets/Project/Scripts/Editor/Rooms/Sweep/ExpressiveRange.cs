using System.Collections.Generic;
using UnityEngine;

namespace CGD.Editor
{
    // Expressive range (Smith & Whitehead, 2010): what a generator really makes, as a 2D
    // histogram of many of its rooms on two measures. A faction whose rooms crowd into a few
    // cells is building one room over and over; two factions whose histograms overlap build
    // the same rooms in different pieces.
    public sealed class ExpressiveRange
    {
        public const int Bins = 8;

        private readonly int[,] _counts = new int[Bins, Bins];

        public ExpressiveRange(IEnumerable<RoomSweepResult> rooms, RoomMeasure x, RoomMeasure y)
        {
            X = x;
            Y = y;
            foreach (RoomSweepResult room in rooms)
            {
                if (!RoomMeasures.TryGet(room, x, out float valueX) || !RoomMeasures.TryGet(room, y, out float valueY)) continue;
                int count = ++_counts[Bin(valueX), Bin(valueY)];
                Max = Mathf.Max(Max, count);
                Total++;
            }
        }

        public RoomMeasure X { get; }
        public RoomMeasure Y { get; }
        // Rooms plotted (those with a value on both measures), and the fullest cell's count.
        public int Total { get; }
        public int Max   { get; }

        public int Count(int x, int y) => _counts[x, y];

        // The share of cells with at least one room: how much of the space the rooms explore.
        public float Coverage
        {
            get
            {
                int used = 0;
                foreach (int count in _counts) if (count > 0) used++;
                return used / (float)(Bins * Bins);
            }
        }

        // How alike two histograms are, 0 (no room in common cells) to 1 (the same spread):
        // the sum over cells of the smaller of the two shares.
        public static float Overlap(ExpressiveRange a, ExpressiveRange b)
        {
            if (a.Total == 0 || b.Total == 0) return 0f;
            float shared = 0f;
            for (int x = 0; x < Bins; x++)
                for (int y = 0; y < Bins; y++)
                    shared += Mathf.Min(a._counts[x, y] / (float)a.Total, b._counts[x, y] / (float)b.Total);
            return shared;
        }

        public static int Bin(float value) => Mathf.Clamp(Mathf.FloorToInt(value * Bins), 0, Bins - 1);
    }
}
