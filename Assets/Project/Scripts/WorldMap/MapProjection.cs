using UnityEngine;

namespace CGD.WorldMap
{
    // Converts between world positions and map space. The map looks straight down:
    // map X is world X, map Y is world Z, and (0,0)–(1,1) covers the area's bounds.
    public readonly struct MapProjection
    {
        public MapProjection(Vector3 center, Vector2 size)
        {
            Size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
            Min  = new Vector2(center.x, center.z) - Size * 0.5f;
        }

        // World XZ of the map's bottom-left corner, and its extent in metres.
        public Vector2 Min  { get; }
        public Vector2 Size { get; }

        public Vector2 ToNormalized(Vector3 world) =>
            new((world.x - Min.x) / Size.x, (world.z - Min.y) / Size.y);

        public Vector3 ToWorld(Vector2 normalized, float height = 0f) =>
            new(Min.x + normalized.x * Size.x, height, Min.y + normalized.y * Size.y);

        public bool Contains(Vector3 world)
        {
            Vector2 n = ToNormalized(world);
            return n.x >= 0f && n.x <= 1f && n.y >= 0f && n.y <= 1f;
        }

        // Compass heading of a transform on the map, degrees clockwise from map-up (+Z).
        public static float Heading(Transform transform)
        {
            Vector3 forward = transform.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
    }
}
