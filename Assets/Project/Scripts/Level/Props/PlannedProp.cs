using UnityEngine;

namespace CGD.Level
{
    // A prop to place: position in tile units on the floor, yaw in degrees, and the floor
    // tiles it was given (the members of a pile or a group share one). A decoration takes no
    // floor. A stretched prop is scaled across and front to back to the footprint it got.
    public readonly struct PlannedProp
    {
        private readonly Vector2 _stretch;

        public PlannedProp(GameObject prefab, Vector2 position, float yaw, RectInt tiles = default, bool decor = false,
                           Vector2 stretch = default)
        {
            Prefab   = prefab;
            Position = position;
            Yaw      = yaw;
            Tiles    = tiles;
            Decor    = decor;
            _stretch = stretch;
        }

        public GameObject Prefab   { get; }
        public Vector2    Position { get; }
        public float      Yaw      { get; }
        public RectInt    Tiles    { get; }
        public bool       Decor    { get; }
        // Scale across (x) and front to back (y) on top of the prefab's own; (1, 1) unstretched.
        public Vector2    Stretch  => _stretch == Vector2.zero ? Vector2.one : _stretch;
    }
}
