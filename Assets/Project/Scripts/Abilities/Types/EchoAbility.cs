using UnityEngine;

namespace CGD.Abilities
{
    // Wrong Echo: the caster leaves a copy of itself where it stands and slips away somewhere
    // no hostile can see. Whoever was watching it is now watching the echo, which holds
    // perfectly still while looked at and creeps closer while not, mimics the noises it hears,
    // and breaks apart if stared at for long (VoidEcho prefab, EchoBody). Shooting it gives
    // the real caster an opening. Needs a caster that can summon and relocate unseen (enemies).
    [CreateAssetMenu(fileName = "EchoAbility", menuName = "CGD/Abilities/Echo")]
    public class EchoAbility : Ability
    {
        [Tooltip("Enemy prefab left behind as the echo (Prefabs/Characters/Enemies/AITest/VoidEcho)")]
        public GameObject EchoPrefab;
        [Tooltip("Game seconds before the echo dissolves on its own")]
        [Min(1f)] public float EchoLifetime = 12f;
        [Tooltip("Metres: the furthest the caster can slip away to. Keep it equal to the caster's Phase Shift distance: the hidden-spot search is then shared between the two")]
        [Min(1f)] public float MaxDistance = 15f;

        // Only with somewhere hidden to go: an echo standing next to its caster fools no one.
        public override bool CanExecute(AbilityContext ctx) =>
            EchoPrefab != null && ctx.Summoner != null && ctx.Relocator != null &&
            ctx.Relocator.CanRelocateUnseen(MaxDistance);

        public override void Execute(AbilityContext ctx)
        {
            if (ctx.Summoner == null || ctx.Relocator == null) return;

            Vector3 left = ctx.CasterTransform.position;
            if (!ctx.Relocator.TryRelocateUnseen(MaxDistance)) return;

            ctx.Summoner.Summon(EchoPrefab, 1, left, EchoLifetime);
        }

        // Vanishing is the point: casting it must not give the caster away.
        public override bool GivesAway => false;
    }
}
