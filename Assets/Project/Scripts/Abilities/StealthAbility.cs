using UnityEngine;
using CGD.Stealth;

namespace CGD.Abilities
{
    // Cloaks the player for a while: hidden as if in cover, anywhere. Needs a Stealthable
    // on the player.
    [CreateAssetMenu(fileName = "StealthAbility", menuName = "CGD/Abilities/Stealth")]
    public class StealthAbility : Ability
    {
        [Min(0.1f)] public float Duration = 6f;
        [Tooltip("Attacking or making noise ends the cloak early")]
        public bool BreaksOnAttack = true;

        public override bool CanExecute(AbilityContext ctx) =>
            ctx.PlayerTransform.TryGetComponent(out Stealthable stealth) && !stealth.IsCloaked;

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.PlayerTransform.TryGetComponent(out Stealthable stealth))
                stealth.Cloak(Duration, BreaksOnAttack);
        }
    }
}
