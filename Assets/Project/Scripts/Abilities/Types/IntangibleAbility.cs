using UnityEngine;

namespace CGD.Abilities
{
    // Wrong Echo: for a few seconds the caster is not quite there. Nothing hurts it and walls
    // are nothing to it — it walks straight at where it is going through whatever is in the way
    // — but it stays in plain view, and it can still attack and cast. Once the time is up it
    // finishes the walk out of any wall it is in before it becomes solid again. Needs a caster
    // that can turn intangible (enemies).
    [CreateAssetMenu(fileName = "IntangibleAbility", menuName = "CGD/Abilities/Intangible")]
    public class IntangibleAbility : Ability
    {
        [Tooltip("Game seconds it is unhittable and free of walls")]
        [Min(0.5f)] public float Duration = 5f;

        public override float EffectDuration => Duration;

        public override bool CanExecute(AbilityContext ctx) => ctx.Phasing != null && !ctx.Phasing.IsIntangible;

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Phasing != null) ctx.Phasing.Begin(Duration);
        }
    }
}
