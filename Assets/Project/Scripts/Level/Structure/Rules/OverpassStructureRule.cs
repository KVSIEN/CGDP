using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Lays footbridges across a room's lower floor (Overpass): a deck straight across a lane at
    // least two levels below it, from floor a level below the deck on one side to the same on the
    // other, with a flight up onto each end. It finds them on the levels an earlier rule made, so
    // it runs after Terraces: across the middle lane of raised walks (1-0-1, a deck at 2) or of a
    // sunken court (0 -1 0, a deck at 1), 1.8 m clear underneath either way. Decks nearest the
    // middle of the room come first, a few tiles apart along the lane. A room with no such lane is
    // left as it is.
    //
    // Does nothing, and draws no random number, in a room with no lane to cross.
    [CreateAssetMenu(fileName = "OverpassStructureRule", menuName = "CGD/Level/Structure/Overpass")]
    public class OverpassStructureRule : RoomStructureRule
    {
        // Decks keep at least this many tiles apart along the lane they cross.
        private const int Apart = 3;
        // Picked at random among this many of the spans nearest the middle.
        private const int Choices = 3;

        [SerializeField, Range(1, 3)] private int _count = 1;
        [Tooltip("Deck tiles across the lane, fewest and most: a lane narrower or wider than this isn't crossed")]
        [SerializeField] private IntRange _span = new(2, 8);

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            List<(Vector2Int start, Vector2Int end, int level)> spans = Spans(structure);
            if (spans.Count == 0) return;

            Vector2 middle = structure.Footprint.Bounds.center;
            spans.Sort((a, b) => DistanceTo(a, middle).CompareTo(DistanceTo(b, middle)));

            int laid = 0;
            while (laid < _count && spans.Count > 0)
            {
                int pick = random.Range(0, Mathf.Min(Choices, spans.Count));
                (Vector2Int start, Vector2Int end, int level) = spans[pick];
                spans.RemoveAt(pick);
                if (NearAnother(structure, start, end)) continue;
                if (structure.TryAddOverpass(new Overpass(level, start, end))) laid++;
            }
        }

        // Every straight run across lower floor: from a tile a level below the deck, over tiles at
        // least OverpassClearance below it, to another tile a level below the deck. Each once
        // (eastward or northward), for every deck level the room allows, highest first.
        private List<(Vector2Int, Vector2Int, int)> Spans(RoomStructure structure)
        {
            var spans = new List<(Vector2Int, Vector2Int, int)>();
            int lowestDeck = structure.LowestLevel + RoomElevation.OverpassClearance;
            for (int level = structure.HighestLevel; level >= lowestDeck; level--)
                foreach (Vector2Int start in structure.Footprint.Tiles)
                {
                    if (structure.LevelOf(start) != level - 1) continue;
                    foreach (Vector2Int along in new[] { Vector2Int.right, Vector2Int.up })
                        if (TryCross(structure, start, along, level, out Vector2Int end)) spans.Add((start, end, level));
                }
            return spans;
        }

        private bool TryCross(RoomStructure structure, Vector2Int start, Vector2Int along, int level, out Vector2Int end)
        {
            end = start;
            int span = 0;
            for (Vector2Int tile = start + along; structure.Contains(tile); tile += along)
            {
                int below = structure.LevelOf(tile);
                if (below == level - 1)
                {
                    end = tile;
                    return span >= Mathf.Max(1, _span.Min) && span <= _span.Max;
                }
                if (level - below < RoomElevation.OverpassClearance || ++span > _span.Max) return false;
            }
            return false;
        }

        // Too close beside a deck already laid, along the lane both cross.
        private static bool NearAnother(RoomStructure structure, Vector2Int start, Vector2Int end)
        {
            foreach (Overpass other in structure.Overpasses)
            {
                Vector2Int across = new(Mathf.Abs(other.Along.y), Mathf.Abs(other.Along.x));
                Vector2Int apart = start - other.Stairs[0].Tile;
                if (Mathf.Abs(apart.x * across.x + apart.y * across.y) < Apart) return true;
            }
            return false;
        }

        private static float DistanceTo((Vector2Int start, Vector2Int end, int level) span, Vector2 point) =>
            Vector2.Distance(((Vector2)span.start + span.end) * 0.5f + Vector2.one * 0.5f, point);
    }
}
