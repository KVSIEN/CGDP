using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Abilities
{
    // Bursts a barbed pod where the caster is aiming: a patch of grasping growth that slows
    // everything hostile in it and makes it bleed, and squelches when one first steps in. A
    // bleeding target leaves a trail hunters with a sense of smell can follow (PerceptionProfile
    // scent). Enemies only: it needs the point they aim at.
    [CreateAssetMenu(fileName = "SnareAbility", menuName = "CGD/Abilities/Snare")]
    public class SnareAbility : Ability
    {
        [Tooltip("Pooled prefab with a SnareZone (Prefabs/VFX/SnareZone)")]
        public GameObject ZonePrefab;

        [Header("Zone")]
        [Min(0.5f)] public float Radius = 3f;
        [Tooltip("Game seconds the patch lasts")]
        [Min(0.5f)] public float Duration = 5f;
        [Tooltip("Seconds between ticks")]
        [Min(0.1f)] public float TickInterval = 0.5f;
        public Color Color = new(0.35f, 0.75f, 0.2f, 0.5f);
        public LayerMask HitMask = ~0;

        [Header("Each tick, on everyone hostile inside")]
        [Tooltip("A scratch from the barbs; its on-hit effects carry the slow and the bleed")]
        [Min(0f)] public float DamagePerTick = 1f;
        [Tooltip("The slow and the bleed (the bleed is what leaves a scent)")]
        public StatusEffectApplication[] OnHitEffects;

        [Header("Squelch")]
        [Tooltip("Metres the noise of a hostile stepping in carries (0 = silent)")]
        [Min(0f)] public float EntryNoiseRadius = 18f;

        public override bool CanExecute(AbilityContext ctx) =>
            ZonePrefab != null && ZonePrefab.TryGetComponent<SnareZone>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Vector3 aim = ctx.HasAimPoint ? ctx.AimPoint : ctx.CasterTransform.position + ctx.CasterTransform.forward * 6f;
            GameObject zone = PrefabPool.Spawn(ZonePrefab, GroundBelow(aim), Quaternion.identity);
            if (zone.TryGetComponent(out SnareZone snare)) snare.Begin(this, ctx.Source);
        }

        // The aim point is at chest height; the patch lies on the floor beneath it.
        private static Vector3 GroundBelow(Vector3 point) =>
            Physics.Raycast(point, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * 0.05f
                : point + Vector3.down * 1f;
    }
}
