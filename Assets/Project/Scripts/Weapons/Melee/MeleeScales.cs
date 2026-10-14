using System;
using UnityEngine;

namespace CGD.Weapons
{
    // Multipliers a melee model applies to its type's rolled ranges, the melee counterpart of
    // StyleScales: quality still decides where a stat lands inside the scaled range, and the
    // model's stats lean toward its side of each (see StyleLeans).
    [Serializable]
    public struct MeleeScales
    {
        [Min(0.01f)] public float Damage;
        [Tooltip("Above 1 = faster wind-up, strike and recovery")]
        [Min(0.01f)] public float AttackSpeed;
        [Min(0.01f)] public float Reach;
        [Min(0.01f)] public float ArmorPenetration;
        [Tooltip("Per swing")]
        [Min(0.01f)] public float StaminaCost;
        [Tooltip("Share of a blocked hit that still gets through: below 1 = a sturdier guard")]
        [Min(0.01f)] public float BlockDamage;
        [Min(0.01f)] public float ParryWindow;

        public static MeleeScales Identity => new()
        {
            Damage = 1f, AttackSpeed = 1f, Reach = 1f, ArmorPenetration = 1f,
            StaminaCost = 1f, BlockDamage = 1f, ParryWindow = 1f,
        };
    }
}
