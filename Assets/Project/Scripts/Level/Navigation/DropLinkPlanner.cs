using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where enemies may drop off an open ledge to the floor a level below (RoomStructure.CanDropDown):
    // each straight run of open ledge becomes a one-way link down across its middle, a few tiles
    // wide at most. Railed ledges, stairs and walls are never crossed. Plain data from the layout;
    // VaultLinkBuilder adds them to the NavMesh where they save a walk round to the stairs.
    public static class DropLinkPlanner
    {
        // Nobody stands on, or lands on, a pillar, a prop or a mound's corner.
        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        public static List<VaultLink> Plan(LevelLayout layout)
        {
            var links = new List<VaultLink>();
            foreach (LevelRoom room in layout.Rooms.Values)
                if (room.Structure != null && room.Structure.HasElevation) PlanRoom(room.Structure, layout.TileSize, links);
            return links;
        }

        private static void PlanRoom(RoomStructure structure, float tileSize, List<VaultLink> links)
        {
            // Grouped by the level dropped from, so one run never mixes two heights.
            var edges = new List<(Vector2Int, Vector2Int, int)>();
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                foreach (Vector2Int side in GridSides.All)
                    if (structure.CanDropDown(tile, tile + side) && !structure.Has(tile, Solid) && !structure.Has(tile + side, Solid))
                        edges.Add((tile, side, structure.LevelOf(tile)));

            foreach (EdgeRuns.Run run in EdgeRuns.Of(edges, VaultLinkPlanner.MaxRunTiles))
            {
                (Vector2 from, Vector2 to) = EdgeRuns.Middles(run);
                links.Add(new VaultLink(VaultLinkPlanner.OnFloor(structure, from, tileSize), VaultLinkPlanner.OnFloor(structure, to, tileSize),
                                        EdgeRuns.Length(run) * tileSize * VaultLinkPlanner.WidthShare, oneWay: true));
            }
        }
    }
}
