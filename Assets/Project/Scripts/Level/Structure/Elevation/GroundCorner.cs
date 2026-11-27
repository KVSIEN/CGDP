using UnityEngine;

namespace CGD.Level
{
    // A tile of ground standing between two levels at a corner, solid (RoomTileTags.Structure),
    // climbing one level toward Up (a diagonal step such as (1, 1)) from the tile's own level:
    //   - the corner of a mound or crater, where the slopes on two sides meet: ground falling away
    //     diagonally, too steep across to walk;
    //   - Sheer: a platform's corner cut at 45° (a stage's front corners, an island's four): the
    //     half toward Up stands at Up's level with a sheer face along the tile's diagonal, carrying
    //     the ledges' guard across it; the other half is the floor round it.
    public readonly struct GroundCorner
    {
        public GroundCorner(Vector2Int tile, Vector2Int up, bool sheer = false)
        {
            Tile  = tile;
            Up    = up;
            Sheer = sheer;
        }

        public Vector2Int Tile  { get; }
        public Vector2Int Up    { get; }
        public bool       Sheer { get; }

        // The half of the tile toward Up, in tile units (tile corners are whole numbers): the
        // corner it rises toward, then the two ends of the diagonal across the tile.
        public (Vector2 inner, Vector2 a, Vector2 b) RaisedHalf
        {
            get
            {
                Vector2 middle = (Vector2)Tile + Vector2.one * 0.5f, up = Up;
                return (middle + up * 0.5f, middle + new Vector2(up.x, -up.y) * 0.5f, middle + new Vector2(-up.x, up.y) * 0.5f);
            }
        }
    }
}
