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
            MapNodeType.Resupply => new Color(0.35f, 0.7f, 0.6f),
            MapNodeType.Breach   => new Color(0.6f, 0.25f, 0.75f),
            MapNodeType.Lockdown => new Color(0.7f, 0.35f, 0.15f),
            MapNodeType.Holdout  => new Color(0.85f, 0.45f, 0.2f),
            MapNodeType.Ambush   => new Color(0.65f, 0.55f, 0.25f),
            MapNodeType.Stealth  => new Color(0.25f, 0.3f, 0.45f),
            MapNodeType.Rift     => new Color(0.45f, 0.15f, 0.6f),
            MapNodeType.Gamble   => new Color(0.9f, 0.4f, 0.65f),
            MapNodeType.EmergencyExit => new Color(0.3f, 0.75f, 0.85f),
            MapNodeType.Hazard   => new Color(0.6f, 0.75f, 0.2f),
            MapNodeType.Quiet    => new Color(0.55f, 0.75f, 0.7f),
            MapNodeType.Junction => new Color(0.6f, 0.6f, 0.65f),
            _                    => Color.gray
        };
    }
}
