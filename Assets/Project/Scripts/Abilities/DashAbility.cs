using UnityEngine;
using CGD.Player;

namespace CGD.Abilities
{
    // Performs a dodge-style move (see DodgeDefinition) through the player's PlayerDodge, so
    // a dash ability steers, commits, grants i-frames and animates like a dodge. The
    // ability's own cooldown and cost apply instead of the dodge's.
    [CreateAssetMenu(fileName = "DashAbility", menuName = "CGD/Abilities/Dash")]
    public class DashAbility : Ability
    {
        [Tooltip("The move performed. Its stage cooldowns and cost are ignored")]
        public DodgeDefinition Move;

        public override bool CanExecute(AbilityContext ctx) =>
            ctx.PlayerTransform.TryGetComponent(out PlayerDodge dodge) && dodge.CanPerform(Move);

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.PlayerTransform.TryGetComponent(out PlayerDodge dodge))
                dodge.TryPerform(Move);
        }
    }
}
