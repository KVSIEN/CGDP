using UnityEngine;

namespace CGD.Combat
{
    // A daze: the target is slowed hard the moment it is hit and recovers evenly, back to full
    // speed once the effect's Duration has passed (see Stunnable.SetFadingSlow). Potency deepens
    // the slow (2 = twice the speed lost). Hitting a dazed target again starts it over.
    [CreateAssetMenu(fileName = "DazeEffect", menuName = "CGD/Combat/Status Effects/Daze")]
    public class DazeEffect : StatusEffect
    {
        [Header("Daze")]
        [Tooltip("Movement speed right after the hit, as a share of normal (0.25 = a quarter speed)")]
        [Range(0f, 1f)] public float StartSpeed = 0.25f;

        public override void Apply(StatusEffectController target, in DamageInfo hit, StatusDose dose)
        {
            float startSpeed = 1f - (1f - StartSpeed) * dose.Potency;
            if (target.Stunnable != null) target.Stunnable.SetFadingSlow(this, startSpeed, dose.DurationOf(this));
            base.Apply(target, hit, dose);
        }

        // The slow eases itself out in Stunnable; the status only shows it on the HUD.
        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }
    }
}
