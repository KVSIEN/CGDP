using UnityEngine;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "HealAbility", menuName = "CGD/Abilities/Heal")]
    public class HealAbility : Ability
    {
        public float HealAmount = 35f;

        public override bool Execute(AbilityContext ctx)
        {
            if (ctx.Health == null) return false;

            // Don't use the cooldown if already at full health
            if (ctx.Health.Health >= ctx.Health.MaxHealth) return false;

            ctx.Health.Heal(HealAmount);
            return true;
        }
    }
}
