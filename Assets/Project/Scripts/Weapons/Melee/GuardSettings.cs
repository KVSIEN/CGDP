using System;
using UnityEngine;

namespace CGD.Weapons
{
    // How a melee weapon's guard (the alternate action) holds up, and how its bash parries.
    // Guarding slows the player to ADS walk speed, since it shares the aim input.
    [Serializable]
    public struct GuardSettings
    {
        [Tooltip("Share of a blocked hit's damage that still gets through")]
        [Range(0f, 1f)] public float BlockDamageMultiplier;
        [Tooltip("Stamina paid per point of damage the block stops")]
        [Min(0f)] public float StaminaPerDamage;
        [Tooltip("Total angle in front of the player the guard covers")]
        [Range(10f, 360f)] public float ArcDeg;
        [Tooltip("Seconds from the start of a bash in which a hit from in front is parried")]
        [Min(0f)] public float ParryWindow;
        [Tooltip("Seconds a parried attacker is stunned")]
        [Min(0f)] public float ParryStun;
        [Tooltip("Seconds after a bash's parry window closes without a parry in which the player can't guard or bash again")]
        [Min(0f)] public float WhiffExposure;
        [Tooltip("Damage taken while exposed after a missed parry, × the hit")]
        [Min(1f)] public float ExposedDamageMultiplier;

        public GuardSettings(float blockDamage, float staminaPerDamage, float arcDeg, float parryWindow, float parryStun,
                             float whiffExposure = 0.5f, float exposedDamageMultiplier = 1.5f)
        {
            BlockDamageMultiplier = blockDamage;
            StaminaPerDamage      = staminaPerDamage;
            ArcDeg                = arcDeg;
            ParryWindow           = parryWindow;
            ParryStun             = parryStun;
            WhiffExposure           = whiffExposure;
            ExposedDamageMultiplier = exposedDamageMultiplier;
        }

        public static GuardSettings Default => new(0.3f, 0.5f, 120f, 0.2f, 1.2f);
    }
}
