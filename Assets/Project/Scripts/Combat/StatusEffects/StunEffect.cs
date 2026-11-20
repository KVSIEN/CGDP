using UnityEngine;

namespace CGD.Combat
{
    // Stops the target for the effect's Duration (or the source's own): enemies freeze in place and the player cannot
    // move. Applying again while stunned never shortens the stun (see Stunnable). Does nothing on
    // targets that cannot be stunned, such as the firing range's dummies.
    [CreateAssetMenu(fileName = "StunEffect", menuName = "CGD/Combat/Status Effects/Stun")]
    public class StunEffect : StatusEffect
    {
        public override void Apply(StatusEffectController target, in DamageInfo hit, StatusDose dose)
        {
            if (target.Stunnable != null) target.Stunnable.ApplyStun(dose.DurationOf(this));
            base.Apply(target, hit, dose);
        }

        // The stun itself is held by Stunnable; the status only shows it on the HUD.
        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }
    }
}
