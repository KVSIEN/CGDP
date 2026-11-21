using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // A grenade that goes off twice: after its first blast it stays where it lies, fuse burning
    // again, and then goes off a second time in the same radius with its own damage and on-hit
    // effects (Void's Frost Charge: a chill, then a harder freeze). Off by default.
    [Serializable]
    public class GrenadeSecondBlast
    {
        [Tooltip("Seconds after the first blast that it goes off again where it lies (0 = it only goes off once)")]
        [Min(0f)] public float Delay = 0f;
        [Tooltip("Damage of the second blast at its centre (the same edge falloff as the first)")]
        [Min(0f)] public float Damage = 0f;
        [Tooltip("Status effects the second blast applies, in place of the first blast's")]
        public StatusEffectApplication[] OnHitEffects;

        public bool Enabled => Delay > 0f;
    }
}
