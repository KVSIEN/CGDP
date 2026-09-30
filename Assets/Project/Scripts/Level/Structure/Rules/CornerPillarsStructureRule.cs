using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // A column in every inside corner — where the arms of an L, T or cross meet — set into
    // the walls, so it frames the opening without taking floor. Both walls must run on for
    // at least two tiles, which skips the stair-steps of round rooms.
    [CreateAssetMenu(fileName = "CornerPillarsStructureRule", menuName = "CGD/Level/Structure/Corner Pillars")]
    public class CornerPillarsStructureRule : RoomStructureRule
    {
        private static readonly Vector2Int[] Diagonals = { new(1, 1), new(1, -1), new(-1, -1), new(-1, 1) };

        [SerializeField, Min(0.1f)] private float _pillarWidth = 1f;
        [SerializeField] private GameObject _pillarPrefab;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RoomFootprint footprint = structure.Footprint;
            foreach (Vector2Int tile in footprint.Tiles)
                foreach (Vector2Int d in Diagonals)
                {
                    var alongX = new Vector2Int(d.x, 0);
                    var alongY = new Vector2Int(0, d.y);
                    bool insideCorner = footprint.Contains(tile + alongX) && footprint.Contains(tile + alongY) && !footprint.Contains(tile + d);
                    bool longWalls = footprint.Contains(tile + alongX * 2) && !footprint.Contains(tile + d + alongX)
                                  && footprint.Contains(tile + alongY * 2) && !footprint.Contains(tile + d + alongY);
                    if (!insideCorner || !longWalls) continue;

                    Vector2 corner = tile + new Vector2(0.5f, 0.5f) + (Vector2)d * 0.5f;
                    structure.TryAddPillar(corner, _pillarWidth, _pillarPrefab, freeStanding: false);
                }
        }
    }
}
