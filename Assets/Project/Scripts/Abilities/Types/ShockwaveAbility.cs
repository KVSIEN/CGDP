using UnityEngine;
using CGD.Combat;
using CGD.Targeting;

namespace CGD.Abilities
{
    // Damages hostile characters around the player and launches nearby rigidbodies away.
    // Walls stop it (CoverMask): what is wholly behind one is spared, what still shows round
    // its edge or over its top is hit.
    [CreateAssetMenu(fileName = "ShockwaveAbility", menuName = "CGD/Abilities/Shockwave")]
    public class ShockwaveAbility : Ability
    {
        public float Radius = 6f;
        public float Force  = 18f;
        [Tooltip("Layers the shockwave affects")]
        public LayerMask HitMask = ~0;
        [Tooltip("Scenery that blocks it: a target wholly behind a wall is spared, one partly round it is still hit. Nothing = it reaches through walls")]
        public LayerMask CoverMask = 1;

        [Header("Damage")]
        public float Damage = 20f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        public StatusEffectApplication[] OnHitEffects;

        // The burst goes out from the caster's chest, so the floor never hides anyone from it.
        private const float BurstHeight = 1f;

        private static readonly AreaHits _area = new(32);

        public override TargetArea Area => TargetArea.AroundCaster(Radius);

        public override void Execute(AbilityContext ctx)
        {
            Vector3 origin = ctx.CasterTransform.position;
            _area.Gather(origin + Vector3.up * BurstHeight, Radius, HitMask, CoverMask);

            // Team filtering in HealthManager keeps the caster and allies unharmed.
            if (Damage > 0f)
            {
                var hit = new DamageInfo(Scaled(Damage, ctx), ArmorPenetration, DamageType, source: ctx.Source,
                                         onHitEffects: WithBonusEffects(OnHitEffects, ctx));
                foreach (AreaHit target in _area.Hits) target.Target.TakeDamage(hit);
            }

            for (int i = 0; i < _area.ColliderCount; i++)
            {
                Collider col = _area.ColliderAt(i);
                var rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb == ctx.CasterRigidbody || !_area.Reaches(col)) continue;

                Vector3 dir = (col.transform.position - origin).normalized;
                rb.AddForce(dir * Force, ForceMode.VelocityChange);
            }
        }
    }
}
