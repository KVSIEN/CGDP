using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Stubs of wall left standing where a building came down: short runs of inner wall with a
    // ragged top, standing alone in the room. They never cross a walkway or crowd a doorway, and
    // never divide the room into zones.
    [CreateAssetMenu(fileName = "WallRemnantsStructureRule", menuName = "CGD/Level/Structure/Wall Remnants")]
    public class WallRemnantsStructureRule : RoomStructureRule
    {
        // Floor area the counts are written for; bigger rooms keep more remnants.
        private const float ReferenceTiles = 64f;
        private const int Attempts = 12;

        [Tooltip("Stubs of wall per room, for a floor of 64 tiles")]
        [SerializeField] private IntRange _count = new(3, 6);
        [Tooltip("Tiles long")]
        [SerializeField] private IntRange _length = new(1, 3);
        [Tooltip("Metres tall, rolled again for every piece of wall so the top is ragged")]
        [SerializeField] private FloatRange _height = new(0.9f, 2.8f);

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            IReadOnlyList<Vector2Int> tiles = structure.Footprint.Tiles;
            float scale = Mathf.Clamp(tiles.Count / ReferenceTiles, 0.5f, 2f);
            int count = Mathf.Max(1, Mathf.RoundToInt(_count.Evaluate(random) * scale));

            for (int i = 0; i < count; i++) StandOneWall(structure, tiles, random);
        }

        private void StandOneWall(RoomStructure structure, IReadOnlyList<Vector2Int> tiles, RandomStream random)
        {
            for (int attempt = 0; attempt < Attempts; attempt++)
                if (TryStandWall(structure, random.Pick(tiles), random)) return;
        }

        // A run of wall starting between `start` and the tile above (or to the right), along the
        // other axis.
        private bool TryStandWall(RoomStructure structure, Vector2Int start, RandomStream random)
        {
            bool acrossY = random.Chance(0.5f);
            Vector2Int across = acrossY ? Vector2Int.up : Vector2Int.right;
            Vector2Int along  = acrossY ? Vector2Int.right : Vector2Int.up;

            var edges = new List<PartitionEdge>();
            int length = Mathf.Max(1, _length.Evaluate(random));
            for (int step = 0; step < length; step++)
            {
                Vector2Int a = start + along * step;
                Vector2Int b = a + across;
                if (!structure.IsOpen(a) || !structure.IsOpen(b) || structure.IsPartitioned(a, b) || structure.EdgeMustStayOpen(a, b)) break;
                edges.Add(new PartitionEdge(a, b, _height.Evaluate(random), dividesZones: false));
            }
            return edges.Count > 0 && structure.TryAddPartition(edges);
        }
    }
}
