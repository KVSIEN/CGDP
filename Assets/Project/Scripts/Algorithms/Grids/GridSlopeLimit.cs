using UnityEngine;

namespace CGD.Algorithms
{
    // A slope limit: lowers a grid of values until none stands more than `step` above a side
    // neighbour, or `step` times √2 above a diagonal one. That makes `step` the steepest slope
    // the grid can have anywhere.
    //
    // How it works: two sweeps over the grid, one forward from the first row and one back from
    // the last. Each sample is held to no higher than each neighbour already passed, plus the
    // step (a chamfer distance transform). Values are only ever lowered, so the lowest stay
    // where they are and everything round them comes down into an even ramp toward them. Two
    // sweeps settle it; more run until nothing changes, so the result never rests on that.
    //
    // Why it's useful: it keeps generated heights walkable. Pin some samples at 0 (a doorway,
    // a path) and every height round them ramps down to them no steeper than the limit; the
    // overgrowth on a room's floor is shaped this way (Level/Overgrowth/OvergrowthPlanner).
    // It also measures distance: start from 0 at some samples and infinity everywhere else,
    // and with step 1 each sample ends up at its distance, in samples, to the nearest of them
    // (diagonals counted as √2): a distance field from walls, water or anything else.
    public static class GridSlopeLimit
    {
        private const int MostSweeps = 8;
        private const float Diagonal = 1.41421356f;

        public static void Apply(float[] values, int columns, int rows, float step)
        {
            float straight = Mathf.Max(0f, step), diagonal = straight * Diagonal;
            for (int sweep = 0; sweep < MostSweeps; sweep++)
            {
                bool changed = Forward(values, columns, rows, straight, diagonal);
                changed |= Backward(values, columns, rows, straight, diagonal);
                if (!changed) return;
            }
        }

        // From the first row up, each sample held to the neighbours already passed: the one
        // before it and the three in the row below.
        private static bool Forward(float[] values, int columns, int rows, float straight, float diagonal)
        {
            bool changed = false;
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    float limit = values[index];
                    if (column > 0) limit = Mathf.Min(limit, values[index - 1] + straight);
                    if (row > 0)
                    {
                        int below = index - columns;
                        limit = Mathf.Min(limit, values[below] + straight);
                        if (column > 0)           limit = Mathf.Min(limit, values[below - 1] + diagonal);
                        if (column < columns - 1) limit = Mathf.Min(limit, values[below + 1] + diagonal);
                    }
                    changed |= Lower(values, index, limit);
                }
            return changed;
        }

        // From the last row down, the mirror of Forward.
        private static bool Backward(float[] values, int columns, int rows, float straight, float diagonal)
        {
            bool changed = false;
            for (int row = rows - 1; row >= 0; row--)
                for (int column = columns - 1; column >= 0; column--)
                {
                    int index = row * columns + column;
                    float limit = values[index];
                    if (column < columns - 1) limit = Mathf.Min(limit, values[index + 1] + straight);
                    if (row < rows - 1)
                    {
                        int above = index + columns;
                        limit = Mathf.Min(limit, values[above] + straight);
                        if (column > 0)           limit = Mathf.Min(limit, values[above - 1] + diagonal);
                        if (column < columns - 1) limit = Mathf.Min(limit, values[above + 1] + diagonal);
                    }
                    changed |= Lower(values, index, limit);
                }
            return changed;
        }

        private static bool Lower(float[] values, int index, float limit)
        {
            if (limit >= values[index]) return false;
            values[index] = limit;
            return true;
        }
    }
}
