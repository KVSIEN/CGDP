using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // A dead enemy lying where it fell, for its allies to find, and who landed the last hit on
    // it (null if nobody did), for those that would avenge it.
    public readonly struct FallenAlly
    {
        public readonly EnemyAI       Body;
        public readonly Vector3       Position;
        public readonly Team          Team;
        public readonly HealthManager Killer;

        public FallenAlly(EnemyAI body, Vector3 position, Team team, HealthManager killer = null)
        {
            Body = body; Position = position; Team = team; Killer = killer;
        }
    }
}
