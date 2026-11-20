using UnityEngine;

namespace CGD.Combat
{
    // One damageable caught by an AreaHits query: through the collider of it nearest the centre.
    public readonly struct AreaHit
    {
        public readonly IDamageable Target;
        public readonly Collider    Collider;
        // The collider's surface point nearest the centre, and how far that is.
        public readonly Vector3     Point;
        public readonly float       Distance;

        public AreaHit(IDamageable target, Collider collider, Vector3 point, float distance)
        {
            Target   = target;
            Collider = collider;
            Point    = point;
            Distance = distance;
        }
    }
}
