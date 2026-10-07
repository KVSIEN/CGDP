using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Weapons
{
    // A shield type (buckler, riot shield, tower shield…), rolled into individual shields like
    // a weapon category. Held in the offhand: the aim input raises it (a gun can't aim while
    // a shield is held), unless it blocks passively, and the Melee key bashes with it — the
    // start of the bash parries. Shields are the blockers: they let little through, cost
    // little stamina per hit and cover a wide arc; weapon guards are for parrying.
    [CreateAssetMenu(fileName = "ShieldDefinition", menuName = "CGD/Weapons/Shield")]
    public class ShieldDefinition : GearDefinition
    {
        [Header("Identity")]
        public string[] Names = { "Shield" };

        [Header("Block")]
        [Tooltip("Blocks hits from in front by itself (a tower shield): no need to raise it, so the aim input stays free. Blocked damage still costs stamina")]
        public bool BlocksPassively;
        [Tooltip("Share of a blocked hit that still gets through. Min = worst roll, Max = best roll")]
        public FloatRange BlockDamageMultiplier = new(0.2f, 0.05f);
        [Tooltip("Stamina per point of damage blocked")]
        public FloatRange StaminaPerDamage = new(0.3f, 0.4f);
        [Tooltip("Arc, parry window, parry stun and exposure after a missed parry (Block Damage and Stamina are rolled above)")]
        public GuardSettings Guard = new(0.1f, 0.35f, 160f, 0.15f, 1.6f);

        [Header("Bash")]
        [Tooltip("The shield bash (Melee key); its start parries. Damage is scaled by the rolled Bash Damage")]
        public MeleeAttackStep Bash = new() { Damage = 1f, WindupTime = 0.05f, ActiveTime = 0.12f, RecoveryTime = 0.4f, CancelFrom = 1f,
                                              HitShape = MeleeHitShape.Sweep, SweepArcDeg = 70f, Range = 1.6f, Radius = 0.5f };
        public FloatRange BashDamage = new(12f, 20f);
        [Tooltip("Meter the bash and blocking cost. Characters without it bash and block for free")]
        public MeterDefinition StaminaMeter;
        [Tooltip("Per bash")]
        public float BashStaminaCost = 10f;
        [Tooltip("Optional: what a parry does with the parried hit (see ReflectProfile)")]
        public ReflectProfile ParryReflect;

        [Header("Offhand")]
        [Tooltip("What holding it costs the main-hand gun")]
        public StatModifier[] MainHandPenalty =
        {
            new(ItemStat.Spread, StatModifierOp.Multiplicative, 0.2f),
            new(ItemStat.Recoil, StatModifierOp.Multiplicative, 0.2f),
        };

        public LayerMask HitMask = ~0;
        [Tooltip("How far away enemies hear a bash (0 = silent)")]
        public float NoiseRadius = 8f;

        public override ItemInstance CreateInstance(ItemRoll roll) => ShieldGenerator.Generate(this, roll);
    }
}
