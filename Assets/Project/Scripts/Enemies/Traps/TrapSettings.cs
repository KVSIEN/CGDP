using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // What a trap does once it is down: how soon it is live, what sets it off, how hard it bites
    // and how long it waits. Held by the ability that lays it (TrapAbility), so one Trap prefab
    // serves every mine and snare and each is tuned on its ability.
    [Serializable]
    public class TrapSettings
    {
        [Tooltip("Seconds after landing before it is live; until then nothing sets it off")]
        [Min(0f)] public float ArmDelay = 1f;
        [Tooltip("Metres: a hostile this close sets it off")]
        [Min(0.2f)] public float TriggerRadius = 1.2f;
        [Tooltip("Seconds between being set off and going off — a beep to dive away from. 0 = at once")]
        [Min(0f)] public float Fuse = 0f;
        [Tooltip("Game seconds it waits before it fizzles out")]
        [Min(1f)] public float Lifetime = 40f;

        [Header("Going off")]
        [Tooltip("Metres it reaches: every hostile this close is hit")]
        [Min(0.2f)] public float BlastRadius = 1.5f;
        [Min(0f)] public float Damage = 10f;
        public DamageType DamageType = DamageType.Physical;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        [Tooltip("Metres the noise of it going off carries, so the trapper and its kin come running")]
        [Min(0f)] public float NoiseRadius = 15f;
        [Tooltip("On: shooting it sets it off where it lies (a mine). Off: shooting it just breaks it (a snare)")]
        public bool GoesOffWhenShot;

        [Header("Look")]
        [Tooltip("Colour of its blinking light; it blinks fast once set off")]
        public Color Color = new(1f, 0.3f, 0.1f, 1f);
        [Tooltip("Optional: Prefabs/VFX/ExplosionFlash, shown when it goes off")]
        public GameObject Flash;
    }
}
