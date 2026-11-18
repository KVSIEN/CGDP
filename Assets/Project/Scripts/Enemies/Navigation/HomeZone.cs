using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The ground an enemy calls home: one or more floor-plan rectangles (a room's bounds, or
    // the bounds of every room of a faction's territory), kept in the level's own space so a
    // moved or turned level carries them along. Pure geometry — what the enemy does about
    // leaving it is the faction's TerritoryDoctrine and EnemyRouting.
    public sealed class HomeZone
    {
        private readonly Rect[]    _areas;
        private readonly Transform _space;

        public HomeZone(IReadOnlyList<Rect> areasInSpace, Transform space)
        {
            _areas = new Rect[areasInSpace.Count];
            for (int i = 0; i < _areas.Length; i++) _areas[i] = areasInSpace[i];
            _space = space;
        }

        // Metres from the nearest area to the point on the floor plan (0 while inside one).
        public float DistanceOutside(Vector3 point) => DistanceOutside(_space.InverseTransformPoint(point), out _);

        // The point itself while it is within `margin` of the zone, otherwise the nearest spot
        // that is.
        public Vector3 Clamp(Vector3 point, float margin)
        {
            Vector3 local = _space.InverseTransformPoint(point);
            if (DistanceOutside(local, out int nearest) <= margin) return point;

            Rect area = _areas[nearest];
            local.x = Mathf.Clamp(local.x, area.xMin - margin, area.xMax + margin);
            local.z = Mathf.Clamp(local.z, area.yMin - margin, area.yMax + margin);
            return _space.TransformPoint(local);
        }

        private float DistanceOutside(Vector3 local, out int nearest)
        {
            float best = float.MaxValue;
            nearest = 0;
            for (int i = 0; i < _areas.Length; i++)
            {
                Rect area = _areas[i];
                float dx = Mathf.Max(area.xMin - local.x, 0f, local.x - area.xMax);
                float dz = Mathf.Max(area.yMin - local.z, 0f, local.z - area.yMax);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance >= best) continue;
                best    = distance;
                nearest = i;
            }
            return best;
        }
    }
}
