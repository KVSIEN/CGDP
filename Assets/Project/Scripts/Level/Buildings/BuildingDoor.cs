using UnityEngine;

namespace CGD.Level
{
    // A doorway through the wall between two neighbouring tiles.
    public readonly struct BuildingDoor
    {
        public BuildingDoor(Vector2Int a, Vector2Int b)
        {
            A = a;
            B = b;
        }

        public Vector2Int A { get; }
        public Vector2Int B { get; }
    }
}
