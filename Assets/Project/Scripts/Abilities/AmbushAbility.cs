using UnityEngine;
using CGD.Stats;

namespace CGD.Abilities
{
    // Slips out of sight, heals faster while hidden, and makes the next attack — which also
    // ends the cloak — hit for extra damage. Enemies only (it needs an ambush to arm).
    [CreateAssetMenu(fileName = "AmbushAbility", menuName = "CGD/Abilities/Ambush")]
    public class AmbushAbility : Ability
    {
        [Tooltip("Game seconds the cloak, the buff and the armed strike last")]
        [Min(0.1f)] public float Duration = 5f;
        [Tooltip("Applied while hidden (e.g. extra health regen)")]
        public StatModifierPreset Buff;
        [Tooltip("The first attack out of hiding deals this many times its damage")]
        [Min(1f)] public float DamageMultiplier = 2.5f;

        public override float EffectDuration => Duration;

        public override bool CanExecute(AbilityContext ctx) =>
            ctx.Stealth != null && ctx.Ambush != null && !ctx.Stealth.IsCloaked;

        public override void Execute(AbilityContext ctx)
        {
            ctx.Stealth.Cloak(Duration, breaksOnAttack: true);
            if (Buff != null && ctx.Stats != null) ctx.Stats.AddTimed(Buff, Duration);
            ctx.Ambush.Arm(DamageMultiplier, Duration);
        }
    }
}
