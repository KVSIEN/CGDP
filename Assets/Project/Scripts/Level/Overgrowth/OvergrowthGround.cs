using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // Which of a room's tiles overgrowth leaves bare, so it never changes how the room plays:
    //   - the floor in front of a doorway, so the room meets its corridor flush;
    //   - stairs and slopes and their landings, and both sides of every ledge, so no drift piles
    //     into a ramp round a flight, onto a platform or over a railing;
    //   - the floor under a deck, which keeps its clearance;
    //   - solid furniture and pillars, and the centre spot, so they stand on the floor;
    //   - the floor beside low cover (a low prop, a broken column) and on both sides of every
    //     inner wall (low walls and wall stubs among them), so whoever crouches behind cover is
    //     still hidden by all of it;
    //   - the tiles a curved or chamfered wall cuts through (tagged Structure too);
    //   - with ClearWalkways, the walkways.
    // The room's own walls are not among them: growth piles up against those. The planner pins
    // bare tiles at 0, and the slope limit ramps the growth down to them.
    // Read once the room is furnished: furniture claims its floor as it is planned.
    public sealed class OvergrowthGround
    {
        private const RoomTileTags AlwaysBare =
            RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure | RoomTileTags.Prop
            | RoomTileTags.Stairs | RoomTileTags.Landing | RoomTileTags.Overpass;

        private readonly RoomStructure _structure;
        private readonly RoomTileTags _bare;
        private readonly HashSet<Vector2Int> _lowCover = new();

        public OvergrowthGround(RoomStructure structure, bool clearWalkways)
        {
            _structure = structure;
            _bare      = clearWalkways ? AlwaysBare | RoomTileTags.Walkway : AlwaysBare;

            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (CoverHeights.IsLow(structure.PropHeightAt(tile))) _lowCover.Add(tile);
            foreach (StructurePillar pillar in structure.Pillars)
                if (CoverHeights.IsLow(pillar.Height))
                    _lowCover.Add(new Vector2Int(Mathf.FloorToInt(pillar.Position.x), Mathf.FloorToInt(pillar.Position.y)));
        }

        // For tiles of the room; one outside it is never grown on.
        public bool StaysBare(Vector2Int tile)
        {
            if (!_structure.Contains(tile)) return true;
            if (_structure.Has(tile, _bare)) return true;

            int level = _structure.LevelOf(tile);
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                Vector2Int next = tile + side;
                if (!_structure.Contains(next)) continue;
                if (_structure.LevelOf(next) != level || _structure.IsPartitioned(tile, next) || _lowCover.Contains(next)) return true;
            }
            return false;
        }
    }
}
