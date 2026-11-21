using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Builds a blueprint into a room's structure: an inner wall on every edge between two spaces
    // (and, with a shell, between the building and the outside), a doorway with a lintel
    // wherever the blueprint has a door, and each space registered to be furnished by its own
    // function. The doorways keep their spaces as zones of their own. With a shell (a building
    // among streets) the spaces are roofed too.
    //
    // All or nothing: the walls go in through RoomStructure.TryAddPartition, so if they would
    // cut any floor off from the room's doorways nothing is built and Write returns false.
    public static class BuildingBlueprintWriter
    {
        // `shell`: wall the building off from the floor around it, with its front door (a building
        // standing among streets); without one, the room's own walls are the building's.
        // `keepRoutesOpen`: walls beside the room's doorways and routes become doorways instead
        // (a building that is the room whole); off for a lot, whose interior is the blueprint's alone.
        public static bool Write(RoomStructure structure, BuildingBlueprint blueprint, bool shell, bool keepRoutesOpen)
        {
            var owner = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < blueprint.Spaces.Count; i++)
                foreach (Vector2Int tile in blueprint.Spaces[i].Rect.allPositionsWithin)
                    owner[tile] = i;

            float doorHeight = blueprint.Plan.DoorHeight;
            HashSet<(Vector2Int, Vector2Int)> doors = DoorEdges(structure, blueprint, shell, doorHeight);

            var walls = new List<PartitionEdge>();
            var openings = new List<PartitionEdge>();
            foreach (KeyValuePair<Vector2Int, int> entry in owner)
            {
                Vector2Int tile = entry.Key;
                if (!structure.Contains(tile)) continue;

                foreach (Vector2Int side in GridSides.All)
                {
                    Vector2Int next = tile + side;
                    bool inside = owner.TryGetValue(next, out int nextOwner);
                    // An edge between two spaces is seen from both; take it from the one below or left.
                    if (inside && (nextOwner == entry.Value || side.x < 0 || side.y < 0)) continue;
                    if (!structure.Contains(next) || (!inside && !shell)) continue;

                    var wall = new PartitionEdge(tile, next, 0f, dividesZones: false);
                    bool door = doors.Contains((wall.Tile, wall.Side));
                    bool forced = inside && keepRoutesOpen && structure.EdgeMustStayOpen(tile, next);
                    if (door || forced) openings.Add(new PartitionEdge(tile, next, doorHeight, dividesZones: false));
                    else walls.Add(wall);
                }
            }

            if (walls.Count == 0 || !structure.TryAddPartition(walls)) return false;

            foreach (PartitionEdge opening in openings)
            {
                structure.AddOpening(opening, separatesZones: true);
                // Kept free so no prop blocks a doorway.
                structure.Tag(opening.Tile, RoomTileTags.Access);
                structure.Tag(opening.Other, RoomTileTags.Access);
            }
            foreach (string room in blueprint.Missing) structure.NoteLeftOut(room);
            foreach (PlannedSpace space in blueprint.Spaces)
            {
                TagWalls(structure, space.Rect);
                if (TryFirstFloor(structure, space.Rect, out Vector2Int first))
                    structure.AddSpace(first, space.Function, space.Name, space.Layout, roofed: shell);
            }
            return true;
        }

        // The inner walls are walls to furnish against too: tiles beside one are Edge, and tiles
        // in a corner of a room Corner, as along the room's own walls (RoomStructurePlanner).
        internal static void TagWalls(RoomStructure structure, RectInt rect)
        {
            foreach (Vector2Int tile in rect.allPositionsWithin)
            {
                if (!structure.Contains(tile)) continue;

                bool acrossX = structure.IsWall(tile, Vector2Int.left) || structure.IsWall(tile, Vector2Int.right);
                bool acrossY = structure.IsWall(tile, Vector2Int.up)   || structure.IsWall(tile, Vector2Int.down);
                if (acrossX || acrossY) structure.Tag(tile, RoomTileTags.Edge);
                if (acrossX && acrossY) structure.Tag(tile, RoomTileTags.Corner);
            }
        }

        // The edges the blueprint's doorways stand on, in the order PartitionEdge keeps them.
        private static HashSet<(Vector2Int, Vector2Int)> DoorEdges(RoomStructure structure, BuildingBlueprint blueprint, bool shell, float height)
        {
            var edges = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (BuildingDoor door in blueprint.Doors)
                Add(door);
            if (shell) Add(blueprint.FrontDoor);
            return edges;

            void Add(BuildingDoor doorway)
            {
                if (!structure.Contains(doorway.A) || !structure.Contains(doorway.B)) return;
                var edge = new PartitionEdge(doorway.A, doorway.B, height, dividesZones: false);
                edges.Add((edge.Tile, edge.Side));
            }
        }

        private static bool TryFirstFloor(RoomStructure structure, RectInt rect, out Vector2Int found)
        {
            foreach (Vector2Int tile in rect.allPositionsWithin)
            {
                if (!structure.Contains(tile)) continue;
                found = tile;
                return true;
            }
            found = default;
            return false;
        }
    }
}
