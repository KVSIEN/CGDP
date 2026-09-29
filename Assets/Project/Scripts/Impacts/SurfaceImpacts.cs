using System;
using UnityEngine;

namespace CGD.Impacts
{
    // Impact effects for one surface, per kind of hit.
    [Serializable]
    public class SurfaceImpacts
    {
        [Tooltip("Physics material that identifies the surface (same ones the footstep SurfaceDatabase uses)")]
        public PhysicsMaterial Material;
        public ImpactEffect Bullet = new();
        public ImpactEffect Melee  = new();

        public ImpactEffect For(ImpactKind kind) => kind == ImpactKind.Melee ? Melee : Bullet;
    }
}
