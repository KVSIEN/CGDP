using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Opens a lingering patch of harmful ground where the caster is aiming, using the shared
    // DamageZone prefab: Void's Rift (chills with Cryo and siphons shield back to the caster),
    // Bio's Barbed Snare (slows, bleeds, and squelches when someone steps in; the bleed leaves a
    // scent hunters can follow). Enemies only: it lands on the point they aim at.
    [CreateAssetMenu(fileName = "ZoneAbility", menuName = "CGD/Abilities/Zone")]
    public class ZoneAbility : Ability
    {
        private const float FallbackDistance = 6f;

        [Tooltip("Pooled prefab with a DamageZone (Prefabs/VFX/DamageZone)")]
        public GameObject ZonePrefab;
        [Tooltip("On: only casts with a point to aim at. Off: without one it lands a few metres ahead of the caster")]
        public bool NeedsAimPoint = true;

        public ZoneSettings Zone = new();
        public LayerMask HitMask = ~0;
        [Tooltip("Applied on every tick to everyone hostile inside (Rift's Cryo, Snare's slow and bleed)")]
        public StatusEffectApplication[] OnHitEffects;

        public override bool CanExecute(AbilityContext ctx) =>
            ZonePrefab != null && ZonePrefab.TryGetComponent<DamageZone>(out _) && (ctx.HasAimPoint || !NeedsAimPoint);

        public override void Execute(AbilityContext ctx)
        {
            if (!ctx.HasAimPoint && NeedsAimPoint) return;

            Vector3 aim = ctx.HasAimPoint
                ? ctx.AimPoint
                : ctx.CasterTransform.position + ctx.CasterTransform.forward * FallbackDistance;
            GameObject spawned = PrefabPool.Spawn(ZonePrefab, TargetQuery.FloorBelow(aim) + Vector3.up * 0.05f, Quaternion.identity);
            if (spawned.TryGetComponent(out DamageZone zone))
                zone.Begin(Zone, Zone.Radius, ctx.Source, HitMask, OnHitEffects, ctx.Health);
        }
    }
}
