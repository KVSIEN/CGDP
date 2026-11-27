using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Low cover in a V out in the room: two short 45° walls (DiagonalWall) meeting at a point, so
    // one piece covers two firing lanes and the lanes round it run on the diagonal. The point
    // faces the doorway a player arrives by, so whoever crouches in the V is covered from the way
    // in on both sides. Each stands on open floor with a tile clear all round it, off the routes.
    [CreateAssetMenu(fileName = "ChevronCoverStructureRule", menuName = "CGD/Level/Structure/Chevron Cover")]
    public class ChevronCoverStructureRule : RoomStructureRule
    {
        private const int Attempts = 24;

        [Tooltip("How many to put in the room")]
        [SerializeField] private IntRange _count = new(1, 2);
        [Tooltip("Tiles in each of the V's two arms")]
        [SerializeField] private IntRange _arm = new(2, 2);
        [Tooltip("Metres: low cover to crouch behind and shoot over (CoverHeights' low band)")]
        [SerializeField, Min(0.1f)] private float _height = 1.1f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            IReadOnlyList<Vector2Int> tiles = structure.Footprint.Tiles;
            int count = _count.Evaluate(random);
            for (int placed = 0, attempt = 0; placed < count && attempt < Attempts * count; attempt++)
            {
                Vector2Int at = tiles[random.Range(0, tiles.Count)];
                int arm = Mathf.Max(1, _arm.Evaluate(random));
                List<DiagonalWall> chevron = Chevron(arm, TurnsToward(structure, at), at, _height);
                if (StandsClear(structure, chevron) && structure.TryAddDiagonals(chevron)) placed++;
            }
        }

        // A V of two `arm`-tile runs whose point is at the top of the shape before it is turned
        // (`turns` quarter turns counter-clockwise), set so its first tile is at `at`.
        public static List<DiagonalWall> Chevron(int arm, int turns, Vector2Int at, float height)
        {
            var local = new List<DiagonalWall>(arm * 2);
            local.AddRange(DiagonalWall.Run(Vector2Int.zero, new Vector2Int(1, 1), arm, height));
            local.AddRange(DiagonalWall.Run(new Vector2Int(arm, arm - 1), new Vector2Int(1, -1), arm, height));

            var chevron = new List<DiagonalWall>(local.Count);
            foreach (DiagonalWall wall in local) chevron.Add(wall.Turned(turns).Moved(at));
            return chevron;
        }

        // The quarter turns that point the V (up before turning) at the arrival, or the room's middle.
        private static int TurnsToward(RoomStructure structure, Vector2Int at)
        {
            Vector2 target = structure.Arrival.HasValue ? structure.Arrival.Value : structure.Footprint.Anchor;
            Vector2 toward = target - at;
            // Up, then each quarter turn counter-clockwise: left, down, right.
            if (Mathf.Abs(toward.y) >= Mathf.Abs(toward.x)) return toward.y >= 0f ? 0 : 2;
            return toward.x < 0f ? 1 : 3;
        }

        // Open floor for every piece, and nothing built or put down within a tile of it.
        private static bool StandsClear(RoomStructure structure, List<DiagonalWall> chevron)
        {
            foreach (DiagonalWall wall in chevron)
            {
                if (!structure.IsOpen(wall.Tile)) return false;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int near = wall.Tile + new Vector2Int(dx, dy);
                        if (structure.Contains(near) && !structure.IsFree(near)) return false;
                    }
            }
            return true;
        }
    }
}
