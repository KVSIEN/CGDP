using System;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks the enemy's waypoint loop, or without waypoints guards its post, looking around
    // now and then. Faster (and more watchful) while on edge after a search. Never finishes:
    // put it last, after branches that abort lower priority, so anything more urgent interrupts it.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Patrol", "Walks the waypoint loop, or guards its post looking around. Keeps running until interrupted.")]
    public sealed class Patrol : EnemyAction
    {
        private const float ArrivalDistance = 0.4f;

        [NonSerialized] private int _waypointIndex;

        internal override bool NeverFinishes => true;

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnEnter(BtContext context)
        {
            Ai.Agent.isStopped = false;
            Ai.Idle.Cancel();
            if (Ai.Waypoints.Length > 0)
                Ai.Routing.SetDestination(Ai.Waypoints[_waypointIndex % Ai.Waypoints.Length].position);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Patrol);
            var perception = Ai.Perception;
            float time = UnityEngine.Time.time;
            Ai.Motion.SetSpeed(Ai.Data.PatrolSpeed * (perception.IsCautious ? perception.Profile.CautionPatrolSpeed : 1f) * Ai.Idle.PaceScale(time));

            var waypoints = Ai.Waypoints;
            if (waypoints.Length == 0)
            {
                Ai.Post.TickGuard(context.DeltaTime, UnityEngine.Time.time);
                return BtStatus.Running;
            }
            // A stop at a waypoint or dead mid-route, doing a beat of its faction's routine.
            if (Ai.Idle.TickStop(context.DeltaTime, time)) return BtStatus.Running;
            if (!HasArrived(Ai.Agent, ArrivalDistance))
            {
                Ai.Idle.MaybeHalt(context.DeltaTime, time);
                return BtStatus.Running;
            }

            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            Ai.Routing.SetDestination(waypoints[_waypointIndex].position);
            Ai.Idle.BeginStop(time);
            return BtStatus.Running;
        }
    }
}
