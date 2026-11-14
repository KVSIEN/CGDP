using UnityEngine;

namespace CGD.Level
{
    // A prop to place: position in tile units on the floor, yaw in degrees, and the floor
    // tiles it was given (the members of a pile share one). A decoration takes no floor.
    public readonly struct PlannedProp
    {
        public PlannedProp(GameObject prefab, Vector2 position, float yaw, RectInt tiles = default, bool decor = false)
        {
            Prefab   = prefab;
            Position = position;
            Yaw      = yaw;
            Tiles    = tiles;
            Decor    = decor;
        }

        public GameObject Prefab   { get; }
        public Vector2    Position { get; }
        public float      Yaw      { get; }
        public RectInt    Tiles    { get; }
        public bool       Decor    { get; }
    }
}
