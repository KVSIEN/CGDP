using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Furnishes a room from its function's prop placements, in order: each picks random
    // tiles that carry its tags, sit in its zone and keep its spacing, and marks them as
    // taken (Prop) so later placements, scattered props and enemies go elsewhere.
    public static class RoomPropPlanner
    {
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        // How far an AgainstWall prop moves from its tile's middle toward the wall, in tiles.
        private const float WallOffset = 0.25f;

        public static List<PlannedProp> Plan(LevelRoom room, RandomStream random)
        {
            var planned = new List<PlannedProp>();
            if (room.Function == null || room.Structure == null) return planned;

            RoomStructure structure = room.Structure;
            int mainZone = LargestZone(structure);

            foreach (RoomPropPlacement placement in room.Function.Props)
            {
                if (placement == null || placement.Prefabs.Length == 0) continue;

                List<Vector2Int> candidates = Candidates(structure, placement, mainZone);
                random.Shuffle(candidates);
                int count = placement.Count.Evaluate(random);
                var placed = new List<Vector2Int>();

                foreach (Vector2Int tile in candidates)
                {
                    if (placed.Count >= count) break;
                    if (structure.Has(tile, placement.Avoid) || TooClose(tile, placed, placement.Spacing)) continue;

                    GameObject prefab = random.Pick(placement.Prefabs);
                    if (prefab == null) continue;

                    planned.Add(Orient(structure, tile, placement.Facing, room.Anchor, prefab, random));
                    structure.Tag(tile, RoomTileTags.Prop);
                    placed.Add(tile);
                }
            }
            return planned;
        }

        private static List<Vector2Int> Candidates(RoomStructure structure, RoomPropPlacement placement, int mainZone)
        {
            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (placement.On != RoomTileTags.None && !structure.Has(tile, placement.On)) continue;
                if (structure.Has(tile, placement.Avoid)) continue;
                if (placement.Zone == PropZone.Largest && structure.ZoneOf(tile) != mainZone) continue;
                if (placement.Zone == PropZone.Others  && structure.ZoneOf(tile) == mainZone) continue;
                if (placement.Facing == PropFacing.AgainstWall && !TryFindWall(structure, tile, out _)) continue;
                tiles.Add(tile);
            }
            return tiles;
        }

        private static PlannedProp Orient(RoomStructure structure, Vector2Int tile, PropFacing facing, Vector2 anchor, GameObject prefab, RandomStream random)
        {
            Vector2 middle = tile + new Vector2(0.5f, 0.5f);
            switch (facing)
            {
                case PropFacing.AgainstWall when TryFindWall(structure, tile, out Vector2Int wall):
                    return new PlannedProp(prefab, middle + (Vector2)wall * WallOffset, Yaw(-(Vector2)wall));
                case PropFacing.TowardCentre when (anchor - middle).sqrMagnitude > 0.01f:
                    return new PlannedProp(prefab, middle, Yaw(anchor - middle));
                case PropFacing.Random:
                    return new PlannedProp(prefab, middle, random.Range(0f, 360f));
                default:
                    return new PlannedProp(prefab, middle, random.Range(0, 4) * 90f);
            }
        }

        // A side of the tile with a wall or inner wall — the first one found.
        private static bool TryFindWall(RoomStructure structure, Vector2Int tile, out Vector2Int side)
        {
            foreach (Vector2Int s in Sides)
            {
                if (structure.Footprint.Contains(tile + s) && !structure.IsPartitioned(tile, tile + s)) continue;
                side = s;
                return true;
            }
            side = default;
            return false;
        }

        // Unity yaw: 0° faces +Z (tile +y), 90° faces +X.
        private static float Yaw(Vector2 forward) => Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg;

        private static bool TooClose(Vector2Int tile, List<Vector2Int> placed, int spacing)
        {
            foreach (Vector2Int other in placed)
                if (Mathf.Max(Mathf.Abs(other.x - tile.x), Mathf.Abs(other.y - tile.y)) <= spacing) return true;
            return false;
        }

        private static int LargestZone(RoomStructure structure)
        {
            var sizes = new int[structure.ZoneCount];
            foreach (Vector2Int tile in structure.Footprint.Tiles) sizes[structure.ZoneOf(tile)]++;

            int largest = 0;
            for (int zone = 1; zone < sizes.Length; zone++)
                if (sizes[zone] > sizes[largest]) largest = zone;
            return largest;
        }
    }
}
