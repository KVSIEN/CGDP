using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Columns on a regular grid, Spacing tiles apart both ways, set out for the room: of the
    // grid's possible offsets, the one that stands its outer rows and columns evenly between the
    // room's walls, then the one that loses the fewest columns to a walkway, a doorway's front or
    // something already built, so it reads as one ordered grid rather than scattered posts.
    [CreateAssetMenu(fileName = "PillarGridStructureRule", menuName = "CGD/Level/Structure/Pillar Grid")]
    public class GridPillarsStructureRule : RoomStructureRule
    {
        [SerializeField, Min(2)] private int _spacing = 4;
        [Tooltip("Tiles kept free between a pillar and the walls")]
        [SerializeField, Min(0)] private int _wallMargin = 1;
        [SerializeField, Min(0.1f)] private float _pillarWidth = 0.8f;
        [Tooltip("Optional — placed instead of a plain box: built 1 m wide and 1 m tall with its origin at the base, it is stretched to the pillar's width and height")]
        [SerializeField] private GameObject _pillarPrefab;

        [Header("Ruin")]
        [Tooltip("Chance a column of the grid is gone altogether")]
        [SerializeField, Range(0f, 1f)] private float _missingChance;
        [Tooltip("Chance a column that stands is broken off short")]
        [SerializeField, Range(0f, 1f)] private float _brokenChance;
        [Tooltip("Metres a broken column still stands; pulled into low (0.9–1.2) or high (2.2+) cover")]
        [SerializeField] private FloatRange _brokenHeight = new(0.7f, 2.4f);

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            Vector2Int phase = BestPhase(structure);
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (!OnGrid(structure, tile, phase)) continue;
                if (_missingChance > 0f && random.Chance(_missingChance)) continue;

                float height = _brokenChance > 0f && random.Chance(_brokenChance) ? CoverHeights.Readable(_brokenHeight.Evaluate(random)) : 0f;
                structure.TryAddPillar(tile + new Vector2(0.5f, 0.5f), _pillarWidth, _pillarPrefab, height: height);
            }
        }

        private bool OnGrid(RoomStructure structure, Vector2Int tile, Vector2Int phase) =>
            Mod(tile.x - phase.x, _spacing) == 0 && Mod(tile.y - phase.y, _spacing) == 0
            && structure.Footprint.IsInterior(tile, _wallMargin);

        // The offset whose grid is most nearly centred between the room's walls (a tile off either
        // way is centred: parity may allow no better), then loses the fewest columns, then has
        // the most. No random draw, so the room's other draws stay where they were.
        private Vector2Int BestPhase(RoomStructure structure)
        {
            RectInt bounds = structure.Footprint.Bounds;
            Vector2Int best = Vector2Int.zero;
            int bestScore = int.MaxValue;
            for (int x = 0; x < _spacing; x++)
                for (int y = 0; y < _spacing; y++)
                {
                    var phase = new Vector2Int(x, y);
                    Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
                    int count = 0, lost = 0;
                    foreach (Vector2Int tile in structure.Footprint.Tiles)
                    {
                        if (!OnGrid(structure, tile, phase)) continue;
                        count++;
                        if (!structure.IsOpen(tile)) lost++;
                        min = Vector2Int.Min(min, tile);
                        max = Vector2Int.Max(max, tile);
                    }
                    if (count == 0) continue;

                    int uneven = Mathf.Max(0, Mathf.Abs((min.x - bounds.xMin) - (bounds.xMax - 1 - max.x)) - 1)
                               + Mathf.Max(0, Mathf.Abs((min.y - bounds.yMin) - (bounds.yMax - 1 - max.y)) - 1);
                    int score = uneven * 10000 + lost * 100 - count;
                    if (score >= bestScore) continue;
                    bestScore = score;
                    best = phase;
                }
            return best;
        }

        private static int Mod(int value, int by) => (value % by + by) % by;
    }
}
