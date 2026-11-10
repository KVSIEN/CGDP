using UnityEngine;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "HealAbility", menuName = "CGD/Abilities/Heal")]
    public class HealAbility : Ability
    {
        public float HealAmount = 35f;

        // Not usable at full health, so the charge isn't wasted.
        public override bool CanExecute(AbilityContext ctx) =>
            ctx.Health != null && ctx.Health.Health < ctx.Health.MaxHealth;

        public override void Execute(AbilityContext ctx) => ctx.Health.Heal(HealAmount);
    }
}
