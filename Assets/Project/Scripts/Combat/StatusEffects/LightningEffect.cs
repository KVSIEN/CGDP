using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // Lightning doesn't deal damage on a tick like the other status effects —
    // landing it adds a stack to the target and immediately chains to nearby
    // characters on the target's team; its stacking only exists to make the NEXT
    // chain reach further. Tick() is a no-op; duration/decay of the stack still comes
    // from the base StatusEffect lifecycle. Set MaxStacks to 4 on the asset to match
    // the given falloff table (100/80/64/51.2/40.96% = 1 initial hit + up to 4 chain hops).
    // Attacks that apply it should use DamageType.Lightning to get the bonus against shields.
    [CreateAssetMenu(fileName = "LightningEffect", menuName = "CGD/Combat/Status Effects/Lightning")]
    public class LightningEffect : StatusEffect
    {
        [Header("Lightning")]
        [Tooltip("Radius (metres) to search for the next chain target")]
        public float ChainRadius = 8f;
        [Tooltip("Damage multiplier lost per chain hop (0.8 = 20% less each hop)")]
        public float ChainFalloff = 0.8f;
        [Tooltip("Layers to search for chain targets")]
        public LayerMask EnemyMask = ~0;

        private static readonly AreaHits _nearby = new(32);
        private readonly HashSet<HealthManager> _visited = new();

        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }

        // The hit that applied Lightning already damaged the target. Chain count equals
        // the target's resulting stack count; each hop deals ChainFalloff less than the
        // previous and never strikes the same character twice in one cast. Potency scales the
        // chain's damage.
        public override void Apply(StatusEffectController target, in DamageInfo hit, StatusDose dose)
        {
            target.AddStack(this, hit.RawDamage, default, dose);
            int hops = target.GetStacks(this);

            _visited.Clear();
            if (target.TryGetComponent(out HealthManager first)) _visited.Add(first);

            Vector3 origin = target.transform.position;
            float   damage = hit.RawDamage * dose.Potency;

            for (int i = 0; i < hops; i++)
            {
                damage *= ChainFalloff;
                HealthManager next = FindNearestUnvisited(origin, target.Team);
                if (next == null) break;

                _visited.Add(next);
                Strike(next, damage, hit.Source, dose);
                origin = next.transform.position;
            }
        }

        private void Strike(HealthManager target, float damage, DamageSource source, StatusDose dose)
        {
            target.TakeDamage(new DamageInfo(damage, type: DamageType.Lightning, source: source));

            if (target.TryGetComponent(out StatusEffectController controller))
                controller.AddStack(this, damage, default, dose);
        }

        private HealthManager FindNearestUnvisited(Vector3 origin, Team team)
        {
            _nearby.Gather(origin, ChainRadius, EnemyMask);

            HealthManager nearest = null;
            float nearestSqrDist = float.MaxValue;

            foreach (AreaHit hit in _nearby.Hits)
            {
                if (hit.Target is not HealthManager candidate) continue;
                if (candidate.Team != team || candidate.IsDead || _visited.Contains(candidate)) continue;

                float sqrDist = (candidate.transform.position - origin).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }
}
