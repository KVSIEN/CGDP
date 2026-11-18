using System.Collections.Generic;

namespace CGD.Level
{
    // Where a building's rooms go: rectangles that tile the whole lot, the doorways between
    // them and the front door. Plain data, no walls yet (BuildingBlueprintWriter builds those).
    public sealed class BuildingBlueprint
    {
        public BuildingBlueprint(BuildingPlan plan)
        {
            Plan = plan;
        }

        public BuildingPlan Plan { get; }
        public List<PlannedSpace> Spaces { get; } = new();
        // Between rooms and the hall (and the hall and the room across its end).
        public List<BuildingDoor> Doors { get; } = new();
        // Tile inside the building, then the tile outside it.
        public BuildingDoor FrontDoor { get; set; }
        // Rooms the plan asked for that didn't fit.
        public List<string> Missing { get; } = new();
    }
}
