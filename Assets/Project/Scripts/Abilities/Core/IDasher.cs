using System;
using UnityEngine;

namespace CGD.Abilities
{
    // A caster that can dash straight ahead under its own power: an enemy (EnemyDash). Null on
    // the player, whose dashes go through PlayerDodge (DashAbility), so abilities that need it
    // (Thruster Dash) can't execute there.
    public interface IDasher
    {
        bool IsDashing { get; }

        // Dashes `distance` metres the way it is facing over `seconds`, stopping short of walls
        // and ledges. `passed` is told each point it reaches every `spacing` metres along the
        // way, starting where it sets off (for something left behind, like a trail of fire).
        void Begin(float distance, float seconds, float spacing, Action<Vector3> passed);
    }
}
