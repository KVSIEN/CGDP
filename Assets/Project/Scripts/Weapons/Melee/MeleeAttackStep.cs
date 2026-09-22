using System;
using UnityEngine;
using CGD.Audio;
using CGD.Combat;

namespace CGD.Weapons
{
    [Serializable]
    public class MeleeAttackStep
    {
        [Header("Damage")]
        public float Damage = 20f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        [Tooltip("Multiplier applied when a Thrust or Sweep hit lands on a critical hitbox region (e.g. head). Slam attacks use area damage and skip region resolution.")]
        public float CriticalMultiplier = 1.5f;

        [Header("Timing")]
        [Tooltip("Delay before the hit window opens.")]
        public float WindupTime = 0.1f;
        [Tooltip("How long the hit window stays open. Detection runs every physics tick during this window.")]
        public float ActiveTime = 0.15f;
        [Tooltip("Delay after the hit window closes before another attack can start, unless a combo input was buffered.")]
        public float RecoveryTime = 0.25f;

        [Header("Hit Detection")]
        [Tooltip("Thrust: single SphereCast forward (stab). Sweep: fan of SphereCasts in a horizontal arc (slash). Slam: OverlapSphere at impact point (overhead smash).")]
        public MeleeHitShape HitShape = MeleeHitShape.Sweep;
        [Tooltip("How far forward the attack reaches (metres).")]
        public float Range = 1.8f;
        [Tooltip("SphereCast / overlap radius (metres). Wider = more forgiving.")]
        public float Radius = 0.25f;
        [Tooltip("Sweep only: total horizontal arc in degrees, symmetric about forward.")]
        [Range(10f, 180f)]
        public float SweepArcDeg = 90f;
        [Tooltip("Sweep only: number of SphereCasts spread across the arc. More = denser coverage, higher cost.")]
        [Range(2, 12)]
        public int SweepRays = 5;

        [Header("On Hit")]
        [Tooltip("Status effects each hit may apply.")]
        public StatusEffectApplication[] OnHitEffects;

        [Header("Audio")]
        public SoundBank SwingSound;
        public SoundBank HitSound;

        [Header("Debug")]
        public Color DebugColor = Color.white;
    }
}
