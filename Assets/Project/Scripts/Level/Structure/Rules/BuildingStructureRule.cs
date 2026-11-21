using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // The whole room is one building: a hall with rooms along it, each walled in with a doorway
    // onto the hall and furnished by its own place (BuildingPlan). The hall runs in from the
    // side of the room nearest its first doorway, so the way in from the corridor leads into it.
    // A room too small for the plan is left bare.
    [CreateAssetMenu(fileName = "BuildingStructureRule", menuName = "CGD/Level/Structure/Building")]
    public class BuildingStructureRule : RoomStructureRule
    {
        [SerializeField] private BuildingPlan _plan;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (_plan == null) return;

            var planner = new BuildingLayoutPlanner(_plan);
            RectInt bounds = structure.Footprint.Bounds;
            foreach (Vector2Int front in FrontsNearestFirst(structure, bounds, random))
            {
                BuildingBlueprint blueprint = planner.Plan(bounds, front, random);
                if (blueprint != null && BuildingBlueprintWriter.Write(structure, blueprint, shell: false, keepRoutesOpen: true)) return;
            }
        }

        // The sides of the room, nearest the first doorway first (random order without one).
        private static List<Vector2Int> FrontsNearestFirst(RoomStructure structure, RectInt bounds, RandomStream random)
        {
            var fronts = new List<Vector2Int>(GridSides.All);
            random.Shuffle(fronts);
            if (structure.Entrances.Count == 0) return fronts;

            Vector2Int door = structure.Entrances[0];
            fronts.Sort((a, b) => DistanceToSide(door, bounds, a).CompareTo(DistanceToSide(door, bounds, b)));
            return fronts;
        }

        private static int DistanceToSide(Vector2Int tile, RectInt bounds, Vector2Int side) =>
            side.x > 0 ? bounds.xMax - 1 - tile.x
          : side.x < 0 ? tile.x - bounds.xMin
          : side.y > 0 ? bounds.yMax - 1 - tile.y
          :              tile.y - bounds.yMin;
    }
}
