using UnityEngine;

namespace CGD.Level
{
    // An inner wall on the edge between Tile and Tile + Side. Side is always up or right,
    // so each edge has exactly one representation. A doorway through a wall is one too: its
    // Height is how tall the opening is.
    public readonly struct PartitionEdge
    {
        public PartitionEdge(Vector2Int a, Vector2Int b, float height, bool dividesZones = true)
        {
            bool aFirst = a.x < b.x || a.y < b.y;
            Tile         = aFirst ? a : b;
            Side         = aFirst ? b - a : a - b;
            Height       = height;
            DividesZones = dividesZones;
        }

        public Vector2Int Tile   { get; }
        public Vector2Int Side   { get; }
        public Vector2Int Other  => Tile + Side;
        public float      Height { get; }    // metres; 0 = the room's full wall height
        // Whether the wall's line across the room cuts it into zones (a kitchen wall does; a
        // crumbled wall stub or an office partition inside a maze doesn't).
        public bool       DividesZones { get; }
    }
}
