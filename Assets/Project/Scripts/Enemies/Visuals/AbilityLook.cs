using System;
using UnityEngine;
using CGD.Abilities;

namespace CGD.Enemies
{
    // How one ability shows on an enemy (EnemyAbilityVisuals): a flash when it goes off and,
    // for as long as its effect lasts, a pulsing shell with an optional ring at the feet and
    // a speed trail; for a cone ability (a flamethrower), streams sprayed across its cone.
    [Serializable]
    public struct AbilityLook
    {
        public Ability Ability;
        [Tooltip("Shell, ring, trail and flash colour; alpha sets how strong they are")]
        public Color Color;
        [Tooltip("Metres: the flash when it goes off. 0 = none")]
        [Min(0f)] public float CastFlash;
        [Tooltip("A glowing ring at its feet while the effect lasts (dug in)")]
        public bool GroundRing;
        [Tooltip("A trail behind it while the effect lasts (speed)")]
        public bool Trail;
        [Tooltip("Streams sprayed side by side across the cone each time it goes off, showing what it hits (a flamethrower). Only for a Targeted ability with Cone targeting. 0 = none")]
        [Min(0)] public int ConeStreams;
    }
}
