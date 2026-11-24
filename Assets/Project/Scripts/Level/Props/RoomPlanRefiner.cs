using System;
using CGD.Core;

namespace CGD.Level
{
    // Improves a furnished room by hill climbing: move one prop, keep the move if the room is
    // closer to what it should be, put it back if not, and repeat. Re-rolling a whole room from
    // another seed throws away everything that was right about it; this keeps it and changes
    // only what the score says is wrong.
    //
    // It knows nothing of rooms or props: the plan says what a legal move is, `cost` what a
    // good room is (0 is perfect, lower is better). A move is kept only if the cost drops, so
    // the result is never worse than what it started from, and a room that costs nothing is
    // left exactly as it was.
    internal static class RoomPlanRefiner
    {
        // A drop smaller than this is no improvement (float noise in the cost).
        private const float MinGain = 1e-4f;

        // Tries up to `steps` moves, stopping early at a cost of 0. Returns what came of it.
        public static RefineResult Refine(IRefinablePlan plan, Func<float> cost, RandomStream random, int steps)
        {
            float current = cost();
            float start = current;
            int kept = 0;
            for (int step = 0; step < steps && current > 0f && plan.MovableCount > 0; step++)
            {
                if (!plan.TryMove(random.Range(0, plan.MovableCount), random, out Action undo)) continue;

                float moved = cost();
                if (moved < current - MinGain)
                {
                    current = moved;
                    kept++;
                    continue;
                }
                undo();
            }
            return new RefineResult(start, current, kept);
        }
    }

    // How a refinement went: the cost before and after, and how many moves were kept.
    internal readonly struct RefineResult
    {
        public RefineResult(float startCost, float cost, int moves)
        {
            StartCost = startCost;
            Cost      = cost;
            Moves     = moves;
        }

        public float StartCost { get; }
        public float Cost      { get; }
        public int   Moves     { get; }
    }
}
