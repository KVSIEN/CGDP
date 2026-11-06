using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // A dead enemy lying where it fell, for its allies to find.
    public readonly struct FallenAlly
    {
        public readonly EnemyAI Body;
        public readonly Vector3 Position;
        public readonly Team    Team;

        public FallenAlly(EnemyAI body, Vector3 position, Team team)
        {
            Body = body; Position = position; Team = team;
        }
    }
}
