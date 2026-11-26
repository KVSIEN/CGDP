using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How well a room's required pieces fit a structure: they are put down on a copy of it, and
    // each one that finds a spot counts, one that needed its rules loosened for less. Only for
    // comparing two structures of the same room; the real furnishing draws its own spots.
    internal static class RoomAnchorFit
    {
        // Fixed, so both structures are tried with the same draws and no room seed moves.
        private const ulong TrySeed = 1UL;

        public static int Of(RoomStructure structure, Vector2 anchor, IReadOnlyList<RoomPropPlacement> placements)
        {
            RoomStructure trial = structure.Copy();
            trial.AssignZones();
            var planner = new RoomPropPlanner(trial, anchor, new RandomStream(new Seed(TrySeed)));
            foreach (RoomPropPlacement placement in placements) planner.Add(placement);

            int fit = 0;
            foreach (PropPlacementOutcome outcome in planner.Outcomes) fit += outcome.Placed * 2 - (outcome.Relaxed ? 1 : 0);
            return fit;
        }
    }
}
