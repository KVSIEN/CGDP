using UnityEngine;

namespace CGD.Level
{
    // An inner wall on the edge between Tile and Tile + Side. Side is always up or right,
    // so each edge has exactly one representation.
    public readonly struct PartitionEdge
    {
        public PartitionEdge(Vector2Int a, Vector2Int b, float height)
        {
            bool aFirst = a.x < b.x || a.y < b.y;
            Tile   = aFirst ? a : b;
            Side   = aFirst ? b - a : a - b;
            Height = height;
        }

        public Vector2Int Tile   { get; }
        public Vector2Int Side   { get; }
        public Vector2Int Other  => Tile + Side;
        public float      Height { get; }    // metres; 0 = the room's full wall height
    }
}
