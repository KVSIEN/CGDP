using UnityEngine;
using CGD.Stats;

namespace CGD.Combat
{
    // What a reflect catches and what the caught damage becomes. Every output is optional and
    // they combine freely: vengeance (take the hit, return most of it), deflect (negate it,
    // fire it where you aim), absorb (negate it, heal from it), a parry that answers with a
    // slash, and so on. Shares are of the caught damage, i.e. the hit after armour and guard.
    [CreateAssetMenu(fileName = "ReflectProfile", menuName = "CGD/Combat/Reflect Profile")]
    public class ReflectProfile : ScriptableObject
    {
        [Header("Catch")]
        [Tooltip("Seconds the reflect stays up when an ability opens it (a parry only reflects the parried hit)")]
        [Min(0f)] public float Duration = 1f;
        [Tooltip("Hits caught before it closes (0 = every hit until Duration ends)")]
        [Min(0)] public int MaxCatches = 1;
        [Tooltip("Total angle in front of the character it catches hits from (360 = all around)")]
        [Range(10f, 360f)] public float ArcDeg = 360f;
        [Tooltip("Share of a caught hit that is stopped (1 = negated, 0 = taken in full like vengeance)")]
        [Range(0f, 1f)] public float Negate = 1f;

        [Header("Return to sender")]
        [Tooltip("Share dealt straight back to the attacker, wherever it is (0 = off)")]
        [Min(0f)] public float ReturnShare;

        [Header("Projectile")]
        [Tooltip("Prefab with a Projectile component (empty = off)")]
        public GameObject  ProjectilePrefab;
        [Min(0f)] public float ProjectileShare = 1f;
        public ReflectAim  ProjectileAim   = ReflectAim.Aim;
        [Min(1f)] public float ProjectileSpeed = 40f;

        [Header("Beam / slash")]
        [Tooltip("Timeline played from the character (empty = off). Each damage event's Damage is a share of the reflected damage (1 = all of it); spawned zones keep their own damage")]
        public ActionTimeline Timeline;
        [Min(0f)] public float TimelineShare = 1f;
        public ReflectAim     TimelineAim   = ReflectAim.Aim;

        [Header("Absorb")]
        [Tooltip("Share healed (0 = off)")]
        [Min(0f)] public float HealShare;
        [Tooltip("Buff gained on each catch (empty = off); needs CharacterStats on the character")]
        public StatModifierPreset SelfBuff;
        [Min(0f)] public float    SelfBuffDuration = 5f;

        [Header("On the attacker")]
        [Tooltip("Status effects the attacker may receive, as if hit by the caught damage")]
        public StatusEffectApplication[] AttackerEffects;
        [Tooltip("Seconds the attacker is stunned (0 = off)")]
        [Min(0f)] public float AttackerStun;

        [Header("Feedback")]
        [Tooltip("Notification on each catch (empty = none)")]
        public string CatchMessage = "Reflected!";
    }
}
