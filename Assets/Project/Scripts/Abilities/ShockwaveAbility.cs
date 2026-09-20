using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    // Damages hostile characters around the player and launches nearby rigidbodies away.
    [CreateAssetMenu(fileName = "ShockwaveAbility", menuName = "CGD/Abilities/Shockwave")]
    public class ShockwaveAbility : Ability
    {
        public float Radius = 6f;
        public float Force  = 18f;
        [Tooltip("Layers the shockwave affects")]
        public LayerMask HitMask = ~0;

        [Header("Damage")]
        public float Damage = 20f;
        [Range(0f, 1f)] public float ArmorPenetration = 0f;
        public DamageType DamageType = DamageType.Physical;
        public StatusEffectApplication[] OnHitEffects;

        private static readonly Collider[] _hitBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> _damaged = new();

        public override void Execute(AbilityContext ctx)
        {
            Vector3 origin = ctx.PlayerTransform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, Radius, _hitBuffer, HitMask, QueryTriggerInteraction.Ignore);

            var hit = new DamageInfo(Damage, ArmorPenetration, DamageType, source: ctx.Source, onHitEffects: OnHitEffects);
            _damaged.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];

                // Team filtering in HealthManager keeps the caster and allies unharmed.
                IDamageable target = Hitbox.FindDamageable(col);
                if (Damage > 0f && target != null && _damaged.Add(target))
                    target.TakeDamage(hit);

                var rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb == ctx.PlayerRigidbody) continue;

                Vector3 dir = (col.transform.position - origin).normalized;
                rb.AddForce(dir * Force, ForceMode.VelocityChange);
            }
        }
    }
}
