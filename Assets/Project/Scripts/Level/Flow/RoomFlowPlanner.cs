using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A room's flow: the doorway a player arrives by and how far into the room each tile lies
    // from it, in steps over the floor plan. The steps split the floor into three beats, the
    // way an encounter space is read on entry:
    //   - Threshold: just inside the way in. A foothold to take in the room from, so it is never
    //     a kill zone: enemies don't start there and a piece of low cover goes down in it.
    //   - Contest: the middle, where the fight happens.
    //   - Goal: the far end, where the onward doorways and the room's reward lie.
    // The bands are tile tags (RoomTileTags.Threshold, Goal); placements read them through
    // RoomPropPlacement.Band and the enemy planner through the tags. A room with no doorway
    // leading back toward Start has no arrival and no bands, so nothing changes in it.
    public static class RoomFlowPlanner
    {
        // The threshold reaches this share of the way to the room's far end, and at least MinThresholdSteps.
        public const float ThresholdShare = 0.25f;
        public const int MinThresholdSteps = 2;
        // The goal starts this share of the way in.
        public const float GoalShare = 0.65f;

        // The floor tile inside the doorway a player arrives by: one leading back toward Start,
        // through an ordinary passage before a shortcut or secret one. Null when none leads back.
        public static Vector2Int? ArrivalOf(LevelRoom room, IReadOnlyList<LevelDoorway> doorways)
        {
            Vector2Int? fallback = null;
            foreach (LevelDoorway doorway in doorways)
            {
                if (doorway.Room != room || doorway.Direction != DoorwayDirection.Back) continue;
                if (IsMainWay(doorway.Connection)) return doorway.RoomTile;
                fallback ??= doorway.RoomTile;
            }
            return fallback;
        }

        private static bool IsMainWay(MapConnection connection) =>
            connection == null || connection.Type is ConnectionType.Normal or ConnectionType.Locked;

        // Records the arrival and tags the threshold and goal, measured round the room's inner
        // walls and pillars (not its props, so the bands hold still while it is furnished).
        public static void Mark(RoomStructure structure, Vector2Int arrival)
        {
            structure.SetArrival(arrival);
            var steps = new FloorSteps(structure, arrival, RoomTileTags.Structure);
            int farthest = steps.Farthest();
            if (farthest <= MinThresholdSteps) return;

            int threshold = Mathf.Max(MinThresholdSteps, Mathf.FloorToInt(farthest * ThresholdShare));
            int goal = Mathf.Max(threshold + 1, Mathf.CeilToInt(farthest * GoalShare));
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                int at = steps.At(tile);
                if (at == FloorSteps.Unreached) continue;
                if (at <= threshold) structure.Tag(tile, RoomTileTags.Threshold);
                else if (at >= goal) structure.Tag(tile, RoomTileTags.Goal);
            }
        }

        // Whether a room, as it stands, would keep a way round once its flow is marked from
        // `arrival` (RoomFlank). Read on a copy, so `structure` keeps no flow tags of it.
        public static bool KeepsFlank(RoomStructure structure, Vector2Int arrival)
        {
            RoomStructure marked = structure.Copy();
            Mark(marked, arrival);
            return RoomFlank.Has(marked);
        }

        // Whether a tile lies in the band (Any and rooms without a flow take every tile).
        public static bool InBand(RoomStructure structure, Vector2Int tile, RoomFlowBand band) => band switch
        {
            RoomFlowBand.Threshold     => !structure.Arrival.HasValue || structure.Has(tile, RoomTileTags.Threshold),
            RoomFlowBand.PastThreshold => !structure.Has(tile, RoomTileTags.Threshold),
            RoomFlowBand.Goal          => !structure.Arrival.HasValue || structure.Has(tile, RoomTileTags.Goal),
            _                          => true,
        };
    }
}
