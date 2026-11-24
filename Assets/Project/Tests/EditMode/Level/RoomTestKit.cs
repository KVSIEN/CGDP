using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Level;

namespace CGD.Tests
{
    // Shared setup for the room dressing and room style tests.
    internal static class RoomTestKit
    {
        public const int Seeds = 60;

        // A bare rectangular room with doorways at `entrances`, tagged the way the level builder does.
        public static RoomStructure Room(int width, int height, params Vector2Int[] entrances)
        {
            var footprint = RoomFootprint.Rectangle(new RectInt(0, 0, width, height));
            RoomStructure structure = new RoomStructurePlanner().Prepare(footprint, 4f, entrances);
            structure.AssignZones();
            return structure;
        }

        // A room with an inner wall of `wallHeight` (0 = full height) between columns wallX - 1 and wallX
        // in every row but the bottom one, so the room stays whole.
        public static RoomStructure WalledRoom(int width, int height, int wallX, float wallHeight, params Vector2Int[] entrances)
        {
            RoomStructure room = Room(width, height, entrances);
            var edges = new List<PartitionEdge>();
            for (int y = 1; y < height; y++)
                edges.Add(new PartitionEdge(new Vector2Int(wallX - 1, y), new Vector2Int(wallX, y), wallHeight, dividesZones: false));
            if (!room.TryAddPartition(edges)) throw new InvalidOperationException("The wall cut the room in two.");
            return room;
        }

        public static RoomPropPlanner Planner(RoomStructure structure, int seed, float fillShare = RoomPropPlanner.DefaultFillShare, float countScale = 1f) =>
            new(structure, structure.Footprint.Anchor, new RandomStream(new Seed((ulong)seed)), fillShare, countScale);

        public static RoomPropPlacement Placement(int min, int max, PropFacing facing = PropFacing.Square, int spacing = 0) => new()
        {
            Prefabs = new GameObject[1],
            Count   = new IntRange(min, max),
            Facing  = facing,
            Spacing = spacing,
        };

        public static Vector2Int TileOf(PlannedProp prop) =>
            new(Mathf.FloorToInt(prop.Position.x), Mathf.FloorToInt(prop.Position.y));

        // The side the prop faces, from its yaw (0 = +y, 90 = +x).
        public static Vector2Int Forward(PlannedProp prop)
        {
            int turns = Mathf.RoundToInt(prop.Yaw / 90f) & 3;
            return new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }[turns];
        }

        public static int TilesWith(RoomStructure structure, RoomTileTags tag)
        {
            int count = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (structure.Has(tile, tag)) count++;
            return count;
        }
    }
}
