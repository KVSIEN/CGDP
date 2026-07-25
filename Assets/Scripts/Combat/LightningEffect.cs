using System.Collections.Generic;
using UnityEngine;

// Lightning doesn't deal damage on a tick like the other status effects —
// applying it immediately chains through nearby enemies via Chain(), and its
// stacking only exists to make the target's NEXT chain reach further. Tick()
// is a no-op; duration/decay of the stack still comes from the base
// StatusEffect lifecycle. Set MaxStacks to 4 on the asset to match the given
// falloff table (100/80/64/51.2/40.96% = 1 initial hit + up to 4 chain hops).
[CreateAssetMenu(fileName = "LightningEffect", menuName = "CGD/Status Effects/Lightning")]
public class LightningEffect : StatusEffect
{
    [Header("Lightning")]
    [Tooltip("Radius (metres) to search for the next chain target")]
    public float ChainRadius = 8f;
    [Tooltip("Damage multiplier lost per chain hop (0.8 = 20% less each hop)")]
    public float ChainFalloff = 0.8f;
    [Tooltip("Layers to search for chain targets")]
    public LayerMask EnemyMask = ~0;

    public override void Tick(IDamageable target, int stacks, float magnitude) { }

    // Call when Lightning lands on `initialTarget`. Deals the hit, stacks the
    // status on the target, then chains to as many additional nearby enemies as
    // the target's resulting stack count allows (Chain Count = 1 + stacks),
    // each hit ChainFalloff weaker than the last, never striking the same
    // enemy twice in one cast.
    public void Chain(GameObject initialTarget, float magnitude)
    {
        if (initialTarget == null) return;

        var visited = new HashSet<GameObject> { initialTarget };
        Vector3 origin = initialTarget.transform.position;

        int   hops   = Strike(initialTarget, magnitude);
        float damage = magnitude;

        for (int i = 0; i < hops; i++)
        {
            damage *= ChainFalloff;
            GameObject next = FindNearestUnvisited(origin, visited);
            if (next == null) break;

            visited.Add(next);
            Strike(next, damage);
            origin = next.transform.position;
        }
    }

    // Deals damage and applies/refreshes the stacking status on `go`. Returns
    // the resulting stack count (0 if it has no StatusEffectController).
    private int Strike(GameObject go, float damage)
    {
        if (go.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(new DamageInfo(damage, type: DamageType.Lightning));

        if (go.TryGetComponent<StatusEffectController>(out var controller))
        {
            controller.ApplyEffect(this, damage);
            return controller.GetStacks(this);
        }
        return 0;
    }

    private GameObject FindNearestUnvisited(Vector3 origin, HashSet<GameObject> visited)
    {
        Collider[] hits = Physics.OverlapSphere(origin, ChainRadius, EnemyMask, QueryTriggerInteraction.Ignore);

        GameObject nearest = null;
        float nearestSqrDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            if (!col.TryGetComponent<EnemyHealth>(out var enemy)) continue;
            if (visited.Contains(enemy.gameObject)) continue;

            float sqrDist = (enemy.transform.position - origin).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = enemy.gameObject;
            }
        }

        return nearest;
    }
}
