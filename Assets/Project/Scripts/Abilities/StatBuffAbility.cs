using UnityEngine;
using CGD.Stats;

namespace CGD.Abilities
{
    // Applies a StatModifierPreset to the player for a while (e.g. +30% damage for 8 s).
    // The player needs a CharacterStats component for it to do anything.
    [CreateAssetMenu(fileName = "StatBuffAbility", menuName = "CGD/Abilities/Stat Buff")]
    public class StatBuffAbility : Ability
    {
        public StatModifierPreset Buff;
        [Tooltip("Game seconds; pauses and slows with game time")]
        [Min(0.1f)] public float Duration = 8f;

        public override bool CanExecute(AbilityContext ctx) =>
            Buff != null && ctx.PlayerTransform.TryGetComponent(out CharacterStats _);

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.PlayerTransform.TryGetComponent(out CharacterStats stats))
                stats.AddTimed(Buff, Duration);
        }
    }
}
