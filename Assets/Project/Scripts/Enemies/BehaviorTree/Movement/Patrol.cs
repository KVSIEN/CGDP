using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks the enemy's patrol: its waypoint loop, stepping off it now and then to somewhere its
    // experience points to (PatrolDoctrine.RouteDrift); or, with no waypoints, going from one
    // place of its own choosing to the next (EnemyExploration), or guarding its post when its
    // faction would rather not explore. Faster (and more watchful) while on edge after a search.
    // Never finishes: put it last, after branches that abort lower priority, so anything more
    // urgent interrupts it.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Patrol", "Walks the waypoint loop, or without one explores on its own, or guards its post. Keeps running until interrupted.")]
    public sealed class Patrol : EnemyAction
    {
        private const float ArrivalDistance = 0.4f;
        // A stop it cannot reach in this long is struck off as seen, so it does not pick it again.
        private const float GiveUpAfter     = 45f;
        private const float RetryAfter      = 1f;

        private enum Heading { None, Waypoint, Stop }

        [NonSerialized] private int     _waypointIndex;
        [NonSerialized] private Heading _heading;
        [NonSerialized] private Vector3 _stop;
        [NonSerialized] private float   _giveUpAt;
        [NonSerialized] private float   _retryAt;

        internal override bool NeverFinishes => true;

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnEnter(BtContext context)
        {
            Ai.Agent.isStopped = false;
            Ai.Idle.Cancel();
            _heading = Heading.None;
            _retryAt = 0f;
            if (Ai.Waypoints.Length > 0) HeadForWaypoint();
        }

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Patrol);
            var perception = Ai.Perception;
            float time = Time.time;
            Ai.Motion.SetSpeed(Ai.Data.PatrolSpeed * (perception.IsCautious ? perception.Profile.CautionPatrolSpeed : 1f) * Ai.Idle.PaceScale(time));

            bool hasRoute = Ai.Waypoints.Length > 0;
            if (!hasRoute && !Ai.Exploration.ExploresWithoutRoute)
            {
                Ai.Post.TickGuard(context.DeltaTime, time);
                return BtStatus.Running;
            }

            // A stop at a waypoint or dead mid-route, doing a beat of its faction's routine.
            if (Ai.Idle.TickStop(context.DeltaTime, time)) return BtStatus.Running;

            if (_heading == Heading.Stop && (Ai.Exploration.NeedsReplan || time >= _giveUpAt))
            {
                if (time >= _giveUpAt) Ai.Exploration.Visit(_stop, time);
                ChooseNext(time, mayDetour: false);
            }
            else if (_heading == Heading.None && time >= _retryAt)
            {
                ChooseNext(time, mayDetour: true);
            }

            if (_heading == Heading.None)
            {
                // Nowhere it wants to go yet: it stands its ground, and tries again shortly.
                Ai.Post.TickGuard(context.DeltaTime, time);
                return BtStatus.Running;
            }

            if (!HasArrived(Ai.Agent, ArrivalDistance))
            {
                Ai.Idle.MaybeHalt(context.DeltaTime, time);
                return BtStatus.Running;
            }

            OnArrived(time);
            return BtStatus.Running;
        }

        // It reached the waypoint or the place it chose: it may stop for a beat, then picks the next.
        private void OnArrived(float time)
        {
            Ai.Exploration.Visit(Ai.transform.position, time);
            if (_heading == Heading.Waypoint) _waypointIndex = (_waypointIndex + 1) % Ai.Waypoints.Length;

            bool wasWaypoint = _heading == Heading.Waypoint;
            ChooseNext(time, mayDetour: wasWaypoint);
            Ai.Idle.BeginStop(time);
        }

        // On a route: usually the next waypoint, now and then (only from a waypoint) somewhere of its
        // own choosing before carrying on. Without one: always somewhere of its own choosing.
        private void ChooseNext(float time, bool mayDetour)
        {
            if (Ai.Waypoints.Length == 0)
            {
                if (!TryHeadForStop(time)) NoStop(time);
                return;
            }
            if (mayDetour && Ai.Exploration.WantsDetour() && TryHeadForStop(time)) return;
            HeadForWaypoint();
        }

        private void HeadForWaypoint()
        {
            Transform[] waypoints = Ai.Waypoints;
            _heading = Heading.Waypoint;
            Ai.Routing.SetDestination(waypoints[_waypointIndex % waypoints.Length].position, RouteStyle.ThroughBushes);
        }

        private bool TryHeadForStop(float time)
        {
            if (!Ai.Exploration.TryPickStop(time, out _stop)) return false;
            if (!Ai.Routing.SetDestination(_stop, RouteStyle.ThroughBushes))
            {
                Ai.Exploration.Visit(_stop, time);
                return false;
            }
            _heading  = Heading.Stop;
            _giveUpAt = time + GiveUpAfter;
            return true;
        }

        private void NoStop(float time)
        {
            _heading = Heading.None;
            _retryAt = time + RetryAfter;
        }
    }
}
