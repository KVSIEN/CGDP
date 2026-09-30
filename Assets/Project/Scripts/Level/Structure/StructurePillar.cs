using UnityEngine;

namespace CGD.Level
{
    // A floor-to-top column. Position is in tile units (tile corners are whole numbers).
    public readonly struct StructurePillar
    {
        public StructurePillar(Vector2 position, float width, GameObject prefab)
        {
            Position = position;
            Width    = width;
            Prefab   = prefab;
        }

        public Vector2    Position { get; }
        public float      Width    { get; }    // metres
        public GameObject Prefab   { get; }    // optional; replaces the plain box
    }
}
