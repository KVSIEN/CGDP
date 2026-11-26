using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // What counts as cover beside a floor tile, for every reading of a room that asks (the trait
    // measure's covered lanes, the post map's firing positions): something solid to duck behind
    // on the neighbouring tile, ground rising a level above the tile with no stairs between
    // (the face of a platform, the rim of a sunken floor) for whoever stands below it, or a half
    // wall along a ledge, for whoever stands either side of it.
    public static class RoomCover
    {
        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        // Whether `beside`, a neighbour of `tile`, shelters someone standing on `tile`. The room's own walls don't count.
        public static bool Shelters(RoomStructure structure, Vector2Int tile, Vector2Int beside)
        {
            if (!structure.Contains(beside)) return false;
            if (structure.Has(beside, Solid)) return true;
            if (structure.GuardOn(tile, beside) == LedgeGuard.HalfWall) return true;
            return structure.LevelOf(beside) > structure.LevelOf(tile) && structure.IsLedge(tile, beside);
        }

        // Whether a ledge drops from `tile` to one of its neighbours: the edge of a platform or the
        // rim of a sunken floor, where someone looks down on the floor below.
        public static bool OverlooksLowerFloor(RoomStructure structure, Vector2Int tile)
        {
            foreach (Vector2Int side in GridNeighbours.Four)
            {
                Vector2Int next = tile + side;
                if (structure.LevelOf(next) < structure.LevelOf(tile) && structure.IsLedge(tile, next)) return true;
            }
            return false;
        }
    }
}
