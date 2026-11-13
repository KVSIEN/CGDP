using UnityEngine;

namespace CGD.Abilities
{
    // A caster that can call up minions of its own side: an enemy (EnemyMinions). Null on the
    // player, so abilities that summon do nothing there.
    public interface IMinionSummoner
    {
        // Spawns `count` of the prefab around `around`, sent after the caster's hostiles, to
        // dissolve after `lifetime` seconds (or when the caster dies).
        void Summon(GameObject prefab, int count, Vector3 around, float lifetime);

        // How many of its minions are still about (alive or not yet released).
        int Alive { get; }
    }
}
