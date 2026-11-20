using UnityEngine;

namespace CGD.Combat
{
    // One damageable a ShapeHits query touched: the collider it was touched through, where, and
    // the surface normal there (zero when the contact has no meaningful one).
    public readonly struct ShapeHit
    {
        public readonly Collider    Collider;
        public readonly IDamageable Target;
        public readonly Vector3     Point;
        public readonly Vector3     Normal;

        public ShapeHit(Collider collider, IDamageable target, Vector3 point, Vector3 normal)
        {
            Collider = collider;
            Target   = target;
            Point    = point;
            Normal   = normal;
        }
    }
}
