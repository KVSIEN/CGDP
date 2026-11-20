using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Whether a room offers a flank: a second way from its threshold to its far end that isn't
    // the main lane. Cyclic level design at the scale of one room — a fight with a loop in it
    // lets either side circle round cover instead of trading shots down one corridor, and keeps
    // a player from being pinned in a dead end.
    //
    // The main lane is the shortest way from the arrival to the far end (the farthest other
    // doorway, or the farthest goal tile in a dead-end room). It is closed off three tiles wide
    // through the middle of the room (the contest), and the far end must still be reachable.
    // Props count as in the way: a furnishing can close a flank the structure left open.
    public static class RoomFlank
    {
        // Rooms smaller than this are too small for a flank to mean anything.
        public const int MinFloorTiles = 48;

        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        // True for a room that has no flow, is too small to need one, or has a way round.
        public static bool Has(RoomStructure structure)
        {
            if (!structure.Arrival.HasValue || structure.Footprint.Count < MinFloorTiles) return true;

            Vector2Int arrival = structure.Arrival.Value;
            var steps = new FloorSteps(structure, arrival, Solid);
            if (!TryFarEnd(structure, steps, arrival, out Vector2Int farEnd)) return true;

            HashSet<Vector2Int> lane = LaneThroughContest(structure, steps.PathTo(farEnd, structure));
            return lane.Count == 0 || new FloorSteps(structure, arrival, Solid, lane).Reaches(farEnd);
        }

        // 1 for a fight room that has no flank, as a level off its priorities (RoomTraitFit).
        public static int Miss(RoomStructure structure) => Has(structure) ? 0 : 1;

        // The farthest other doorway in the goal that can be reached; without one, the farthest
        // goal tile. A doorway part-way in (in the contest) is no far end: its lane would wall it in.
        private static bool TryFarEnd(RoomStructure structure, FloorSteps steps, Vector2Int arrival, out Vector2Int farEnd)
        {
            farEnd = arrival;
            int best = 0;
            foreach (Vector2Int entrance in structure.Entrances)
                if (entrance != arrival && structure.Has(entrance, RoomTileTags.Goal) && steps.Reaches(entrance) && steps.At(entrance) > best)
                {
                    best   = steps.At(entrance);
                    farEnd = entrance;
                }
            if (best > 0) return true;

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, RoomTileTags.Goal) && steps.Reaches(tile) && steps.At(tile) > best)
                {
                    best   = steps.At(tile);
                    farEnd = tile;
                }
            return best > 0;
        }

        // The lane's tiles and their neighbours, in the contest only: the threshold and the goal
        // stay open, so a flank may leave and rejoin the lane there.
        private static HashSet<Vector2Int> LaneThroughContest(RoomStructure structure, List<Vector2Int> path)
        {
            var lane = new HashSet<Vector2Int>();
            foreach (Vector2Int tile in path)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var near = new Vector2Int(tile.x + dx, tile.y + dy);
                        if (structure.Contains(near) && !structure.Has(near, RoomTileTags.Threshold | RoomTileTags.Goal))
                            lane.Add(near);
                    }
            return lane;
        }
    }
}
