using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Free-standing walls put up for a fight: a straight run or an L, full height to break a line
    // of sight or waist high to shoot over, standing across the room's long way so they cut the
    // longest lines. Each keeps a clear ring of floor round it, off the room's walls, other
    // walls, columns and anything built, so it reads as placed on purpose. It may line a walkway
    // but never crosses one, never stands in front of a doorway or on the centre spot, and never
    // divides the room. Can be aimed at one part of a room
    // (IFocusedStructureRule).
    [CreateAssetMenu(fileName = "CoverWallsStructureRule", menuName = "CGD/Level/Structure/Cover Walls")]
    public class CoverWallsStructureRule : RoomStructureRule, IFocusedStructureRule
    {
        private const int Attempts = 24;
        // Floor a wall's two sides may not stand on.
        private const RoomTileTags Kept = RoomTileTags.Structure | RoomTileTags.Stairs | RoomTileTags.Landing | RoomTileTags.NearDoor
                                        | RoomTileTags.Centre | RoomTileTags.Overpass | RoomTileTags.Prop;

        [Tooltip("Walls put up each time the rule runs")]
        [SerializeField] private IntRange _count = new(1, 2);
        [Tooltip("Tiles along each arm")]
        [SerializeField] private IntRange _length = new(2, 4);
        [Tooltip("Chance a wall turns a corner into an L")]
        [SerializeField, Range(0f, 1f)] private float _cornerChance = 0.5f;
        [Tooltip("Chance a wall is waist high (low cover) rather than full height")]
        [SerializeField, Range(0f, 1f)] private float _lowChance = 0.25f;
        [Tooltip("Metres tall when low")]
        [SerializeField] private float _lowHeight = 1.1f;
        [Tooltip("Chance a wall stands across the room's long way, cutting its longest lines of sight")]
        [SerializeField, Range(0f, 1f)] private float _acrossChance = 0.7f;
        [Tooltip("Tiles of floor kept clear round a wall: nothing built, no other wall, no column")]
        [SerializeField, Min(1)] private int _clearance = 1;

        public override void Apply(RoomStructure structure, RandomStream random) =>
            Apply(structure, random, structure.Footprint.Bounds);

        public void Apply(RoomStructure structure, RandomStream random, RectInt focus)
        {
            var spots = new List<Vector2Int>();
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (focus.Contains(tile) && !structure.Has(tile, Kept)) spots.Add(tile);
            if (spots.Count == 0) return;

            int count = Mathf.Max(1, _count.Evaluate(random));
            RectInt bounds = structure.Footprint.Bounds;
            for (int built = 0, attempt = 0; built < count && attempt < Attempts; attempt++)
                if (TryStand(structure, random.Pick(spots), focus, bounds.width >= bounds.height, random)) built++;
        }

        // A wall whose first stretch runs from `start`, between its row and the next, all of it inside `focus`.
        private bool TryStand(RoomStructure structure, Vector2Int start, RectInt focus, bool wide, RandomStream random)
        {
            // Across a wide room's long way (x) is a wall running along y, on the edges between columns.
            bool runsAlongY = random.Chance(_acrossChance) == wide;
            Vector2Int along = runsAlongY ? Vector2Int.up : Vector2Int.right;
            Vector2Int across = runsAlongY ? Vector2Int.right : Vector2Int.up;
            float height = random.Chance(_lowChance) ? CoverHeights.Readable(_lowHeight) : 0f;

            var edges = new List<PartitionEdge>();
            int first = Mathf.Max(1, _length.Evaluate(random));
            for (int i = 0; i < first; i++)
                edges.Add(new PartitionEdge(start + along * i, start + along * i + across, height, dividesZones: false));

            if (random.Chance(_cornerChance))
            {
                // The second arm leaves the first's far end, on the line between that tile and the next along.
                int second = Mathf.Max(1, _length.Evaluate(random));
                Vector2Int end = start + along * (first - 1);
                bool turnsAcross = random.Chance(0.5f);
                for (int j = 1; j <= second; j++)
                {
                    Vector2Int tile = turnsAcross ? end + across * j : end - across * (j - 1);
                    edges.Add(new PartitionEdge(tile, tile + along, height, dividesZones: false));
                }
            }

            foreach (PartitionEdge edge in edges)
                if (!focus.Contains(edge.Tile) || !focus.Contains(edge.Other) || !Fits(structure, edge.Tile, edge.Other)) return false;
            return structure.TryAddPartition(edges);
        }

        // Both tiles floor a tile in from the room's walls, off what is kept clear, the edge free
        // to close (not across a walkway), and no wall, column or anything built within the clearance.
        private bool Fits(RoomStructure structure, Vector2Int a, Vector2Int b)
        {
            if (!structure.Contains(a) || !structure.Contains(b) || structure.Has(a, Kept) || structure.Has(b, Kept)) return false;
            if (structure.EdgeMustStayOpen(a, b) || structure.IsPartitioned(a, b)) return false;
            if (structure.LevelOf(a) != structure.LevelOf(b)) return false;
            if (!structure.Footprint.IsInterior(a, 1) || !structure.Footprint.IsInterior(b, 1)) return false;
            return IsClear(structure, a) && IsClear(structure, b);
        }

        private bool IsClear(RoomStructure structure, Vector2Int centre)
        {
            for (int dx = -_clearance; dx <= _clearance; dx++)
                for (int dy = -_clearance; dy <= _clearance; dy++)
                {
                    Vector2Int tile = centre + new Vector2Int(dx, dy);
                    if (!structure.Contains(tile)) continue;
                    if (structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Stairs | RoomTileTags.Landing)) return false;
                    if (structure.IsPartitioned(tile, tile + Vector2Int.right) || structure.IsPartitioned(tile, tile + Vector2Int.up)) return false;
                }
            return true;
        }
    }
}
