using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks the enemy's waypoint loop, or without waypoints guards its post, looking around
    // now and then. Faster (and more watchful) while on edge after a search. Never finishes:
    // put it last under a reactive Selector so anything more urgent interrupts it.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Patrol", "Walks the waypoint loop, or guards its post looking around. Keeps running until interrupted.")]
    public sealed class Patrol : EnemyAction
    {
        private const float ArrivalDistance = 0.4f;

        [NonSerialized] private int _waypointIndex;

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnEnter(BtContext context)
        {
            Ai.Agent.isStopped = false;
            if (Ai.Waypoints.Length > 0)
                Ai.Agent.SetDestination(Ai.Waypoints[_waypointIndex % Ai.Waypoints.Length].position);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Patrol);
            var perception = Ai.Perception;
            Ai.SetSpeed(Ai.Data.PatrolSpeed * (perception.IsCautious ? perception.Profile.CautionPatrolSpeed : 1f));

            var waypoints = Ai.Waypoints;
            if (waypoints.Length == 0)
            {
                Ai.Post.TickGuard(context.DeltaTime, UnityEngine.Time.time);
                return BtStatus.Running;
            }
            if (!HasArrived(Ai.Agent, ArrivalDistance)) return BtStatus.Running;

            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            Ai.Agent.SetDestination(waypoints[_waypointIndex].position);
            return BtStatus.Running;
        }
    }
}
