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
            // Low edges by the line they lie on: (side, the coordinate across the line) → the
            // coordinates along it.
            var lines = new Dictionary<(Vector2Int Side, int Across), List<int>>();
            foreach (PartitionEdge edge in structure.Partitions)
            {
                if (!CoverHeights.IsLow(edge.Height) || structure.Has(edge.Tile, Taken) || structure.Has(edge.Other, Taken)) continue;
                bool vertical = edge.Side.x != 0;   // a wall between x and x + 1, running along y
                var key = (edge.Side, vertical ? edge.Tile.x : edge.Tile.y);
                if (!lines.TryGetValue(key, out List<int> along)) lines[key] = along = new List<int>();
                along.Add(vertical ? edge.Tile.y : edge.Tile.x);
            }

            foreach (KeyValuePair<(Vector2Int Side, int Across), List<int>> line in lines)
            {
                List<int> along = line.Value;
                along.Sort();
                int start = 0;
                for (int i = 1; i <= along.Count; i++)
                {
                    bool ends = i == along.Count || along[i] != along[i - 1] + 1 || i - start == MaxRunTiles;
                    if (!ends) continue;
                    links.Add(Across(line.Key.Side, line.Key.Across, along[start], along[i - 1], tileSize));
                    start = i;
                }
            }
        }

        // A link from the middle of the tiles on one side of the run to those on the other.
        private static VaultLink Across(Vector2Int side, int across, int first, int last, float tileSize)
        {
            float middle = (first + last + 1) * 0.5f;
            float width  = (last - first + 1) * tileSize * WidthShare;
            bool vertical = side.x != 0;
            Vector2 from = vertical ? new Vector2(across + 0.5f, middle) : new Vector2(middle, across + 0.5f);
            Vector2 to   = from + (Vector2)side;
            return new VaultLink(new Vector3(from.x, 0f, from.y) * tileSize, new Vector3(to.x, 0f, to.y) * tileSize, width);
        }
    }
}
