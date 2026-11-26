using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // The hub room: a square court walled round the room's centre, with a gap in the middle of
    // each side, and a loop of open floor round it. Fights circle the court; the centrepiece
    // stands in it, seen only through the gaps. Walkways and doorways always get through, and
    // the walls go in through TryAddPartition, so the room stays one connected space.
    [CreateAssetMenu(fileName = "HubRingStructureRule", menuName = "CGD/Level/Structure/Hub Ring")]
    public class HubRingStructureRule : RoomStructureRule
    {
        private const int MinHalfSize = 2;

        [Tooltip("The court's width as a share of the room's shorter side")]
        [SerializeField] private FloatRange _courtShare = new(0.35f, 0.5f);
        [Tooltip("Floor kept round the court, in tiles: the loop. The court shrinks to keep it")]
        [SerializeField, Min(1)] private int _loopTiles = 2;
        [Tooltip("The gap in the middle of each side of the court, in tiles")]
        [SerializeField, Min(1)] private int _gapTiles = 2;
        [Tooltip("Metres; 0 = full wall height. Low (about 1.1) makes a rail you shoot over; high (2.2+) hides the court")]
        [SerializeField, Min(0f)] private float _height = 1.1f;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            int shortSide = Mathf.Min(bounds.width, bounds.height);
            var centre = new Vector2Int(Mathf.RoundToInt(structure.Footprint.Anchor.x), Mathf.RoundToInt(structure.Footprint.Anchor.y));

            for (int half = Mathf.RoundToInt(shortSide * _courtShare.Evaluate(random) * 0.5f); half >= MinHalfSize; half--)
            {
                var court = new RectInt(centre.x - half, centre.y - half, half * 2, half * 2);
                if (!HasLoop(structure, court)) continue;

                List<PartitionEdge> walls = CourtWalls(structure, court, CoverHeights.Readable(_height));
                if (walls.Count > 0 && structure.TryAddPartition(walls)) return;
            }
        }

        // Whether all the floor within the loop's reach of the court is the room's.
        private bool HasLoop(RoomStructure structure, RectInt court)
        {
            var reach = new RectInt(court.x - _loopTiles, court.y - _loopTiles, court.width + _loopTiles * 2, court.height + _loopTiles * 2);
            foreach (Vector2Int tile in reach.allPositionsWithin)
                if (!structure.Contains(tile)) return false;
            return true;
        }

        private List<PartitionEdge> CourtWalls(RoomStructure structure, RectInt court, float height)
        {
            var edges = new List<PartitionEdge>();
            foreach (Vector2Int tile in court.allPositionsWithin)
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int outside = tile + side;
                    if (court.Contains(outside) || IsGap(court, tile, side) || structure.EdgeMustStayOpen(tile, outside)) continue;
                    edges.Add(new PartitionEdge(tile, outside, height, dividesZones: false));
                }
            return edges;
        }

        // Whether the tile is in the middle stretch of the court's side it lies on.
        private bool IsGap(RectInt court, Vector2Int tile, Vector2Int side)
        {
            bool vertical = side.x != 0;
            int along = vertical ? tile.y - court.yMin : tile.x - court.xMin;
            int length = vertical ? court.height : court.width;
            int start = (length - _gapTiles) / 2;
            return along >= start && along < start + _gapTiles;
        }
    }
}
