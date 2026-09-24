using UnityEngine;

namespace CGD.Map
{
    // One colour per node type, shared by the Map Graph editor and the in-game map so a
    // Shop looks the same in both.
    public static class MapNodeColors
    {
        public static Color Of(MapNodeType type) => type switch
        {
            MapNodeType.Start    => new Color(0.25f, 0.65f, 0.3f),
            MapNodeType.Combat   => new Color(0.55f, 0.3f, 0.3f),
            MapNodeType.Elite    => new Color(0.75f, 0.2f, 0.45f),
            MapNodeType.Puzzle   => new Color(0.3f, 0.45f, 0.75f),
            MapNodeType.Shop     => new Color(0.8f, 0.65f, 0.2f),
            MapNodeType.Event    => new Color(0.45f, 0.35f, 0.7f),
            MapNodeType.Treasure => new Color(0.85f, 0.75f, 0.35f),
            MapNodeType.Boss     => new Color(0.6f, 0.1f, 0.1f),
            MapNodeType.Exit     => new Color(0.2f, 0.5f, 0.55f),
            _                    => Color.gray
        };
    }
}
