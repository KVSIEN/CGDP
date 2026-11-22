using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // Lays a trap toward its aim point (at most Max Deploy Distance away): a proximity mine, a
    // jaw snare. What the trap does is tuned here (Settings); the Trap prefab is shared. A caster
    // keeps at most Max Active traps down — laying one more takes up its oldest. Enemies only:
    // a Lay Trap node picks where it goes.
    [CreateAssetMenu(fileName = "TrapAbility", menuName = "CGD/Abilities/Trap")]
    public class TrapAbility : Ability
    {
        [Tooltip("Pooled prefab with a Trap and a DeployableHealth (Prefabs/Weapons/Trap)")]
        public GameObject TrapPrefab;
        [Tooltip("Metres from the caster it lands, toward its aim point, at most")]
        [Min(1f)] public float MaxDeployDistance = 10f;
        [Tooltip("Traps of this caster down at once; laying another takes up the oldest")]
        [Min(1)] public int MaxActive = 3;

        public TrapSettings Settings = new();
        public LayerMask HitMask = ~0;
        [Tooltip("Applied to everyone hostile it catches (a Restrain to hold them, a Bleed)")]
        public StatusEffectApplication[] OnHitEffects;

        public override bool CanExecute(AbilityContext ctx) =>
            TrapPrefab != null && TrapPrefab.TryGetComponent<Trap>(out _);

        public override void Execute(AbilityContext ctx)
        {
            GameObject owner = ctx.Source.Owner;
            while (Trap.CountOwnedBy(owner) >= MaxActive) Trap.RemoveOldestOf(owner);

            GameObject spawned = PrefabPool.Spawn(TrapPrefab, DeploySpot.Toward(ctx, MaxDeployDistance), Quaternion.identity);
            if (spawned.TryGetComponent(out Trap trap)) trap.Deploy(Settings, ctx.Source, HitMask, OnHitEffects);
        }
    }
}
