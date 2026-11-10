using UnityEngine;
using CGD.Stats;

namespace CGD.Abilities
{
    // Vanishes and reappears a short way off behind full cover, out of sight of every hostile
    // around, then gets a buff for a while (Void: its shield recharges much faster). Minions
    // appear beside it where it reappears and go after its hostiles, guarding it while it
    // recovers. Needs a caster that can relocate unseen (enemies) and, for the buff, a CharacterStats.
    [CreateAssetMenu(fileName = "PhaseShiftAbility", menuName = "CGD/Abilities/Phase Shift")]
    public class PhaseShiftAbility : Ability
    {
        [Tooltip("Metres: the furthest it can reappear from where it vanished")]
        [Min(1f)] public float MaxDistance = 15f;
        [Tooltip("Applied once it has reappeared. Empty = no buff")]
        public StatModifierPreset Buff;
        [Tooltip("Game seconds the buff lasts")]
        [Min(0.1f)] public float Duration = 6f;

        [Header("Minions")]
        [Tooltip("Enemy prefab called up where it reappears. Empty = none")]
        public GameObject MinionPrefab;
        [Min(0)] public int MinionCount = 2;
        [Tooltip("Game seconds before they dissolve")]
        [Min(1f)] public float MinionLifetime = 15f;

        public override float EffectDuration => Buff != null ? Duration : 0f;

        // Only with somewhere hidden to go, so a tree falls through to its next option
        // (running for cover) instead of casting for nothing.
        public override bool CanExecute(AbilityContext ctx) =>
            ctx.Relocator != null && ctx.Relocator.CanRelocateUnseen(MaxDistance);

        // The spot is looked for again as the cast goes off: everyone has moved since. If it is
        // gone by then nothing happens and the cooldown is still spent.
        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Relocator == null) return;

            if (!ctx.Relocator.TryRelocateUnseen(MaxDistance)) return;

            if (MinionPrefab != null && ctx.Summoner != null)
                ctx.Summoner.Summon(MinionPrefab, MinionCount, ctx.CasterTransform.position, MinionLifetime);
            if (Buff != null && ctx.Stats != null) ctx.Stats.AddTimed(Buff, Duration);
        }
    }
}
