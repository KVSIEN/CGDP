using UnityEngine;

namespace CGD.Combat
{
    // Holds the target in place: a jaw trap closing, a tether, a web. The player is restrained —
    // can't move or dodge until they mash Interact to break free, or the effect's Duration (or
    // the source's own) runs out. Characters that can't be restrained, such as enemies, are
    // stunned for the Duration instead.
    [CreateAssetMenu(fileName = "RestrainEffect", menuName = "CGD/Combat/Status Effects/Restrain")]
    public class RestrainEffect : StatusEffect
    {
        [Header("Restrain")]
        [Tooltip("What the break-free prompt says")]
        public string Label = "Break free";
        [Tooltip("Taps of Interact it takes to break free. 0 = the player's default")]
        [Min(0)] public int TapsRequired = 6;

        public override void Apply(StatusEffectController target, in DamageInfo hit, StatusDose dose)
        {
            float seconds = dose.DurationOf(this);
            // The break-free prompt already shows it, and it can end early; no status tile for it.
            if (target.TryGetComponent(out IRestrainable restrainable))
            {
                restrainable.Restrain(Label, TapsRequired, seconds);
                return;
            }

            if (target.Stunnable != null) target.Stunnable.ApplyStun(seconds);
            base.Apply(target, hit, dose);
        }

        // The hold is kept by the restrained character (or Stunnable); the status only shows it on the HUD.
        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }
    }
}
