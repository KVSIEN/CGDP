using UnityEngine;

namespace CGD.Level
{
    // One kit piece to place, in the level's local space.
    public readonly struct WallKitPlacement
    {
        public WallKitPlacement(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Prefab   = prefab;
            Position = position;
            Rotation = rotation;
            Scale    = scale;
        }

        public GameObject Prefab   { get; }
        public Vector3    Position { get; }
        public Quaternion Rotation { get; }
        public Vector3    Scale    { get; }
    }
}
