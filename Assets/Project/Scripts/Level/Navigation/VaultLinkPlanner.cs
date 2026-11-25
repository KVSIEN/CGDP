using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Where a generated level's low inner walls (low cover: wall stubs, counters, office
    // partitions) can be hopped: each straight run of low wall becomes a link across its
    // middle, at most a few tiles wide so a long wall gets several, broken where something
    // stands against either side. Full-height walls and openings are never crossed. Plain
    // data from the layout; VaultLinkBuilder decides which are worth adding to the NavMesh.
    public static class VaultLinkPlanner
    {
        // Tiles of wall one link covers, and the share of that it may be crossed along.
        public const int   MaxRunTiles = 4;
        public const float WidthShare  = 0.9f;

        private const RoomTileTags Taken = RoomTileTags.Structure | RoomTileTags.Prop;

        public static List<VaultLink> Plan(LevelLayout layout)
        {
            var links = new List<VaultLink>();
            foreach (LevelRoom room in layout.Rooms.Values)
                if (room.Structure != null) PlanRoom(room.Structure, layout.TileSize, links);
            return links;
        }

        private static void PlanRoom(RoomStructure structure, float tileSize, List<VaultLink> links)
        {
            var edges = new List<(Vector2Int, Vector2Int, int)>();
            foreach (PartitionEdge edge in structure.Partitions)
            {
                if (!CoverHeights.IsLow(edge.Height) || structure.Has(edge.Tile, Taken) || structure.Has(edge.Other, Taken)) continue;
                edges.Add((edge.Tile, edge.Side, 0));
            }

            // A link from the middle of the tiles on one side of the run to those on the other.
            foreach (EdgeRuns.Run run in EdgeRuns.Of(edges, MaxRunTiles))
            {
                (Vector2 from, Vector2 to) = EdgeRuns.Middles(run);
                links.Add(new VaultLink(OnFloor(structure, from, tileSize), OnFloor(structure, to, tileSize), EdgeRuns.Length(run) * tileSize * WidthShare));
            }
        }

        // A point in tile units, in metres on the floor of the tile it falls in (raised or sunken floor included).
        internal static Vector3 OnFloor(RoomStructure structure, Vector2 point, float tileSize)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            return new Vector3(point.x * tileSize, RoomElevation.HeightOf(structure.LevelOf(tile)), point.y * tileSize);
        }
    }
}
