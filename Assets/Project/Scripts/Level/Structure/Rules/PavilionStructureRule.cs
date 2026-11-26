using System;
using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Stands small walled structures in a room's open floor: a restroom block or bar hut in a
    // pool hall, a kiosk in a promenade, a site office in a cargo bay. Each is a block you walk
    // round, with one doorway facing the room's middle, and is furnished inside by its own
    // place as an annex: the room keeps its own function, cover and props round it.
    //
    // A pavilion only goes on open floor (no route, doorway, centre spot or pillar) with free
    // floor all round it, and its walls go in through TryAddPartition, so the room stays whole.
    // List it after the room's pillar rules, so no pillar lands inside one.
    [CreateAssetMenu(fileName = "PavilionStructureRule", menuName = "CGD/Level/Structure/Pavilions")]
    public class PavilionStructureRule : RoomStructureRule
    {
        // Spots looked at for each pavilion, best first by its placement, before giving up.
        private const int SpotsTried = 16;

        [Tooltip("What a pavilion can be, one picked by weight for each")]
        [SerializeField] private PavilionKind[] _kinds = Array.Empty<PavilionKind>();
        [Tooltip("How many a room big enough gets")]
        [SerializeField] private IntRange _count = new(1, 2);
        [Tooltip("Floor tiles the room needs for each pavilion: a smaller room gets fewer, or none")]
        [SerializeField, Min(1)] private int _tilesEach = 80;
        [Tooltip("Free floor kept all round each pavilion, in tiles")]
        [SerializeField, Min(1)] private int _margin = 1;
        [SerializeField] private PavilionPlacement _placement;
        [Tooltip("Metres; 0 = the room's full wall height. Pulled into the low or high cover band: a hut wants high (2.2+)")]
        [SerializeField, Min(0f)] private float _wallHeight = 2.6f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            if (_kinds.Length == 0) return;

            int wanted = Mathf.Min(_count.Evaluate(random), structure.Footprint.Count / _tilesEach);
            var placed = new List<RectInt>();
            for (int i = 0; i < wanted; i++)
            {
                PavilionKind kind = random.PickWeighted(_kinds, k => k != null && k.Function != null ? k.Weight : 0f);
                if (kind == null || kind.Function == null) return;

                Vector2Int size = RollSize(kind, random);
                foreach (RectInt spot in BestSpots(structure, size, placed, random))
                {
                    if (!TryBuild(structure, spot, kind)) continue;
                    placed.Add(spot);
                    break;
                }
            }
        }

        private static Vector2Int RollSize(PavilionKind kind, RandomStream random)
        {
            var min = Vector2Int.Max(Vector2Int.one * 2, kind.MinSize);
            var max = Vector2Int.Max(min, kind.MaxSize);
            var size = new Vector2Int(random.Range(min.x, max.x + 1), random.Range(min.y, max.y + 1));
            return random.Chance(0.5f) ? new Vector2Int(size.y, size.x) : size;
        }

        // Every spot the pavilion fits, ordered by its placement (random among equals), the first few.
        private List<RectInt> BestSpots(RoomStructure structure, Vector2Int size, List<RectInt> placed, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            var spots = new List<RectInt>();
            for (int y = bounds.yMin; y <= bounds.yMax - size.y; y++)
                for (int x = bounds.xMin; x <= bounds.xMax - size.x; x++)
                {
                    var rect = new RectInt(x, y, size.x, size.y);
                    if (Fits(structure, rect, placed)) spots.Add(rect);
                }

            random.Shuffle(spots);
            Vector2 anchor = structure.Footprint.Anchor;
            if (_placement == PavilionPlacement.Centre)
                spots.Sort((a, b) => (a.center - anchor).sqrMagnitude.CompareTo((b.center - anchor).sqrMagnitude));
            else if (_placement == PavilionPlacement.Perimeter)
                spots.Sort((a, b) => (b.center - anchor).sqrMagnitude.CompareTo((a.center - anchor).sqrMagnitude));

            if (spots.Count > SpotsTried) spots.RemoveRange(SpotsTried, spots.Count - SpotsTried);
            return spots;
        }

        // Open floor inside, free floor (no pillar, no other pavilion) all round within the margin.
        private bool Fits(RoomStructure structure, RectInt rect, List<RectInt> placed)
        {
            foreach (Vector2Int tile in rect.allPositionsWithin)
                if (!structure.IsOpen(tile) || !structure.IsFree(tile)) return false;

            var ring = new RectInt(rect.x - _margin, rect.y - _margin, rect.width + _margin * 2, rect.height + _margin * 2);
            foreach (Vector2Int tile in ring.allPositionsWithin)
            {
                if (!structure.Contains(tile) || structure.Has(tile, RoomTileTags.Structure)) return false;
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (ring.Contains(tile + side) && structure.IsPartitioned(tile, tile + side)) return false;
            }

            var apart = new RectInt(ring.x - 1, ring.y - 1, ring.width + 2, ring.height + 2);
            foreach (RectInt other in placed)
                if (apart.Overlaps(other)) return false;
            return true;
        }

        // Walls the rect in with a doorway on the side facing the room's middle, and registers
        // its inside as an annex furnished by the kind's place. False when the walls would cut
        // floor off; nothing is built then.
        private bool TryBuild(RoomStructure structure, RectInt rect, PavilionKind kind)
        {
            if (!TryDoor(structure, rect, out PartitionEdge door)) return false;

            float height = CoverHeights.Readable(_wallHeight);
            var walls = new List<PartitionEdge>();
            foreach (Vector2Int tile in rect.allPositionsWithin)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int outside = tile + side;
                    if (rect.Contains(outside)) continue;
                    var wall = new PartitionEdge(tile, outside, height, dividesZones: false);
                    if (wall.Tile != door.Tile || wall.Side != door.Side) walls.Add(wall);
                }
            if (!structure.TryAddPartition(walls)) return false;

            structure.AddOpening(door, separatesZones: true);
            structure.Tag(door.Tile, RoomTileTags.Access);
            structure.Tag(door.Other, RoomTileTags.Access);
            BuildingBlueprintWriter.TagWalls(structure, rect);
            structure.AddSpace(rect.min, kind.Function, kind.Name, annex: true);
            return true;
        }

        // The doorway: the middle of the side that faces the room's anchor, or the next best
        // side whose outside tile is open floor.
        private static bool TryDoor(RoomStructure structure, RectInt rect, out PartitionEdge door)
        {
            Vector2 toAnchor = structure.Footprint.Anchor - rect.center;
            var sides = new List<Vector2Int>(GridNeighbours.Four);
            sides.Sort((a, b) => Vector2.Dot(b, toAnchor).CompareTo(Vector2.Dot(a, toAnchor)));

            foreach (Vector2Int side in sides)
            {
                Vector2Int inside = MiddleOfSide(rect, side);
                Vector2Int outside = inside + side;
                if (!structure.Contains(outside) || structure.Has(outside, RoomTileTags.Structure)) continue;
                door = new PartitionEdge(inside, outside, 0f, dividesZones: false);
                return true;
            }
            door = default;
            return false;
        }

        private static Vector2Int MiddleOfSide(RectInt rect, Vector2Int side)
        {
            int midX = rect.xMin + (rect.width - 1) / 2;
            int midY = rect.yMin + (rect.height - 1) / 2;
            if (side.x > 0) return new Vector2Int(rect.xMax - 1, midY);
            if (side.x < 0) return new Vector2Int(rect.xMin, midY);
            if (side.y > 0) return new Vector2Int(midX, rect.yMax - 1);
            return new Vector2Int(midX, rect.yMin);
        }
    }
}
