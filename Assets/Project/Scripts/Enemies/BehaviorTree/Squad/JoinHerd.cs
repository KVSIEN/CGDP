using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Goes to its nearest kin and stays with them until it is within a few metres. Succeeds on
    // arrival; fails when it has no kin anywhere, so a lone enemy just carries on with what it was
    // doing.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Join Herd", "Moves to its nearest kin until within a few metres of them. Fails if it has none.")]
    public sealed class JoinHerd : EnemyAction
    {
        private const float RepathEvery = 0.5f;
        private const float PaceShare   = 0.8f;

        [Tooltip("Metres from its kin that count as being with the herd")]
        [SerializeField, Min(1f)] private float _within = 8f;
        [Tooltip("Give up if not there after this many seconds")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 12f;

        [NonSerialized] private EnemyAI _kin;
        [NonSerialized] private float   _nextRepath;
        [NonSerialized] private float   _givesUpAt;

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt  = context.Time + _giveUpAfter;
            _nextRepath = 0f;
            EnemyRoster.TryNearestKin(Ai, out _kin);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_kin == null || !EnemyRoster.IsKin(Ai, _kin) || context.Time >= _givesUpAt) return BtStatus.Failure;

            Vector3 offset = _kin.transform.position - Ai.transform.position;
            if (offset.sqrMagnitude <= _within * _within) return BtStatus.Success;

            if (context.Time >= _nextRepath)
            {
                _nextRepath = context.Time + RepathEvery;
                Ai.Agent.isStopped = false;
                if (!Ai.Routing.SetDestination(_kin.transform.position)) return BtStatus.Failure;
            }

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.SetSpeed(Ai.Data.ChaseSpeed * PaceShare);
            return BtStatus.Running;
        }
    }
}
