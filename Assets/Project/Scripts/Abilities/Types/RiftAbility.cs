using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Opens a rift on the ground where the caster is aiming: a zone that wears everything hostile
    // inside it down tick by tick (and slows it), while the caster takes a share of the damage back
    // as shield. Enemies only: it needs the point they aim at.
    [CreateAssetMenu(fileName = "RiftAbility", menuName = "CGD/Abilities/Rift")]
    public class RiftAbility : Ability
    {
        [Tooltip("Pooled prefab with a RiftZone (Prefabs/VFX/RiftZone)")]
        public GameObject ZonePrefab;

        [Header("Zone")]
        [Min(0.5f)] public float Radius = 4f;
        [Tooltip("Game seconds the rift stays open")]
        [Min(0.5f)] public float Duration = 6f;
        [Tooltip("Seconds between ticks")]
        [Min(0.1f)] public float TickInterval = 0.5f;
        public Color Color = new(0.55f, 0.25f, 1f, 0.5f);
        public LayerMask HitMask = ~0;

        [Header("Each tick, on everyone hostile inside")]
        [Min(0f)] public float DamagePerTick = 4f;
        public DamageType DamageType = DamageType.True;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        [Tooltip("Applied to everyone hit (a slow)")]
        public StatusEffectApplication[] OnHitEffects;
        [Tooltip("Share of the damage actually dealt that comes back to the caster as shield")]
        [Range(0f, 2f)] public float ShieldSiphon = 0.75f;

        public override bool CanExecute(AbilityContext ctx) =>
            ZonePrefab != null && ZonePrefab.TryGetComponent<RiftZone>(out _) && ctx.HasAimPoint;

        public override void Execute(AbilityContext ctx)
        {
            if (!ctx.HasAimPoint) return;

            GameObject zone = PrefabPool.Spawn(ZonePrefab, TargetQuery.FloorBelow(ctx.AimPoint) + Vector3.up * 0.05f, Quaternion.identity);
            if (zone.TryGetComponent(out RiftZone rift)) rift.Begin(this, ctx.Source, ctx.Health);
        }
    }
}
