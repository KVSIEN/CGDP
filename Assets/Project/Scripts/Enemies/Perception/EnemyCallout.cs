using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // An enemy shouting that it has spotted a hostile, and where it saw it.
    public readonly struct EnemyCallout
    {
        public readonly EnemyAI Caller;
        public readonly Vector3 From;
        public readonly Vector3 Reported;
        public readonly Team    Team;
        public readonly float   Radius;

        public EnemyCallout(EnemyAI caller, Vector3 from, Vector3 reported, Team team, float radius)
        {
            Caller = caller; From = from; Reported = reported; Team = team; Radius = radius;
        }
    }
}
