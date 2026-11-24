using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // One prop the planner put down on its own, as it may be moved later: the placement whose
    // rules it keeps to, where it stands, and which entry of the plan it is.
    internal sealed class PlacedPiece
    {
        public PlacedPiece(RoomPropPlacement placement, Vector2Int size, ICollection<int> zones, PropCandidate spot, int planIndex)
        {
            Placement = placement;
            Size      = size;
            Zones     = zones;
            Spot      = spot;
            PlanIndex = planIndex;
        }

        public RoomPropPlacement Placement { get; }
        // The footprint it was given, across and front to back (before turning).
        public Vector2Int Size { get; }
        // The zones it was limited to (null = anywhere in the room).
        public ICollection<int> Zones { get; }
        public PropCandidate Spot { get; set; }
        // Its place in RoomPropPlanner.Planned, which stays put when it moves.
        public int PlanIndex { get; }
    }
}
