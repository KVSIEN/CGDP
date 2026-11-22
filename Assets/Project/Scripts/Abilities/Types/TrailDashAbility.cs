using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Tech's Thruster Dash: a burst straight ahead, the way the caster faces — so to dash away
    // it has to turn its back first (the cast time is that turn) — leaving a strip of burning
    // floor along the way. The patches are the shared DamageZone, so rivals treat them as a
    // hazard and route round them. Needs a caster that can dash on its own (enemies).
    [CreateAssetMenu(fileName = "TrailDashAbility", menuName = "CGD/Abilities/Trail Dash")]
    public class TrailDashAbility : Ability
    {
        [Header("Dash")]
        [Tooltip("Metres it covers, unless a wall or ledge stops it sooner")]
        [Min(0.5f)] public float Distance = 7f;
        [Tooltip("Seconds the dash takes")]
        [Min(0.05f)] public float Duration = 0.3f;

        [Header("Trail")]
        [Tooltip("Pooled prefab with a DamageZone (Prefabs/VFX/DamageZone)")]
        public GameObject ZonePrefab;
        [Tooltip("Metres between the patches it leaves; with a patch Radius above half of this they join up")]
        [Min(0.25f)] public float Spacing = 1.5f;
        [Tooltip("Each patch along the trail")]
        public ZoneSettings Zone = new() { Radius = 1.1f, Duration = 4f, Glow = ZoneGlow.Pulse };
        public LayerMask HitMask = ~0;
        [Tooltip("Applied on every tick to everyone hostile standing in the trail (Fire)")]
        public StatusEffectApplication[] OnHitEffects;

        public override bool CanExecute(AbilityContext ctx) =>
            ctx.Dasher != null && !ctx.Dasher.IsDashing && ZonePrefab != null && ZonePrefab.TryGetComponent<DamageZone>(out _);

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Dasher == null || ctx.CasterTransform == null) return;
            ctx.Dasher.Begin(ctx.CasterTransform.forward, Distance, Duration, Spacing, point => DropPatch(point, ctx));
        }

        private void DropPatch(Vector3 point, AbilityContext ctx)
        {
            GameObject spawned = PrefabPool.Spawn(ZonePrefab, TargetQuery.FloorBelow(point + Vector3.up) + Vector3.up * 0.05f, Quaternion.identity);
            if (spawned.TryGetComponent(out DamageZone zone))
                zone.Begin(Zone, Zone.Radius, ctx.Source, HitMask, OnHitEffects, ctx.Health);
        }
    }
}
