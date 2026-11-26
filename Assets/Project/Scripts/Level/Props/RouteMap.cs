using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How far each floor tile of a room is from the routes people walk: the walkways between
    // its doorways and centre, plus the floor in front of props that mark a route (a service
    // door). Distances are steps over the floor plan, around inner walls and pillars but not
    // around props, so they hold still while a room is furnished. Concealment placements use
    // it to stand beside the routes (where people pass) or away from them (dead ends).
    internal sealed class RouteMap
    {
        public const int Unreached = FloorSteps.Unreached;

        private readonly RoomStructure _structure;
        private readonly HashSet<Vector2Int> _marked = new();
        // Measured on first use and again after a route is marked.
        private FloorSteps _steps;

        public RouteMap(RoomStructure structure) => _structure = structure;

        // A room with no route to measure from (no doorway) puts no limit on route distances.
        public bool HasRoutes => Steps.HasSources;

        // Steps from the tile to the nearest route: 0 on a route, Unreached where none leads.
        public int DistanceOf(Vector2Int tile) => _structure.Contains(tile) ? Steps.At(tile) : Unreached;

        // The nearest any tile of the rectangle comes to a route.
        public int DistanceOf(RectInt rect)
        {
            int nearest = Unreached;
            foreach (Vector2Int tile in rect.allPositionsWithin) nearest = Mathf.Min(nearest, DistanceOf(tile));
            return nearest;
        }

        public void Mark(Vector2Int tile)
        {
            if (_structure.Contains(tile) && _marked.Add(tile)) _steps = null;
        }

        private FloorSteps Steps => _steps ??= new FloorSteps(_structure, Routes(), RoomTileTags.Structure);

        private IEnumerable<Vector2Int> Routes()
        {
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                if (_structure.Has(tile, RoomTileTags.Walkway) || _marked.Contains(tile)) yield return tile;
        }
    }
}
