using UnityEngine;
using CGD.Enemies;

namespace CGD.Level
{
    // A prop to place: position in tile units on the floor, yaw in degrees, and the floor
    // tiles it was given (the members of a pile or a group share one). A decoration takes no
    // floor. A stretched prop is scaled across and front to back to the footprint it got. An
    // activity is something idle enemies come and do at it (RoomPropPlacement.Activity).
    public readonly struct PlannedProp
    {
        private readonly Vector2 _stretch;

        public PlannedProp(GameObject prefab, Vector2 position, float yaw, RectInt tiles = default, bool decor = false,
                           Vector2 stretch = default, InterestKind activity = null)
        {
            Prefab   = prefab;
            Position = position;
            Yaw      = yaw;
            Tiles    = tiles;
            Decor    = decor;
            Activity = activity;
            _stretch = stretch;
        }

        // The same prop standing `offset` tiles away, on the same tiles.
        public PlannedProp MovedBy(Vector2 offset) =>
            new(Prefab, Position + offset, Yaw, Tiles, Decor, _stretch, Activity);

        public GameObject Prefab   { get; }
        public Vector2    Position { get; }
        public float      Yaw      { get; }
        public RectInt    Tiles    { get; }
        public bool       Decor    { get; }
        public InterestKind Activity { get; }
        // Scale across (x) and front to back (y) on top of the prefab's own; (1, 1) unstretched.
        public Vector2    Stretch  => _stretch == Vector2.zero ? Vector2.one : _stretch;
    }
}
