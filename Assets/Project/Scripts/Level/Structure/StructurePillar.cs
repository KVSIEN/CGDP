using UnityEngine;

namespace CGD.Level
{
    // A column, floor to top unless it is broken. Position is in tile units (tile corners are whole numbers).
    public readonly struct StructurePillar
    {
        public StructurePillar(Vector2 position, float width, GameObject prefab, float height = 0f)
        {
            Position = position;
            Width    = width;
            Prefab   = prefab;
            Height   = height;
        }

        public Vector2    Position { get; }
        public float      Width    { get; }    // metres
        public float      Height   { get; }    // metres; 0 = the room's full wall height
        public GameObject Prefab   { get; }    // optional; replaces the plain box
    }
}
