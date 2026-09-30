using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Lays out one room's structure once its doorways are known: tags the edge, corner,
    // centre and doorway tiles, reserves a walkway from every doorway to the centre, runs the
    // room function's structure rules, then works out the zones their inner walls make.
    public class RoomStructurePlanner
    {
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public RoomStructure Plan(LevelRoom room, IReadOnlyList<LevelDoorway> doorways, RandomStream random)
        {
            var structure = new RoomStructure(room.Footprint, room.WallHeight);
            Vector2Int centre = CentreTile(room);

            TagWalls(structure);
            TagCentre(structure, room);
            foreach (LevelDoorway doorway in doorways)
            {
                if (doorway.Room != room) continue;
                TagNearDoor(structure, doorway.RoomTile);
                TagWalkway(structure, doorway.RoomTile, centre);
            }

            if (room.Function != null)
                foreach (RoomStructureRule rule in room.Function.Structure)
                    if (rule != null) rule.Apply(structure, random);

            structure.AssignZones();
            return structure;
        }

        private static Vector2Int CentreTile(LevelRoom room) =>
            new(Mathf.FloorToInt(room.Anchor.x), Mathf.FloorToInt(room.Anchor.y));

        private static void TagWalls(RoomStructure structure)
        {
            RoomFootprint footprint = structure.Footprint;
            foreach (Vector2Int tile in footprint.Tiles)
            {
                bool horizontalWall = !footprint.Contains(tile + Vector2Int.left) || !footprint.Contains(tile + Vector2Int.right);
                bool verticalWall   = !footprint.Contains(tile + Vector2Int.up)   || !footprint.Contains(tile + Vector2Int.down);
                if (!footprint.IsInterior(tile, 1)) structure.Tag(tile, RoomTileTags.Edge);
                if (horizontalWall && verticalWall) structure.Tag(tile, RoomTileTags.Corner);
            }
        }

        // Same reach as the populator always kept clear around the centrepiece.
        private static void TagCentre(RoomStructure structure, LevelRoom room)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (Vector2.Distance(tile + Vector2.one * 0.5f, room.Anchor) < 1.5f)
                    structure.Tag(tile, RoomTileTags.Centre);
        }

        private static void TagNearDoor(RoomStructure structure, Vector2Int doorTile)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (Mathf.Abs(tile.x - doorTile.x) + Mathf.Abs(tile.y - doorTile.y) <= 2)
                    structure.Tag(tile, RoomTileTags.NearDoor);
        }

        // Shortest route over the floor (breadth-first), preferring to keep going straight.
        private static void TagWalkway(RoomStructure structure, Vector2Int from, Vector2Int to)
        {
            RoomFootprint footprint = structure.Footprint;
            if (!footprint.Contains(to)) return;

            var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var open = new Queue<Vector2Int>();
            open.Enqueue(from);
            while (open.Count > 0 && !cameFrom.ContainsKey(to))
            {
                Vector2Int tile = open.Dequeue();
                Vector2Int heading = tile - cameFrom[tile];
                if (heading != Vector2Int.zero) TryStep(tile, heading);
                foreach (Vector2Int side in Sides) TryStep(tile, side);

                void TryStep(Vector2Int current, Vector2Int dir)
                {
                    Vector2Int next = current + dir;
                    if (!footprint.Contains(next) || cameFrom.ContainsKey(next)) return;
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(to)) return;
            for (Vector2Int tile = to; ; tile = cameFrom[tile])
            {
                structure.Tag(tile, RoomTileTags.Walkway);
                if (tile == from) break;
            }
        }
    }
}
