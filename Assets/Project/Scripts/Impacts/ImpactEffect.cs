using System;
using UnityEngine;
using CGD.Audio;
using CGD.Core;

namespace CGD.Impacts
{
    // What one kind of hit leaves on one kind of surface. Every part is optional.
    [Serializable]
    public class ImpactEffect
    {
        [Tooltip("Decal material (transparent, e.g. a bullet hole). Empty = no decal")]
        public Material Decal;
        [Tooltip("Decal width in metres, picked at random within the range")]
        public FloatRange DecalSize = new(0.08f, 0.14f);
        [Tooltip("Spawned at the hit facing out of the surface. Needs PooledLifetime to return itself")]
        public GameObject Vfx;
        public SoundBank Sound;
    }
}
