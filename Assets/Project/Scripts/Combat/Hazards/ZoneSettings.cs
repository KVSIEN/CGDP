using System;
using UnityEngine;

namespace CGD.Combat
{
    // What a DamageZone does while it lasts: how big it is, how long it stays, how hard and how
    // often it hits, how it looks, and the optional extras a zone can have. Whatever spawns the
    // zone (a grenade, a zone ability, a timeline event) holds one of these, so every lingering
    // patch of harmful ground shares the one prefab and is tuned on its owner. The status effects
    // it applies and the layers it hits come from the owner too, so a grenade's zone carries the same
    // element as its blast.
    [Serializable]
    public class ZoneSettings
    {
        [Tooltip("Metres. A grenade treats 0 as its explosion radius")]
        [Min(0f)] public float Radius = 3f;
        [Tooltip("Game seconds it lasts")]
        [Min(0.5f)] public float Duration = 6f;
        [Tooltip("Seconds between hits on everything hostile standing in it")]
        [Min(0.1f)] public float TickInterval = 0.5f;

        [Header("Each tick, on everyone hostile inside")]
        [Min(0f)] public float DamagePerTick = 5f;
        public DamageType DamageType = DamageType.Physical;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;

        [Header("Look")]
        [Tooltip("Tint of the ground; alpha sets how see-through it is")]
        public Color Color = new(1f, 0.4f, 0.1f, 0.5f);
        public ZoneGlow Glow = ZoneGlow.Steady;

        [Header("Extras")]
        [Tooltip("Metres the noise of a hostile first stepping in carries, so the owner's kin hear the trap spring (0 = silent)")]
        [Min(0f)] public float EntryNoiseRadius = 0f;
        [Tooltip("Share of the damage actually dealt that comes back to the caster as shield (0 = none)")]
        [Range(0f, 2f)] public float ShieldSiphon = 0f;

        public float DamagePerSecond => DamagePerTick / TickInterval;
    }
}
