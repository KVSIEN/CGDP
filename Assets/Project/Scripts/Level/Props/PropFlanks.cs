using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Where a Flank placement's pairs go (PropSelection.Flank): a pair of props either side of
    // something the room is arranged round, the same distance out, lined up with its back and
    // facing the way it faces, so the two read as a frame. Plain geometry over the room: which
    // pairs suit the placement is the planner's to check.
    internal static class PropFlanks
    {
        // How many tiles past the placement's Flank Gap a pair may stand, at most: it takes the nearest that fits both.
        public const int MaxGap = 3;

        // Something to frame: its tiles and the way it faces. The pair stands either side of it
        // across that way, its back in line with the anchor's.
        public readonly struct Anchor
        {
            public Anchor(RectInt rect, Vector2Int front)
            {
                Rect  = rect;
                Front = front;
            }

            public RectInt    Rect  { get; }
            public Vector2Int Front { get; }
        }

        // The anchors a placement frames (what it stands Near, the doorways, the flights or beats
        // down the walkway), nearest the doorway a player arrives by first, so the
        // pairs a count allows go where they are seen. `keyed`: what the placement stands Near
        // put down (null when nothing).
        public static List<Anchor> Anchors(RoomStructure structure, RoomPropPlacement placement, IReadOnlyList<RectInt> keyed)
        {
            var anchors = new List<Anchor>();
            switch (placement.Flanks)
            {
                case FlankAnchor.Doorways:
                    foreach (Vector2Int entrance in structure.Entrances)
                        if (TryOutside(structure, entrance, out Vector2Int outside)) anchors.Add(new Anchor(new RectInt(entrance, Vector2Int.one), -outside));
                    break;
                case FlankAnchor.Stairs:
                    AddFlights(structure, placement.Floor, anchors);
                    break;
                case FlankAnchor.Route:
                    AddRoute(structure, placement.Rhythm, anchors);
                    break;
                default:
                    if (keyed != null)
                        foreach (RectInt rect in keyed) anchors.Add(new Anchor(rect, FrontOf(structure, rect)));
                    break;
            }

            Vector2 seenFrom = structure.Arrival.HasValue ? structure.Arrival.Value + Vector2.one * 0.5f : structure.Footprint.Anchor;
            anchors.Sort((a, b) => (a.Rect.center - seenFrom).sqrMagnitude.CompareTo((b.Rect.center - seenFrom).sqrMagnitude));
            return anchors;
        }

        // The two spots either side of `anchor`, `gap` tiles out, for a prop of `size` (across,
        // front to back) facing the anchor's way.
        public static (PropCandidate first, PropCandidate second) Pair(Anchor anchor, Vector2Int size, int gap)
        {
            Vector2Int front = anchor.Front;
            int rotation = GridNeighbours.IndexOf(front);
            Vector2Int extent = rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
            RectInt rect = anchor.Rect;

            // Its back in line with the anchor's back.
            int backX = front.x > 0 ? rect.xMin : front.x < 0 ? rect.xMax - extent.x : 0;
            int backY = front.y > 0 ? rect.yMin : front.y < 0 ? rect.yMax - extent.y : 0;

            RectInt first, second;
            if (front.x == 0)
            {
                first  = new RectInt(rect.xMin - gap - extent.x, backY, extent.x, extent.y);
                second = new RectInt(rect.xMax + gap, backY, extent.x, extent.y);
            }
            else
            {
                first  = new RectInt(backX, rect.yMin - gap - extent.y, extent.x, extent.y);
                second = new RectInt(backX, rect.yMax + gap, extent.x, extent.y);
            }
            return (new PropCandidate(first, rotation), new PropCandidate(second, rotation));
        }

        // The side of a doorway's tile the room's wall (and the doorway in it) is on.
        private static bool TryOutside(RoomStructure structure, Vector2Int entrance, out Vector2Int outside)
        {
            foreach (Vector2Int side in GridNeighbours.Four)
                if (!structure.Contains(entrance + side))
                {
                    outside = side;
                    return true;
                }
            outside = default;
            return false;
        }

        // Each flight, side by side flights climbing the same way from the same level as one: at
        // the top of it for a placement on raised floor, at its foot otherwise (a sunken floor's
        // flights stand inside the pit, so a pair on the ground floor frames their tops). Facing
        // away from the climb, toward whoever walks up to it.
        private static void AddFlights(RoomStructure structure, PropFloor floor, List<Anchor> anchors)
        {
            var seen = new HashSet<Vector2Int>();
            foreach (ElevatedArea area in structure.Elevations)
                foreach (StairFlight stairs in area.Stairs)
                {
                    if (!seen.Add(stairs.Tile)) continue;

                    // The whole wide flight this one is part of, along the step.
                    var across = new Vector2Int(Mathf.Abs(stairs.Up.y), Mathf.Abs(stairs.Up.x));
                    Vector2Int low = stairs.Tile, high = stairs.Tile;
                    while (SameFlight(structure, stairs, low - across)) seen.Add(low -= across);
                    while (SameFlight(structure, stairs, high + across)) seen.Add(high += across);

                    int topLevel = structure.LevelOf(stairs.Head);
                    bool atTop = floor == PropFloor.Raised || (floor == PropFloor.Ground && topLevel == 0);
                    Vector2Int shift = atTop ? stairs.Up : Vector2Int.zero;
                    Vector2Int min = Vector2Int.Min(low, high) + shift, max = Vector2Int.Max(low, high) + shift;
                    anchors.Add(new Anchor(new RectInt(min, max - min + Vector2Int.one), -stairs.Up));
                }
        }

        // Every Rhythm.x-th tile of the walkway counted from the doorway a player arrives by (the
        // first entrance when none is set), from the Rhythm.y-th on, facing the way the walkway runs
        // there: a pair either side of it keeps an even beat down the room's spine, and down each
        // branch to the other doorways. Steps are counted along the walkway itself.
        private static void AddRoute(RoomStructure structure, Vector2Int rhythm, List<Anchor> anchors)
        {
            int pitch = Mathf.Max(1, rhythm.x), first = Mathf.Max(0, rhythm.y);
            Vector2Int? start = structure.Arrival ?? (structure.Entrances.Count > 0 ? structure.Entrances[0] : (Vector2Int?)null);
            if (!start.HasValue || !structure.Has(start.Value, RoomTileTags.Walkway)) return;

            var steps = new Dictionary<Vector2Int, int> { [start.Value] = 0 };
            var open = new Queue<Vector2Int>();
            open.Enqueue(start.Value);
            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                int step = steps[tile];
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int next = tile + side;
                    if (steps.ContainsKey(next) || !structure.Has(next, RoomTileTags.Walkway) || !structure.CanStep(tile, next)) continue;
                    steps[next] = step + 1;
                    open.Enqueue(next);
                    if (step + 1 >= first && (step + 1 - first) % pitch == 0) anchors.Add(new Anchor(new RectInt(next, Vector2Int.one), side));
                }
            }
        }

        private static bool SameFlight(RoomStructure structure, StairFlight stairs, Vector2Int tile) =>
            structure.TryGetStairs(tile, out StairFlight other) && other.Up == stairs.Up
            && structure.LevelOf(tile) == structure.LevelOf(stairs.Tile);

        // The way a keyed prop faces, for framing it: away from the wall it backs onto, else
        // toward the doorway a player arrives by (or the room's middle), so the pair frames it as
        // it is first seen.
        private static Vector2Int FrontOf(RoomStructure structure, RectInt rect)
        {
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                PropPlacementRules.SideSpan(rect, side, out Vector2Int first, out Vector2Int step, out int length);
                bool walled = true;
                for (int i = 0; i < length && walled; i++) walled = structure.IsWall(first + step * i, side);
                if (walled) return -side;
            }

            Vector2 toward = (structure.Arrival.HasValue ? structure.Arrival.Value + Vector2.one * 0.5f : structure.Footprint.Anchor) - rect.center;
            Vector2Int front = GridNeighbours.Four[0];
            float best = float.NegativeInfinity;
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                float facing = Vector2.Dot(side, toward);
                if (facing > best)
                {
                    best = facing;
                    front = side;
                }
            }
            return front;
        }
    }
}
