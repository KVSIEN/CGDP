using System;
using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Cuts a routed corridor's square bends into 45° runs: the tiles round the corner, `steps`
    // back along both arms, are swapped for a staircase of tiles straight across the inside of
    // the bend, as many tiles as before. LevelWallPlan walls a staircase with one straight
    // diagonal each side, so the corridor turns in two 45° bends instead of one 90° one.
    //
    // A bend is cut as far as both arms run straight and a tile more (the doorway's tile and the
    // one in front of it never move), every new tile is free (`isPassable`) and touches the
    // corridor nowhere but at the run's two ends; shorter where only a shorter cut fits, and not
    // at all under MinSteps. Deterministic: no random numbers.
    public static class CorridorCorners
    {
        public const int MaxSteps = 6;
        // A cut shorter than this is two small chamfers, not a run.
        public const int MinSteps = 2;

        public static List<Vector2Int> Cut(IReadOnlyList<Vector2Int> path, int steps, Func<Vector2Int, bool> isPassable)
        {
            var result = new List<Vector2Int>(path);
            if (steps < MinSteps) return result;

            for (int i = 1; i < result.Count - 1; i++)
            {
                Vector2Int before = result[i] - result[i - 1], after = result[i + 1] - result[i];
                if (before == after) continue;

                int longest = Mathf.Min(Mathf.Min(steps, Arm(result, i, -1) - 1), Arm(result, i, 1) - 1);
                longest = Mathf.Min(longest, Mathf.Min(i - 1, result.Count - 2 - i));
                // The longest cut that fits: a tight bend (a U-turn's) takes a shorter one.
                for (int k = longest; k >= MinSteps; k--)
                {
                    List<Vector2Int> stairs = Staircase(result[i - k], before, after, k);
                    if (!Fits(result, i - k, i + k, stairs, isPassable)) continue;

                    for (int s = 0; s < stairs.Count; s++) result[i - k + 1 + s] = stairs[s];
                    i += k;   // the run's far end is a bend of its own; carry on past it
                    break;
                }
            }
            return result;
        }

        // How many moves in a straight line meet at tile `i`, going back (-1) or on (+1).
        private static int Arm(List<Vector2Int> path, int i, int way)
        {
            Vector2Int Move(int j) => way < 0 ? path[j] - path[j - 1] : path[j + 1] - path[j];
            Vector2Int first = Move(i);
            int count = 0;
            for (int j = i; way < 0 ? j >= 1 : j + 1 < path.Count; j += way)
            {
                if (Move(j) != first) break;
                count++;
            }
            return count;
        }

        // The tiles strictly between `start` and the run's far end: across the inside of the bend,
        // the turn's way first, then alternately.
        private static List<Vector2Int> Staircase(Vector2Int start, Vector2Int before, Vector2Int after, int k)
        {
            var tiles = new List<Vector2Int>(2 * k - 1);
            Vector2Int tile = start;
            for (int s = 0; s < 2 * k - 1; s++)
            {
                tile += s % 2 == 0 ? after : before;
                tiles.Add(tile);
            }
            return tiles;
        }

        // Every new tile is free, isn't already on the corridor and touches it only where the run
        // joins it: side to side, its first tile the start and its last the far end; corner to
        // corner, only the arms either end. Anywhere else the corridor would run into itself.
        private static bool Fits(List<Vector2Int> path, int start, int end, List<Vector2Int> stairs, Func<Vector2Int, bool> isPassable)
        {
            var kept = new Dictionary<Vector2Int, int>();
            for (int j = 0; j < path.Count; j++)
                if (j <= start || j >= end) kept[path[j]] = j;

            for (int s = 0; s < stairs.Count; s++)
            {
                Vector2Int tile = stairs[s];
                if (kept.ContainsKey(tile) || !isPassable(tile)) return false;
                foreach (Vector2Int side in GridNeighbours.Eight)
                {
                    if (!kept.TryGetValue(tile + side, out int index)) continue;
                    bool joins = GridNeighbours.IsDiagonal(side)
                        ? index >= start - 1 && index <= end + 1
                        : (s == 0 && index == start) || (s == stairs.Count - 1 && index == end);
                    if (!joins) return false;
                }
            }
            return true;
        }
    }
}
