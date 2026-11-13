using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Stats;
using CGD.Targeting;

namespace CGD.Abilities
{
    // A rallying cry: every ally within reach (and the caster) gets a buff for a while — a pack
    // leader's roar, a commander's order, a hive's pheromone. Allies without CharacterStats are
    // unaffected.
    [CreateAssetMenu(fileName = "RallyAbility", menuName = "CGD/Abilities/Rally")]
    public class RallyAbility : Ability
    {
        public StatModifierPreset Buff;
        [Tooltip("Game seconds the buff lasts")]
        [Min(0.1f)] public float Duration = 6f;
        [Tooltip("Metres: allies this close are rallied")]
        [Min(0.5f)] public float Radius = 15f;

        private static readonly List<HealthManager> Allies = new();

        public override float EffectDuration => Duration;

        public override bool CanExecute(AbilityContext ctx) => Buff != null && ctx.Health != null;

        public override void Execute(AbilityContext ctx)
        {
            if (Buff == null || ctx.Health == null) return;

            var filter = new TargetFilter(TargetAffiliation.Friendly, ctx.Health.Team, ctx.Health, ~0);
            TargetQuery.Sphere(ctx.CasterTransform.position, Radius, filter, Allies);
            foreach (HealthManager ally in Allies)
                if (ally != null && !ally.IsDead && ally.TryGetComponent(out CharacterStats stats))
                    stats.AddTimed(Buff, Duration);
        }
    }
}
