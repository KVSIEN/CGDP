using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Freezes, scorches or fouls the ground in a cone in front of the caster: a row of the shared
    // DamageZone patches, each wider than the last, that linger and apply their effects (a slow)
    // to everyone hostile standing on them. The Void tank's Frost Ground. Like Zone, it only
    // reaches enemies of the caster (the caster's own team is never hurt).
    [CreateAssetMenu(fileName = "ConeZoneAbility", menuName = "CGD/Abilities/Cone Zone")]
    public class ConeZoneAbility : Ability
    {
        [Tooltip("Pooled prefab with a DamageZone (Prefabs/VFX/DamageZone)")]
        public GameObject ZonePrefab;
        [Tooltip("Metres the cone reaches in front of the caster")]
        [Min(1f)] public float Length = 9f;
        [Tooltip("Metres across the patch nearest the caster")]
        [Min(0.5f)] public float NearRadius = 1.5f;
        [Tooltip("Metres across the patch farthest from the caster")]
        [Min(0.5f)] public float FarRadius = 3.5f;
        [Tooltip("How many patches make up the cone (more = a smoother cone)")]
        [Range(2, 8)] public int Patches = 4;

        [Tooltip("What every patch does while it lasts (its Radius is ignored: the cone sets it)")]
        public ZoneSettings Zone = new();
        public LayerMask HitMask = ~0;
        [Tooltip("Applied on every tick to everyone hostile on the ground (the slow)")]
        public StatusEffectApplication[] OnHitEffects;

        public override bool CanExecute(AbilityContext ctx) =>
            ZonePrefab != null && ZonePrefab.TryGetComponent<DamageZone>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Vector3 origin  = ctx.CasterTransform.position;
            Vector3 forward = ctx.CasterTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return;
            forward.Normalize();

            for (int i = 0; i < Patches; i++)
            {
                float   along  = (i + 0.5f) / Patches;
                float   radius = Mathf.Lerp(NearRadius, FarRadius, along);
                Vector3 centre = TargetQuery.FloorBelow(origin + forward * (Length * along)) + Vector3.up * 0.05f;

                GameObject spawned = PrefabPool.Spawn(ZonePrefab, centre, Quaternion.identity);
                if (spawned.TryGetComponent(out DamageZone zone))
                    zone.Begin(Zone, radius, ctx.Source, HitMask, OnHitEffects, ctx.Health);
            }
        }
    }
}
