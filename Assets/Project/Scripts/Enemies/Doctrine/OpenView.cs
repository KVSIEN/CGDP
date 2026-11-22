using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Where an idle look should go so it takes in the room, not a wall a step away: staring at a
    // wall reads as broken, and learns nothing. Given how far it can see along evenly spread
    // directions across an arc, it keeps the widest run of open ones (ties going to the run
    // nearest straight ahead) and returns that part of the arc, as a centre and a half-width in
    // degrees from straight ahead. With nothing open it looks the way it can see furthest.
    // Plain logic, tested without the editor.
    public static class OpenView
    {
        // Directions sampled across an arc, and how far one must see along a direction for it to count as open.
        public const int   Samples  = 9;
        public const float MinReach = 3.5f;

        // The angle of sample `index` of `count` spread over -half..half.
        public static float AngleOf(int index, int count, float half) =>
            count > 1 ? -half + index * (2f * half / (count - 1)) : 0f;

        // `reaches[i]` is how far it can see at AngleOf(i, reaches.Count, half). False when nothing is open.
        public static bool Fit(IReadOnlyList<float> reaches, float half, float minReach, out float centre, out float fitHalf)
        {
            int count = reaches.Count;
            float mid = (count - 1) * 0.5f;
            int   bestStart = -1, bestLength = 0;
            float bestGap   = float.MaxValue;

            int i = 0;
            while (i < count)
            {
                if (reaches[i] < minReach) { i++; continue; }

                int start = i;
                while (i < count && reaches[i] >= minReach) i++;
                int   length = i - start;
                float gap    = Mathf.Abs(start + (length - 1) * 0.5f - mid);
                if (length < bestLength || (length == bestLength && gap >= bestGap)) continue;

                bestStart  = start;
                bestLength = length;
                bestGap    = gap;
            }

            if (bestStart < 0)
            {
                int furthest = 0;
                for (int k = 1; k < count; k++)
                    if (reaches[k] > reaches[furthest]) furthest = k;
                centre  = AngleOf(furthest, count, half);
                fitHalf = 0f;
                return false;
            }

            float first = AngleOf(bestStart, count, half);
            float last  = AngleOf(bestStart + bestLength - 1, count, half);
            centre  = (first + last) * 0.5f;
            fitHalf = (last - first) * 0.5f;
            return true;
        }
    }
}
