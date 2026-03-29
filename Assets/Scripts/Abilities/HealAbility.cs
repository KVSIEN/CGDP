using UnityEngine;

[CreateAssetMenu(fileName = "HealAbility", menuName = "CGD/Abilities/Heal")]
public class HealAbility : Ability
{
    public float HealAmount = 35f;

    public override bool Execute(AbilityContext ctx)
    {
        if (ctx.Stats == null) return false;

        // Don't use the cooldown if already at full health
        if (ctx.Stats.Health >= ctx.Stats.MaxHealth) return false;

        ctx.Stats.Heal(HealAmount);
        return true;
    }
}
