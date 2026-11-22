using System;
using UnityEngine;

namespace CGD.Abilities
{
    // How an ability shows when an enemy uses it (EnemyAbilityVisuals), until real VFX exist:
    // a flash as it goes off, its hitbox drawn as it hits, and for as long as its effect lasts
    // a pulsing shell with an optional ring at the feet and a speed trail; and how a rigless
    // enemy holds its arm while casting (Pose). Left at its default (clear colour), the ability
    // shows nothing but the arm.
    [Serializable]
    public struct AbilityLook
    {
        [Tooltip("Colour of every part; alpha sets how strong they are (keep it low for big areas)")]
        public Color Color;
        [Tooltip("Metres: the flash at its chest when it goes off. 0 = none")]
        [Min(0f)] public float CastFlash;
        [Tooltip("Draws what it hits as it goes off: streams across a cone in front of it, or a burst filling the sphere round it. Only for abilities with a fixed area (Targeted with Cone or caster Area targeting, Shockwave, Cloud Burst, Rally)")]
        public bool ShowArea;
        [Tooltip("A glowing ring at its feet while the effect lasts (dug in)")]
        public bool GroundRing;
        [Tooltip("A trail behind it while the effect lasts (speed)")]
        public bool Trail;
        [Tooltip("How a rigless enemy holds its arm while casting: Thrust (out in front, a physical move) or Wand (raised upright, a spell)")]
        public CastPose Pose;

        public bool IsSet => Color.a > 0f;
    }
}
