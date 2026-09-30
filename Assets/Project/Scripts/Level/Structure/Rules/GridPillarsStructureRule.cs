using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Columns on a grid shared by the whole ship: a pillar wherever both tile coordinates are
    // multiples of Spacing, so the columns of neighbouring halls line up.
    [CreateAssetMenu(fileName = "PillarGridStructureRule", menuName = "CGD/Level/Structure/Pillar Grid")]
    public class GridPillarsStructureRule : RoomStructureRule
    {
        [SerializeField, Min(2)] private int _spacing = 4;
        [Tooltip("Tiles kept free between a pillar and the walls")]
        [SerializeField, Min(0)] private int _wallMargin = 1;
        [SerializeField, Min(0.1f)] private float _pillarWidth = 0.8f;
        [Tooltip("Optional — placed instead of a plain box (origin at the base)")]
        [SerializeField] private GameObject _pillarPrefab;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (Mod(tile.x, _spacing) != 0 || Mod(tile.y, _spacing) != 0) continue;
                if (!structure.Footprint.IsInterior(tile, _wallMargin)) continue;
                structure.TryAddPillar(tile + new Vector2(0.5f, 0.5f), _pillarWidth, _pillarPrefab);
            }
        }

        private static int Mod(int value, int by) => (value % by + by) % by;
    }
}
