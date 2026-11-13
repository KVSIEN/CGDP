using UnityEngine;

namespace CGD.Abilities
{
    // Calls up minions round the caster — a necromancer raising the dead, a hive spitting out
    // drones — sent straight after its hostiles and gone when their time is up (or the caster
    // falls). Any faction can use it: the minions are whatever enemy prefab it names, which
    // should be on the caster's team. Enemies only (the player has no summoner), and only
    // while it has fewer than Max Alive of its own about, so it never floods the level.
    [CreateAssetMenu(fileName = "SummonAbility", menuName = "CGD/Abilities/Summon")]
    public class SummonAbility : Ability
    {
        [Tooltip("Enemy prefab it calls up (on the caster's team)")]
        public GameObject MinionPrefab;
        [Tooltip("How many each cast")]
        [Min(1)] public int Count = 3;
        [Tooltip("Game seconds before they dissolve")]
        [Min(1f)] public float Lifetime = 20f;
        [Tooltip("It won't cast while this many of its minions are still about")]
        [Min(1)] public int MaxAlive = 4;

        public override bool CanExecute(AbilityContext ctx) =>
            MinionPrefab != null && ctx.Summoner != null && ctx.Summoner.Alive < MaxAlive;

        public override void Execute(AbilityContext ctx)
        {
            if (MinionPrefab == null || ctx.Summoner == null) return;
            ctx.Summoner.Summon(MinionPrefab, Count, ctx.CasterTransform.position, Lifetime);
        }
    }
}
