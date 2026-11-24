using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // What each floor tile of a furnished room offers an enemy (RoomPostKinds): concealment
    // from the concealment props over it, overwatch and chokes from the sightlines, dead ends
    // and routes from the walkways. Read once the room's props are planned.
    public sealed class RoomPostMap
    {
        public const int OverwatchLaneTiles = RoomTraitMeasure.LaneMediumTiles;
        public const int ChokeTiles         = RoomTraitMeasure.NarrowTiles;
        public const int DeadEndSteps       = 3;
        public const int RouteSteps         = 1;

        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        private readonly RoomStructure _structure;
        private readonly SightlineMap _sight;
        // What can be seen from each tile, in every direction; null when chokes and overwatch are read along rows and columns.
        private readonly RoomVisibility _visibility;
        private readonly RouteMap _routes;
        private readonly HashSet<Vector2Int> _concealed = new();
        // Each tile's kinds once worked out: the room is finished by now, and a plan asks about
        // every floor tile again for every enemy it places.
        private readonly RectInt _bounds;
        private readonly RoomPostKinds[] _kinds;
        private readonly bool[] _known;
        private int _openTiles = -1;

        // `isovist`: read chokes and overwatch from what can be seen in every direction (IsovistUse.Posts).
        public RoomPostMap(RoomStructure structure, IReadOnlyList<PlannedProp> props, bool isovist = false)
        {
            _structure = structure;
            _sight     = new SightlineMap(structure);
            if (isovist) _visibility = new RoomVisibility(structure, _sight.Blocking);
            _routes    = new RouteMap(structure);
            _bounds    = structure.Footprint.Bounds;
            _kinds     = new RoomPostKinds[_bounds.width * _bounds.height];
            _known     = new bool[_kinds.Length];

            foreach (PlannedProp prop in props)
            {
                if (!RoomTraitMeasure.Conceals(prop.Prefab)) continue;
                foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                    if (structure.Contains(tile)) _concealed.Add(tile);
            }
        }

        // Nothing for tiles off the floor or taken by a pillar or prop.
        public RoomPostKinds KindsAt(Vector2Int tile)
        {
            if (!_structure.Contains(tile)) return RoomPostKinds.None;

            int index = (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);
            if (!_known[index])
            {
                _kinds[index] = Measure(tile);
                _known[index] = true;
            }
            return _kinds[index];
        }

        private RoomPostKinds Measure(Vector2Int tile)
        {
            if (_structure.Has(tile, Solid)) return RoomPostKinds.None;

            RoomPostKinds kinds = RoomPostKinds.None;
            if (_concealed.Contains(tile)) kinds |= RoomPostKinds.Concealed;

            kinds |= _visibility != null ? SeenFrom(tile) : SightAlongRuns(tile);

            if (_routes.HasRoutes)
            {
                int steps = _routes.DistanceOf(tile);
                if (steps <= RouteSteps) kinds |= RoomPostKinds.Route;
                else if (steps >= DeadEndSteps && steps != RouteMap.Unreached) kinds |= RoomPostKinds.DeadEnd;
            }
            return kinds;
        }

        // A choke is floor hemmed in to a corridor's width; overwatch a long open lane with cover beside it.
        private RoomPostKinds SightAlongRuns(Vector2Int tile)
        {
            (int alongX, int alongY) = _sight.Runs(tile);
            RoomPostKinds kinds = RoomPostKinds.None;
            if (Mathf.Min(alongX, alongY) <= ChokeTiles) kinds |= RoomPostKinds.Choke;
            if (Mathf.Max(alongX, alongY) >= OverwatchLaneTiles && HasCoverBeside(tile)) kinds |= RoomPostKinds.Overwatch;
            return kinds;
        }

        // The same from what can be seen in every direction: a choke is a tile that sees little of the room
        // (RoomSightReading.EnclosedShare of the open floor or less), overwatch one with a long line of sight and cover beside it.
        private RoomPostKinds SeenFrom(Vector2Int tile)
        {
            RoomPostKinds kinds = RoomPostKinds.None;
            if (_visibility.Area(tile) <= RoomSightReading.EnclosedShare * OpenTiles()) kinds |= RoomPostKinds.Choke;
            if (_visibility.Reach(tile) + 1f >= OverwatchLaneTiles && HasCoverBeside(tile)) kinds |= RoomPostKinds.Overwatch;
            return kinds;
        }

        private int OpenTiles()
        {
            if (_openTiles >= 0) return _openTiles;

            _openTiles = 0;
            foreach (Vector2Int tile in _structure.Footprint.Tiles)
                if (_visibility.Area(tile) > 0) _openTiles++;
            return _openTiles;
        }

        // A pillar, a prop or an inner wall right next to the tile (the room's own walls don't count).
        private bool HasCoverBeside(Vector2Int tile)
        {
            foreach (Vector2Int side in GridSides.All)
            {
                Vector2Int next = tile + side;
                if (_structure.IsPartitioned(tile, next)) return true;
                if (_structure.Contains(next) && _structure.Has(next, Solid)) return true;
            }
            return false;
        }
    }
}
