using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A calm enemy's roam from bush to bush round its post instead of walking a fixed route:
    // picks a bush it knows within reach (rarely the one it just left), sneaks there through any
    // bushes on the way, goes in at the edge it meets and stops a little way into it for a beat of its faction's idle routine (a Bio creature
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

        [NonSerialized] private BushMemory.KnownBush _heading;
        [NonSerialized] private BushMemory.KnownBush _last;
        [NonSerialized] private BushMemory.KnownBush _beforeLast;

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
            IReadOnlyList<BushMemory.KnownBush> known = Ai.Ground.Bushes.Bushes;
            Vector3 self = Ai.transform.position;

            var choice = new WeightedChoice<BushMemory.KnownBush>();
            for (int i = 0; i < known.Count; i++) choice.Offer(known[i], Weight(known[i], self));
            return choice.HasPick && HeadFor(choice.Picked, self);
        }

        private float Weight(BushMemory.KnownBush bush, Vector3 self)
        {
            if (bush.Zone == null || bush == _last) return 0f;

            Vector3 center = bush.Area.Centre;
            if (!Ai.Post.IsNear(center, _radius)) return 0f;

            Vector3 offset = center - self;
            offset.y = 0f;
            float weight = 1f / (1f + offset.magnitude / NearbyFalloff);
            return bush == _beforeLast ? weight * RecentWeight : weight;
        }

        // To a spot a little way into the bush from the side it comes at it, not its middle.
        private bool HeadFor(BushMemory.KnownBush bush, Vector3 self)
        {
            Vector3 rest = BushApproach.RestSpot(self, bush.Area);
            if (!NavMesh.SamplePosition(rest, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) return false;
            if (!Ai.Routing.SetDestination(hit.position, RouteStyle.ThroughBushes)) return false;

            _heading = bush;
            Ai.Agent.isStopped = false;
            return true;
        }
    }
}
