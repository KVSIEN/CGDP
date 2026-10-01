using UnityEngine;

namespace CGD.Level
{
    // A prop to place: position in tile units on the floor, yaw in degrees.
    public readonly struct PlannedProp
    {
        public PlannedProp(GameObject prefab, Vector2 position, float yaw)
        {
            Prefab   = prefab;
            Position = position;
            Yaw      = yaw;
        }

        public GameObject Prefab   { get; }
        public Vector2    Position { get; }
        public float      Yaw      { get; }
    }
}
