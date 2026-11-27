using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Cuts a corner of the room off with a straight 45° wall (DiagonalWall) from one of its walls
    // to the other, left open in a gap or two: a nook behind an angled counter, a corner office,
    // a booth. The wall starts and ends on the room's walls, so it reads as built in, and the
    // corner behind it is reached through its gaps. A corner with a doorway in it, or a run that
    // would cross a route, is passed over (a route tile on the run becomes one of its gaps).
    // Draws nothing more once it has a corner, and nothing at all in a room with none that fits.
    [CreateAssetMenu(fileName = "AngledCornerStructureRule", menuName = "CGD/Level/Structure/Angled Corner")]
    public class AngledCornerStructureRule : RoomStructureRule
    {
        private static readonly Vector2Int[] Inward = { new(1, 1), new(-1, 1), new(1, -1), new(-1, -1) };

        [Tooltip("How many corners to cut")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Tiles along each wall the cut reaches; tried from the largest drawn down to 3")]
        [SerializeField] private IntRange _size = new(4, 5);
        [Tooltip("Openings left in the wall; a route crossing it makes one of them")]
        [SerializeField] private IntRange _gaps = new(1, 1);
        [Tooltip("Metres; 0 = the room's full wall height. Low (about 1 m) makes an angled counter to shoot over")]
        [SerializeField, Min(0f)] private float _height;

        private const int SmallestSize = 3;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            int count = _count.Evaluate(random);
            var corners = new List<Vector2Int>(Inward);
            random.Shuffle(corners);
            int cut = 0;
            foreach (Vector2Int inward in corners)
            {
                if (cut >= count) break;
                int gaps = Mathf.Max(1, _gaps.Evaluate(random));
                for (int size = Mathf.Max(SmallestSize, _size.Evaluate(random)); size >= SmallestSize; size--)
                    if (TryCut(structure, inward, size, gaps))
                    {
                        cut++;
                        break;
                    }
            }
        }

        // The wall across the corner the `inward` diagonal points away from, `size` tiles along each wall.
        private bool TryCut(RoomStructure structure, Vector2Int inward, int size, int gaps)
        {
            RectInt bounds = structure.Footprint.Bounds;
            var corner = new Vector2Int(inward.x > 0 ? bounds.xMin : bounds.xMax - 1, inward.y > 0 ? bounds.yMin : bounds.yMax - 1);

            // The whole corner, wall line included, is floor with no doorway in it.
            for (int i = 0; i < size; i++)
                for (int j = 0; i + j < size; j++)
                {
                    Vector2Int tile = corner + new Vector2Int(i * inward.x, j * inward.y);
                    if (!structure.Contains(tile) || structure.Has(tile, RoomTileTags.NearDoor)) return false;
                }

            // From the tile against one wall to the tile against the other, a diagonal step at a time.
            Vector2Int start = corner + new Vector2Int(0, (size - 1) * inward.y);
            List<DiagonalWall> run = DiagonalWall.Run(start, new Vector2Int(inward.x, -inward.y), size, _height);

            var kept = new List<DiagonalWall>(run.Count);
            int openings = 0;
            foreach (DiagonalWall wall in run)
            {
                if (structure.IsOpen(wall.Tile)) kept.Add(wall);
                else openings++;
            }
            // Gaps in the middle first, so the ends stay against the walls.
            for (int k = 0; openings < gaps && kept.Count > 2; k++)
            {
                kept.RemoveAt(kept.Count / 2);
                openings++;
            }
            if (openings == 0 || openings > gaps + 1 || kept.Count < 2) return false;
            return structure.TryAddDiagonals(kept);
        }
    }
}
