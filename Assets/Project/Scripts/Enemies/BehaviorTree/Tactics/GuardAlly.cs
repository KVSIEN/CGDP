using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A bodyguard's place: picks the ally of its own side it protects — the highest tier within
    // reach (a boss, a broodmother, an engineer), ties going to the same one for every guard —
    // and keeps between it and the threat, a couple of metres out in front of it, facing the
    // threat. With no threat it stays close to its ward. Never finishes while the ward lives;
    // fails when there is nobody to guard. Pair it in a Parallel with Shoot Burst.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Guard Ally", "Keeps between its highest-tier ally nearby and the threat, facing the threat. Runs while the ward lives.")]
    public sealed class GuardAlly : EnemyAction
    {
        private const float InPlace = 0.8f;

        [Tooltip("Metres it looks for a ward in")]
        [SerializeField, Min(1f)] private float _searchRadius = 25f;
        [Tooltip("Metres in front of the ward, toward the threat, that it stands")]
        [SerializeField, Min(0.5f)] private float _standOff = 2.5f;
        [Tooltip("Metres it lets its ward get from it when there is no threat")]
        [SerializeField, Min(1f)] private float _leash = 5f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;

        [NonSerialized] private EnemyAI _ward;

        public override string Summary => $"{_standOff:0.#} m in front";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            _ward = PickWard();
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_ward == null || _ward.Health.IsDead || !_ward.isActiveAndEnabled) return BtStatus.Failure;

            Vector3 ward = _ward.transform.position;
            EnemyPerception perception = Ai.Perception;
            Vector3 spot;
            if (perception.Target != null)
            {
                Vector3 threat = perception.TargetPosition;
                Vector3 toThreat = Flat(threat - ward);
                spot = toThreat.sqrMagnitude > 0.01f ? ward + toThreat.normalized * _standOff : ward;
                Ai.ReportState(EnemyAI.AiState.Chase);
                Ai.ReportStep(EngageStep.Hold);
                Ai.Motion.FaceTowards(threat, context.DeltaTime);
            }
            else
            {
                spot = ward;
                if (Flat(ward - Ai.transform.position).sqrMagnitude <= _leash * _leash)
                {
                    Ai.Routing.Stop();
                    return BtStatus.Running;
                }
            }

            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            if (Flat(spot - Ai.transform.position).sqrMagnitude > InPlace * InPlace) Ai.Routing.SetDestination(spot);
            else Ai.Routing.Stop();
            return BtStatus.Running;
        }

        // The highest tier ally of its team within reach; among equals, the same one for everyone
        // (the lowest instance id), so a pair of guards doesn't end up guarding each other.
        private EnemyAI PickWard()
        {
            EnemyAI best = null;
            var all = EnemyRoster.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnemyAI other = all[i];
                if (other == Ai || other.Health == null || other.Health.IsDead || other.Health.Team != Ai.Health.Team) continue;
                if (Flat(other.transform.position - Ai.transform.position).sqrMagnitude > _searchRadius * _searchRadius) continue;
                if (best == null || Better(other, best)) best = other;
            }
            return best;
        }

        private static bool Better(EnemyAI a, EnemyAI b)
        {
            int tierA = a.Data != null ? a.Data.Tier : 0;
            int tierB = b.Data != null ? b.Data.Tier : 0;
            return tierA != tierB ? tierA > tierB : a.GetInstanceID() < b.GetInstanceID();
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
