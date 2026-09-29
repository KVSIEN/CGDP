using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A map node turned into a rectangle of floor tiles.
    public class LevelRoom
    {
        public LevelRoom(MapNode node, RectInt tiles)
        {
            Node  = node;
            Tiles = tiles;
        }

        public MapNode Node  { get; }
        public RectInt Tiles { get; }

        public Vector2 Center => Tiles.center;
    }
}
