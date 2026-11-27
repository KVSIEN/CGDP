using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Mounds and craters for ElevationStructureRule, in as many tiers as asked for and as fit: a
    // round top (a mound's, raised) or floor (a crater's, sunk), sloping one level on every side,
    // a slope wherever its edge runs straight and a solid corner piece where two slopes meet. A
    // mound's slopes stand round its top, on the ground below it; a crater's just inside its rim.
    // Each further tier is a ring of ground round the last, a level lower (a stepped hill) or a
    // smaller hollow inside it, a level deeper (a stepped pit), so the slopes of one tier rise from
    // a ring of the tier below them. One tier is the plain feature, built exactly as it always was.
    internal sealed class RoundElevationBuilder
    {
        // A round feature's tiles lie within its radius and this much more, so a radius of 1 is a plus and 2 a round blob.
        private const float RoundSlack = 0.3f;
        // Tiles between one tier's edge and the next one's: a ring of slopes and a ring of ground for their feet.
        private const int TierInset = 2;

        private readonly ElevationKind _kind;
        private readonly IntRange _radius;

        public RoundElevationBuilder(ElevationKind kind, IntRange radius)
        {
            _kind   = kind;
            _radius = radius;
        }

        private bool Raised => _kind == ElevationKind.Mound;

        // One area per tier, the tier on the doorways' floor first, up to `tiers` of them (fewer
        // where the room leaves no space for more); null when not even one fits.
        public List<ElevatedArea> Build(RoomStructure structure, RandomStream random, int tiers)
        {
            int radius = Mathf.Max(Raised ? 1 : 2, _radius.Evaluate(random));
            RoomFootprint footprint = structure.Footprint;
            Vector2Int centre = footprint.Tiles[random.Range(0, footprint.Tiles.Count)];

            for (int count = tiers; count >= 1; count--)
            {
                if (!footprint.IsInterior(centre, radius + TierInset * (count - 1) + 2)) continue;
                List<ElevatedArea> areas = Raised ? Mound(structure, centre, radius, count) : Crater(structure, centre, radius, count);
                if (areas != null) return areas;
            }
            return null;
        }

        // From the top down: the top, its ring of slopes, then for each lower tier the ground that
        // carries those slopes (and their feet), with its own ring round it.
        private List<ElevatedArea> Mound(RoomStructure structure, Vector2Int centre, int radius, int count)
        {
            HashSet<Vector2Int> ground = Disc(centre, radius);
            var tierOf = new Dictionary<Vector2Int, int>();
            var stairs = new List<StairFlight>[count + 1];
            var corners = new List<GroundCorner>[count + 1];
            for (int tier = count; tier >= 1; tier--)
            {
                stairs[tier] = new List<StairFlight>();
                corners[tier] = new List<GroundCorner>();
                if (!RingAround(ground, stairs[tier], corners[tier])) return null;
                foreach (Vector2Int tile in ground) tierOf.TryAdd(tile, tier);
                if (tier == 1) break;

                foreach (StairFlight slope in stairs[tier]) ground.Add(slope.Tile);
                foreach (GroundCorner corner in corners[tier]) ground.Add(corner.Tile);
                foreach (Vector2Int foot in Around(ground)) ground.Add(foot);
            }

            if (!Stands(structure, ground, stairs, corners)) return null;
            return Areas(ground, tierOf, stairs, corners, count, direction: 1);
        }

        // From the rim inward: the hollow, its ring of slopes, then for each deeper tier the floor
        // inside the slopes and their feet, with its own ring inside it. A rim tile a tier drops
        // from its hollow stays on the tier round it.
        private List<ElevatedArea> Crater(RoomStructure structure, Vector2Int centre, int radius, int count)
        {
            HashSet<Vector2Int> hollow = Disc(centre, radius + TierInset * (count - 1));
            var all = new HashSet<Vector2Int>(hollow);
            var tierOf = new Dictionary<Vector2Int, int>();
            var stairs = new List<StairFlight>[count + 1];
            var corners = new List<GroundCorner>[count + 1];
            for (int tier = 1; tier <= count; tier++)
            {
                stairs[tier] = new List<StairFlight>();
                corners[tier] = new List<GroundCorner>();
                if (!RingInside(hollow, stairs[tier], corners[tier])) return null;
                foreach (Vector2Int tile in hollow) tierOf[tile] = tier;
                if (tier == count) break;

                hollow = Inside(hollow, stairs[tier], corners[tier]);
                if (hollow.Count == 0) return null;
            }

            // A tile dropped from the first tier's hollow is the room's floor again.
            all.RemoveWhere(tile => !tierOf.ContainsKey(tile));
            if (!Stands(structure, all, stairs, corners)) return null;
            return Areas(all, tierOf, stairs, corners, count, direction: -1);
        }

        // Every tile bare floor, every slope's feet free and on the doorways' level before anything is raised.
        private static bool Stands(RoomStructure structure, HashSet<Vector2Int> tiles, List<StairFlight>[] stairs, List<GroundCorner>[] corners)
        {
            foreach (Vector2Int tile in tiles)
                if (!ElevationGround.IsBare(structure, tile)) return false;
            for (int tier = 1; tier < stairs.Length; tier++)
            {
                foreach (StairFlight slope in stairs[tier])
                    if (!ElevationGround.IsBare(structure, slope.Tile) || !structure.IsFree(slope.Foot) || !structure.IsFree(slope.Head)
                        || structure.LevelOf(slope.Foot) != 0) return false;
                foreach (GroundCorner corner in corners[tier])
                    if (!ElevationGround.IsBare(structure, corner.Tile)) return false;
            }
            return true;
        }

        private List<ElevatedArea> Areas(HashSet<Vector2Int> tiles, Dictionary<Vector2Int, int> tierOf, List<StairFlight>[] stairs,
                                         List<GroundCorner>[] corners, int count, int direction)
        {
            var areas = new List<ElevatedArea>(count);
            for (int tier = 1; tier <= count; tier++)
            {
                var own = new List<Vector2Int>();
                foreach (Vector2Int tile in tiles)
                    if (tierOf[tile] == tier) own.Add(tile);
                areas.Add(new ElevatedArea(_kind, direction * tier, own, stairs[tier], LedgeGuards.None, corners[tier]));
            }
            return areas;
        }

        private static HashSet<Vector2Int> Disc(Vector2Int centre, int radius)
        {
            var disc = new HashSet<Vector2Int>();
            float reach = (radius + RoundSlack) * (radius + RoundSlack);
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                    if (dx * dx + dy * dy <= reach) disc.Add(centre + new Vector2Int(dx, dy));
            return disc;
        }

        // The hollow's floor inside its ring of slopes and corners and the ring of their feet: the
        // next tier's hollow.
        private static HashSet<Vector2Int> Inside(HashSet<Vector2Int> hollow, List<StairFlight> stairs, List<GroundCorner> corners)
        {
            var floor = new HashSet<Vector2Int>(hollow);
            foreach (StairFlight slope in stairs) floor.Remove(slope.Tile);
            foreach (GroundCorner corner in corners) floor.Remove(corner.Tile);

            var inside = new HashSet<Vector2Int>();
            foreach (Vector2Int tile in hollow)
                if (floor.Contains(tile) && AllAround(floor, tile)) inside.Add(tile);
            return inside;
        }

        private static bool AllAround(HashSet<Vector2Int> tiles, Vector2Int tile)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (!tiles.Contains(tile + new Vector2Int(dx, dy))) return false;
            return true;
        }

        // A mound's slopes and corners on the floor round its top. A tile touching the top on two
        // opposite sides, or three, joins the top instead, and the ring is worked out again.
        private static bool RingAround(HashSet<Vector2Int> top, List<StairFlight> stairs, List<GroundCorner> corners)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                stairs.Clear();
                corners.Clear();
                bool grew = false;
                foreach (Vector2Int tile in Around(top))
                {
                    Vector2Int toward = Vector2Int.zero;
                    int sides = 0;
                    foreach (Vector2Int side in GridNeighbours.Four)
                        if (top.Contains(tile + side)) { toward += side; sides++; }

                    if (sides == 0) continue;   // beside the top corner to corner only: plain floor
                    if (sides == 1) stairs.Add(new StairFlight(tile, toward, slope: true));
                    else if (sides == 2 && toward != Vector2Int.zero) corners.Add(new GroundCorner(tile, toward));
                    else { top.Add(tile); grew = true; }
                }
                if (!grew) return true;
            }
            return false;
        }

        // A crater's slopes and corners along the inside of its rim, climbing out. A rim tile open
        // to the room on two opposite sides, or three, is dropped from the crater, and the rim is
        // worked out again; a crater left with no floor inside its slopes is no crater.
        private static bool RingInside(HashSet<Vector2Int> hollow, List<StairFlight> stairs, List<GroundCorner> corners)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                stairs.Clear();
                corners.Clear();
                var dropped = new List<Vector2Int>();
                foreach (Vector2Int tile in hollow)
                {
                    Vector2Int outward = Vector2Int.zero;
                    int sides = 0;
                    foreach (Vector2Int side in GridNeighbours.Four)
                        if (!hollow.Contains(tile + side)) { outward += side; sides++; }

                    if (sides == 0) continue;
                    if (sides == 1) stairs.Add(new StairFlight(tile, outward, slope: true));
                    else if (sides == 2 && outward != Vector2Int.zero) corners.Add(new GroundCorner(tile, outward));
                    else dropped.Add(tile);
                }
                foreach (Vector2Int tile in dropped) hollow.Remove(tile);
                if (dropped.Count > 0) continue;

                // Every slope's foot must be the crater's floor, not another slope or a corner.
                var rim = new HashSet<Vector2Int>();
                foreach (StairFlight slope in stairs) rim.Add(slope.Tile);
                foreach (GroundCorner corner in corners) rim.Add(corner.Tile);
                foreach (StairFlight slope in stairs)
                    if (!hollow.Contains(slope.Foot) || rim.Contains(slope.Foot)) return false;
                return stairs.Count > 0;
            }
            return false;
        }

        // Every tile beside the set, side to side or corner to corner, not in it, in a fixed order.
        private static List<Vector2Int> Around(HashSet<Vector2Int> tiles)
        {
            var seen = new HashSet<Vector2Int>();
            var around = new List<Vector2Int>();
            var sorted = new List<Vector2Int>(tiles);
            sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            foreach (Vector2Int tile in sorted)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Vector2Int next = tile + new Vector2Int(dx, dy);
                        if (!tiles.Contains(next) && seen.Add(next)) around.Add(next);
                    }
            return around;
        }
    }
}
