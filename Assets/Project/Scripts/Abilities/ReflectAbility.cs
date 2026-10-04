using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    // Puts a ReflectProfile up on the player's Reflector: vengeance, deflect, absorb… The
    // profile decides what is caught and what the caught damage becomes.
    [CreateAssetMenu(fileName = "ReflectAbility", menuName = "CGD/Abilities/Reflect")]
    public class ReflectAbility : Ability
    {
        public ReflectProfile Profile;

        public override bool CanExecute(AbilityContext ctx) =>
            Profile != null && ctx.PlayerTransform.TryGetComponent(out Reflector _);

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.PlayerTransform.TryGetComponent(out Reflector reflector))
                reflector.Open(Profile);
        }
    }
}
