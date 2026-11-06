using UnityEngine;
using CGD.Combat;

namespace CGD.Impacts
{
    // One hit landing on a surface.
    public readonly struct Impact
    {
        public Impact(Vector3 point, Vector3 normal, Collider collider, ImpactKind kind, DamageSource source = default)
        {
            Point    = point;
            Normal   = normal;
            Collider = collider;
            Kind     = kind;
            Source   = source;
        }

        public Vector3    Point    { get; }
        // Points out of the surface, back toward whoever fired.
        public Vector3    Normal   { get; }
        public Collider   Collider { get; }
        public ImpactKind Kind     { get; }
        // Who fired, when known — enemies feel near misses from hostile fire.
        public DamageSource Source { get; }
    }
}
