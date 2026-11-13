using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;
using CGD.Stealth;

namespace CGD.Enemies
{
    // A calm enemy's roam from bush to bush round its post instead of walking a fixed route:
    // picks a bush within reach (rarely the one it just left), sneaks there through any bushes
    // on the way, and stops in it for a beat of its faction's idle routine (a Bio creature
    // sniffs and rests) before moving on. Never finishes while there are bushes to roam; fails
    // when there are none within reach, so a Patrol after it takes over.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Roam Bushes", "Roams from bush to bush around its post, pausing in each. Fails when no bush is in reach; otherwise runs until interrupted.")]
    public sealed class RoamBushes : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;
        private const float SnapDistance    = 2f;
        private const float RecentWeight    = 0.2f;   // the bush before last is this much less likely
        private const float NearbyFalloff   = 12f;    // metres: nearer bushes are likelier, halving around this far
        private const int   PickAttempts    = 3;

        [Tooltip("Metres around its post it roams")]
        [SerializeField, Min(2f)] private float _radius = 25f;

        [NonSerialized] private ConcealmentZone _heading;
        [NonSerialized] private ConcealmentZone _last;
        [NonSerialized] private ConcealmentZone _beforeLast;

        public override string Summary => $"Within {_radius:0} m";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            Ai.Agent.isStopped = false;
            Ai.Idle.Cancel();
            _heading = null;
            TryPickNext();
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override BtStatus OnTick(BtContext context)
        {
            if (_heading == null && !TryPickNext()) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Patrol);
            var perception = Ai.Perception;
            float time = Time.time;
            Ai.Motion.SetSpeed(Ai.Data.PatrolSpeed * (perception.IsCautious ? perception.Profile.CautionPatrolSpeed : 1f) * Ai.Idle.PaceScale(time));

            // In a bush, doing a beat of its faction's routine; or halting on the way now and then.
            if (Ai.Idle.TickStop(context.DeltaTime, time)) return BtStatus.Running;
            if (!HasArrived(Ai.Agent, ArrivalDistance))
            {
                Ai.Idle.MaybeHalt(context.DeltaTime, time);
                return BtStatus.Running;
            }

            _beforeLast = _last;
            _last       = _heading;
            _heading    = null;
            TryPickNext();
            Ai.Idle.BeginStop(time);
            return BtStatus.Running;
        }

        // A few tries, in case the bush drawn can't be walked to.
        private bool TryPickNext()
        {
            for (int attempt = 0; attempt < PickAttempts; attempt++)
                if (PickNext()) return true;
            return false;
        }

        // A bush within its roaming radius other than the one it is in, the nearer and the less
        // recently visited the likelier, and heads for it.
        private bool PickNext()
        {
            IReadOnlyList<ConcealmentZone> zones = ConcealmentZone.Active;
            Vector3 self = Ai.transform.position;

            float total = 0f;
            for (int i = 0; i < zones.Count; i++) total += Weight(zones[i], self);
            if (total <= 0f) return false;

            float pick = UnityEngine.Random.value * total;
            for (int i = 0; i < zones.Count; i++)
            {
                float weight = Weight(zones[i], self);
                if (weight <= 0f) continue;
                pick -= weight;
                if (pick > 0f) continue;
                return HeadFor(zones[i]);
            }
            return false;
        }

        private float Weight(ConcealmentZone zone, Vector3 self)
        {
            if (!BushHops.Hides(zone) || zone == _last) return 0f;

            Vector3 center = zone.GroundCenter;
            if (!Ai.Post.IsNear(center, _radius)) return 0f;

            Vector3 offset = center - self;
            offset.y = 0f;
            float weight = 1f / (1f + offset.magnitude / NearbyFalloff);
            return zone == _beforeLast ? weight * RecentWeight : weight;
        }

        private bool HeadFor(ConcealmentZone zone)
        {
            if (!NavMesh.SamplePosition(zone.GroundCenter, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;
            if (!Ai.Routing.SetDestination(hit.position, RouteStyle.ThroughBushes)) return false;

            _heading = zone;
            Ai.Agent.isStopped = false;
            return true;
        }
    }
}
