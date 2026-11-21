using System;
using UnityEngine;

namespace CGD.Abilities
{
    // A caster that can dash under its own power: an enemy (EnemyDash). Null on the player,
    // whose dashes go through PlayerDodge (DashAbility), so abilities that need it (Thruster
    // Dash, Smoke Screen, Venom Bite) can't execute there.
    public interface IDasher
    {
        bool IsDashing { get; }

        // How far a dash along `direction` (flattened onto the ground) would get before a wall
        // or ledge stops it, up to `distance`.
        float Reach(Vector3 direction, float distance);

        // Dashes `distance` metres along `direction` (flattened onto the ground) over `seconds`,
        // stopping short of walls and ledges. `passed` is told each point it reaches every
        // `spacing` metres along the way, starting where it sets off (for something left
        // behind, like a trail of fire); `arrived` is told where it ended, once it got there
        // (not when a stun cut it short). Either may be null.
        void Begin(Vector3 direction, float distance, float seconds, float spacing,
                   Action<Vector3> passed, Action<Vector3> arrived = null);
    }
}
