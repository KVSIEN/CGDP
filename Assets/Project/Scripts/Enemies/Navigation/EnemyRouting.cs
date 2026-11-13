using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using CGD.Combat;

namespace CGD.Enemies
{
    // Where an enemy walks, routed round ground that would hurt it. Every destination goes
    // through here rather than straight to the NavMeshAgent: the NavMesh knows nothing of
    // hazards, and they only hurt some (a rift spares its caster's side), so the route is
    // checked against the open hazards and bent round any it crosses, a waypoint at a time.
    // A destination inside one stops at its edge instead — walking in only to step straight
    // back out (Leave Hazard), over and over, is what this prevents. Costs nothing while no
    // hazard that could hurt it is open.
    //
    // A sneaking move (RouteStyle.ThroughBushes) also passes through bushes on the way, one at a
    // time (BushHops), if the enemy's data allows the detour; hazards still come first.
    internal sealed class EnemyRouting
    {
        private const float Clearance       = 0.6f;   // metres kept from a hazard's edge, beyond its own radius
        private const float WaypointReached = 1f;
        private const float SameGoal        = 0.75f;  // a destination this close to the last is the same one
        private const float RecheckInterval = 0.25f;  // a hazard opening across its path is noticed this soon
        private const float SnapDistance    = 1.5f;
        private const int   MaxCorners      = 32;

        private readonly EnemyAI _ai;
        private readonly NavMeshPath _path = new();
        private readonly Vector3[] _corners = new Vector3[MaxCorners];
        private readonly Func<Vector3, bool> _walkable;
        private readonly List<Vector3> _bushes = new();

        private Vector3 _goal;
        private Vector3 _waypoint;
        private bool    _hasGoal;
        private bool    _detouring;   // heading for a waypoint round a hazard
        private bool    _hopping;     // heading for a bush on the way
        private RouteStyle _style;
        private float   _nextCheck;

        public EnemyRouting(EnemyAI ai)
        {
            _ai       = ai;
            _walkable = IsWalkable;
        }

        private NavMeshAgent  Agent  => _ai.Agent;
        private HealthManager Self   => _ai.Health;
        private float         Margin => Agent.radius + Clearance;

        // Same contract as NavMeshAgent.SetDestination: false if no route could be requested.
        public bool SetDestination(Vector3 goal, RouteStyle style = RouteStyle.Direct)
        {
            // Asked again for where it is already headed (nodes re-send it every tick): keep going round.
            if ((_detouring || _hopping) && style == _style && (goal - _goal).sqrMagnitude < SameGoal * SameGoal)
            {
                _goal = goal;
                return true;
            }

            _goal    = goal;
            _style   = style;
            _hasGoal = true;
            return Route();
        }

        // Stands still: drops the path and the destination behind it.
        public void Stop()
        {
            Clear();
            if (Agent.isOnNavMesh) Agent.ResetPath();
        }

        // Forgets the destination without touching the agent (death, respawn).
        public void Clear()
        {
            _hasGoal = false;
            SetWaypoint(detour: false, hop: false);
        }

        // Each frame: on to the destination once round a hazard (or once it has closed), and a
        // fresh look at the route now and then while hazards are open, in case one opened across it.
        public void Tick(float time)
        {
            if (!_hasGoal || !Agent.isOnNavMesh) return;

            if (_hopping)
            {
                // At the bush, or as close as the NavMesh gets it: on to the next bush, or the destination.
                bool atBush = Flat(Agent.transform.position - _waypoint).sqrMagnitude < WaypointReached * WaypointReached ||
                              (!Agent.pathPending && (!Agent.hasPath || Agent.remainingDistance < WaypointReached));
                if (atBush) Route();
                else if (time >= _nextCheck && Hazards.AnyHurting(Self))
                {
                    _nextCheck = time + RecheckInterval;
                    Route();
                }
                return;
            }

            if (_detouring)
            {
                bool reached = Flat(Agent.transform.position - _waypoint).sqrMagnitude < WaypointReached * WaypointReached;
                if (reached || !Hazards.AnyHurting(Self)) Reroute();
                return;
            }

            if (time < _nextCheck || !Hazards.AnyHurting(Self)) return;
            _nextCheck = time + RecheckInterval;
            if (Agent.hasPath && !Agent.pathPending && Agent.remainingDistance > WaypointReached) Route();
        }

        private void Reroute()
        {
            SetWaypoint(detour: false, hop: false);
            Route();
        }

        private bool Route()
        {
            if (!Agent.isOnNavMesh) return false;

            if (!Hazards.AnyHurting(Self))
            {
                if (_style == RouteStyle.ThroughBushes && TryFindBushOnTheWay(out Vector3 bush))
                {
                    _waypoint = bush;
                    SetWaypoint(detour: false, hop: true);
                    return Agent.SetDestination(bush);
                }
                SetWaypoint(detour: false, hop: false);
                return Agent.SetDestination(_goal);
            }

            Vector3 from = Agent.transform.position;
            Vector3 goal = OutsideHazards(from, _goal);
            if (!NavMesh.CalculatePath(from, goal, NavMesh.AllAreas, _path) || _path.status == NavMeshPathStatus.PathInvalid)
            {
                SetWaypoint(detour: false, hop: false);
                return Agent.SetDestination(goal);
            }

            if (TryFindWayRound(out Vector3 waypoint))
            {
                _waypoint = waypoint;
                SetWaypoint(detour: true, hop: false);
                return Agent.SetDestination(waypoint);
            }

            SetWaypoint(detour: false, hop: false);
            return Agent.SetPath(_path);
        }

        // The next bush to pass through, on the NavMesh, if its data allows the detour.
        private bool TryFindBushOnTheWay(out Vector3 bush)
        {
            bush = default;
            float detour = _ai.Data.BushDetour;
            if (detour <= 0f) return false;

            BushHops.CollectCenters(_bushes);
            if (!BushHops.TryPick(Agent.transform.position, _goal, detour, _bushes, out Vector3 center)) return false;
            if (!NavMesh.SamplePosition(center, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;

            bush = hit.position;
            return true;
        }

        // A waypoint round the first hazard the planned path (_path) runs through. False when it
        // crosses none, or there is no way round (it fills the passage): then the plain route,
        // and Leave Hazard if it must.
        private bool TryFindWayRound(out Vector3 waypoint)
        {
            waypoint = default;
            int count = _path.GetCornersNonAlloc(_corners);
            for (int i = 0; i + 1 < count; i++)
            {
                if (!Hazards.TryFindCrossing(_corners[i], _corners[i + 1], Self, Margin * 0.5f, out HazardZone zone)) continue;
                if (!Hazards.TryFindWayRound(zone, _corners[i], _corners[i + 1], Self, Margin, _walkable, out Vector3 round)) return false;
                if (!NavMesh.SamplePosition(round, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;

                waypoint = hit.position;
                return true;
            }
            return false;
        }

        // A destination inside a hazard that would hurt it becomes the nearest edge on its own
        // side. Already standing in that one, it may carry on (it is in anyway).
        private Vector3 OutsideHazards(Vector3 from, Vector3 goal)
        {
            if (!Hazards.TryFindAt(goal, Self, out HazardZone zone) || zone.Covers(from)) return goal;

            Vector3 outward = Flat(from - zone.Center);
            if (outward.sqrMagnitude < 0.0001f) return goal;

            Vector3 edge = zone.Center + outward.normalized * (zone.Radius + Margin);
            edge.y = goal.y;
            if (IsWalkable(edge) && NavMesh.SamplePosition(edge, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas))
                return hit.position;
            return Hazards.TryFindWayOut(goal, Self, _walkable, out Vector3 exit) ? exit : goal;
        }

        // Through a waypoint (round a hazard, or a bush on the way) it keeps its pace instead of
        // braking as if it were the end of the road.
        private void SetWaypoint(bool detour, bool hop)
        {
            _detouring = detour;
            _hopping   = hop;
            if (Agent.isOnNavMesh) Agent.autoBraking = !(detour || hop);
        }

        private bool IsWalkable(Vector3 spot) =>
            NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas) &&
            Hazards.DamagePerSecondAt(hit.position, Self) <= 0f;

        private static Vector3 Flat(Vector3 vector) => new(vector.x, 0f, vector.z);
    }
}
