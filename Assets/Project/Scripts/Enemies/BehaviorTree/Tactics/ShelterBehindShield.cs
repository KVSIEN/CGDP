using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Moving cover: picks the nearest ally of its own side carrying a Frontal Shield that is up,
    // and keeps behind it — a couple of metres back from the threat and a step to one side, so
    // it can lean out and shoot past the bearer's shoulder. It follows the bearer as it advances.
    // Fails when there is no bearer, or once the bearer falls or its shield breaks, so the tree
    // can find real cover instead. Pair it in a Parallel with Shoot Burst.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Shelter Behind Shield", "Keeps behind the nearest ally with a Frontal Shield up, a step to one side, facing the threat. Fails once the shield breaks or there is none.")]
    public sealed class ShelterBehindShield : EnemyAction
    {
        private const float InPlace = 0.7f;

        [Tooltip("Metres it looks for a shield-bearer in")]
        [SerializeField, Min(1f)] private float _searchRadius = 18f;
        [Tooltip("Metres behind the bearer, away from the threat, that it stands")]
        [SerializeField, Min(0.5f)] private float _behind = 2f;
        [Tooltip("Metres to one side of the line from the threat, to shoot past the bearer")]
        [SerializeField, Min(0f)] private float _side = 1.1f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;

        [NonSerialized] private FrontalShield _shield;
        [NonSerialized] private EnemyAI       _bearer;
        [NonSerialized] private float         _sideSign;

        public override string Summary => $"{_behind:0.#} m behind, within {_searchRadius:0} m";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            _bearer = NearestBearer(out _shield);
            // Two sheltering behind one bearer take opposite shoulders.
            _sideSign = (Ai.GetInstanceID() & 1) == 0 ? 1f : -1f;
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_bearer == null || _bearer.Health.IsDead || !_bearer.isActiveAndEnabled || !_shield.IsUp)
                return BtStatus.Failure;

            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;

            Vector3 threat = perception.TargetPosition;
            Vector3 bearer = _bearer.transform.position;
            Vector3 away   = Flat(bearer - threat);
            if (away.sqrMagnitude < 0.01f) return BtStatus.Failure;

            away.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, away) * _sideSign;
            Vector3 spot = bearer + away * _behind + side * _side;

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.ReportStep(EngageStep.Hold);
            Ai.Motion.FaceTowards(threat, context.DeltaTime);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            if (Flat(spot - Ai.transform.position).sqrMagnitude > InPlace * InPlace) Ai.Routing.SetDestination(spot);
            else Ai.Routing.Stop();
            return BtStatus.Running;
        }

        private EnemyAI NearestBearer(out FrontalShield shield)
        {
            shield = null;
            EnemyAI best = null;
            float bestSq = _searchRadius * _searchRadius;
            var all = EnemyRoster.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnemyAI other = all[i];
                if (other == Ai || other.Health == null || other.Health.IsDead || other.Health.Team != Ai.Health.Team) continue;

                float sq = Flat(other.transform.position - Ai.transform.position).sqrMagnitude;
                if (sq > bestSq || !other.TryGetComponent(out FrontalShield candidate) || !candidate.IsUp) continue;

                bestSq = sq;
                best   = other;
                shield = candidate;
            }
            return best;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
