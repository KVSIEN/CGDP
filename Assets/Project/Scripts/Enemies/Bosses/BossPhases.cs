using System;
using System.Collections.Generic;

namespace CGD.Enemies
{
    // Which phase a boss fight is in, from its health. Phases only move forward and each
    // starts once: healing back above a threshold doesn't replay it, and one big hit past
    // several thresholds enters every one of them.
    public class BossPhases
    {
        private readonly float[] _thresholds;

        // `thresholds`: the health share (0–1) below which each later phase starts, in any order.
        public BossPhases(IReadOnlyList<float> thresholds)
        {
            _thresholds = new float[thresholds.Count];
            for (int i = 0; i < _thresholds.Length; i++) _thresholds[i] = thresholds[i];
            Array.Sort(_thresholds);
            Array.Reverse(_thresholds);
        }

        // The opening phase included.
        public int Count => _thresholds.Length + 1;

        // 0 = the opening phase; n = the nth threshold has been crossed.
        public int Current { get; private set; }

        // Moves to the phase `healthFraction` belongs in; returns how many phases began.
        public int Advance(float healthFraction)
        {
            int before = Current;
            while (Current < _thresholds.Length && healthFraction < _thresholds[Current]) Current++;
            return Current - before;
        }
    }
}
