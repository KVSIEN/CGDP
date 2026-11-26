using System;
using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // How an elevated area is guarded and reached for its role (ElevationRole), shared by the
    // rules that build blocks and raised sections so a perch or a pit plays the same whichever
    // built it.
    //
    // Flights are spread rather than bunched: past the ones every piece and route needs, each
    // further flight is the one that climbs from a side no flight does yet, else the farthest
    // from the flights already chosen. So a second way onto a guarded area is a flank, not a
    // second door beside the first.
    internal static class ElevationAccess
    {
        // A perch smaller than this is reached one way: a second flight onto a ledge that small
        // would leave nothing to hold.
        public const int SmallPerch = 12;
        // A bastion is reached by two flights, three along a long step: any more and it is no
        // longer held at its stairs.
        public const int MostBastionWays = 3;

        // The guards along an area's ledges: `open` on a split level's or a pit's (None or
        // Railing, as the rule rolls), `wall` on a perch's front or all round a bastion. A perch's
        // other sides stay open: one level, so they are mantled up.
        public static LedgeGuards Guards(ElevationRole role, RoomStructure structure, IReadOnlyCollection<Vector2Int> tiles,
                                         LedgeGuard open, LedgeGuard wall) => role switch
        {
            ElevationRole.Bastion => LedgeGuards.All(wall),
            ElevationRole.Perch   => LedgeGuards.None.With(Front(structure, tiles), wall),
            _                     => LedgeGuards.All(open),
        };

        // Whether the role rolls its open ledges' railing (the others are set by the role alone).
        public static bool RollsRailing(ElevationRole role) => role == ElevationRole.SplitLevel || role == ElevationRole.Pit;

        // How many flights the role wants onto an area of `tiles` tiles, given the rule's own `wanted`.
        public static int Ways(ElevationRole role, int wanted, int tiles) => role switch
        {
            ElevationRole.Perch   => Mathf.Clamp(wanted, tiles >= SmallPerch ? 2 : 1, 2),
            ElevationRole.Bastion => Mathf.Clamp(wanted, 2, MostBastionWays),
            ElevationRole.Pit     => Mathf.Max(2, wanted),
            _                     => wanted,
        };

        // The fewest flights an area with these guards may have: two onto a bastion, and into a
        // railed pit, so neither is a dead end (an open pit's rim is climbed out of); one otherwise.
        public static int Required(ElevationRole role, LedgeGuards guards) =>
            role == ElevationRole.Bastion || (role == ElevationRole.Pit && guards.Any) ? 2 : 1;

        // The guards an area keeps with `flights` flights: `guards` when that is enough (Required),
        // an open rim for a pit with room for only one way out, else null and the rule tries elsewhere.
        public static LedgeGuards? Fit(ElevationRole role, LedgeGuards guards, int flights)
        {
            if (flights >= Required(role, guards)) return guards;
            if (role == ElevationRole.Pit && flights >= 1) return LedgeGuards.None;
            return null;
        }

        // The side most of the area's ledge faces out to, which is the side facing into the room
        // (a gallery's front, the long step of a raised corner). Ties go to the first side in
        // GridNeighbours.Four. The room's own walls, inner walls and other areas' tiles are no ledge of its.
        public static Vector2Int Front(RoomStructure structure, IReadOnlyCollection<Vector2Int> tiles)
        {
            var inside = tiles as HashSet<Vector2Int> ?? new HashSet<Vector2Int>(tiles);
            Vector2Int front = GridNeighbours.Four[0];
            int most = -1;
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                int count = 0;
                foreach (Vector2Int tile in tiles)
                {
                    Vector2Int outside = tile + side;
                    if (structure.Contains(outside) && !inside.Contains(outside) && !structure.IsPartitioned(tile, outside)) count++;
                }
                if (count > most)
                {
                    most = count;
                    front = side;
                }
            }
            return front;
        }

        // Flights for an area from `preferred` (most wanted first), at least `minGap` tiles apart:
        // one onto every piece of it first (`reaches`: whether a flight leads onto a piece), then
        // every flight `always` asks for (null = none), then spread out (see above) until there
        // are `wanted`. Draws no random numbers: the caller's order carries the seed.
        public static List<StairFlight> Pick(IReadOnlyList<StairFlight> preferred, IReadOnlyList<HashSet<Vector2Int>> pieces,
                                             Func<StairFlight, HashSet<Vector2Int>, bool> reaches, Predicate<StairFlight> always,
                                             int wanted, int minGap)
        {
            var chosen = new List<StairFlight>(Mathf.Max(wanted, pieces.Count));
            foreach (HashSet<Vector2Int> piece in pieces)
                foreach (StairFlight stairs in preferred)
                    if (reaches(stairs, piece) && Apart(stairs, chosen, minGap))
                    {
                        chosen.Add(stairs);
                        break;
                    }

            if (always != null)
                foreach (StairFlight stairs in preferred)
                    if (always(stairs) && Apart(stairs, chosen, minGap)) chosen.Add(stairs);

            while (chosen.Count < wanted)
            {
                int best = -1;
                float bestSpread = float.NegativeInfinity;
                for (int i = 0; i < preferred.Count; i++)
                {
                    if (!Apart(preferred[i], chosen, minGap)) continue;
                    float spread = Spread(preferred[i], chosen);
                    if (spread > bestSpread)
                    {
                        bestSpread = spread;
                        best = i;
                    }
                }
                if (best < 0) break;
                chosen.Add(preferred[best]);
            }
            return chosen;
        }

        // How much a flight adds to the spread of those chosen: a side none climbs from counts
        // for more than any distance, then the distance to the nearest of them. The first flight
        // has no spread to add, so the most preferred is taken.
        private static float Spread(StairFlight stairs, List<StairFlight> chosen)
        {
            if (chosen.Count == 0) return 0f;

            bool newSide = true;
            int nearest = int.MaxValue;
            foreach (StairFlight other in chosen)
            {
                if (other.Up == stairs.Up) newSide = false;
                nearest = Mathf.Min(nearest, Distance(stairs, other));
            }
            return (newSide ? 10000f : 0f) + nearest;
        }

        private static bool Apart(StairFlight stairs, List<StairFlight> chosen, int minGap)
        {
            foreach (StairFlight other in chosen)
                if (other.Tile == stairs.Tile || Distance(stairs, other) < minGap) return false;
            return true;
        }

        private static int Distance(StairFlight a, StairFlight b)
        {
            Vector2Int apart = a.Tile - b.Tile;
            return Mathf.Max(Mathf.Abs(apart.x), Mathf.Abs(apart.y));
        }
    }
}
