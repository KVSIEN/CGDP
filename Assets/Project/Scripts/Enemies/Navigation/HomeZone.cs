using UnityEngine;

namespace CGD.Enemies
{
    // The ground an enemy calls home: a room's bounds, kept in the level's own space so a
    // moved or turned level carries it along. Pure geometry — what the enemy does about
    // leaving it is the faction's TerritoryDoctrine and EnemyRouting.
    public sealed class HomeZone
    {
        private readonly Rect      _area;
        private readonly Transform _space;

        public HomeZone(Rect areaInSpace, Transform space)
        {
            _area  = areaInSpace;
            _space = space;
        }

        // Metres from the zone's edge to the point on the floor plan (0 while inside it).
        public float DistanceOutside(Vector3 point)
        {
            Vector3 local = _space.InverseTransformPoint(point);
            float dx = Mathf.Max(_area.xMin - local.x, 0f, local.x - _area.xMax);
            float dz = Mathf.Max(_area.yMin - local.z, 0f, local.z - _area.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // The point itself while it is within `margin` of the zone, otherwise the nearest spot
        // that is.
        public Vector3 Clamp(Vector3 point, float margin)
        {
            if (DistanceOutside(point) <= margin) return point;

            Vector3 local = _space.InverseTransformPoint(point);
            local.x = Mathf.Clamp(local.x, _area.xMin - margin, _area.xMax + margin);
            local.z = Mathf.Clamp(local.z, _area.yMin - margin, _area.yMax + margin);
            return _space.TransformPoint(local);
        }
    }
}
