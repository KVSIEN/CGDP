using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Walks back to its squad's anchor (see Anchor Tether) until it is well inside the anchor's
    // radius, so the squad closes up on the siege tank instead of drifting off ahead of it.
    // Succeeds on arrival; fails with no anchor or after taking too long.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Return To Anchor", "Walks back toward its squad's anchor until well inside its radius. Fails with no anchor.")]
    public sealed class ReturnToAnchor : EnemyAction
    {
        [Tooltip("Counts as back once within this share of the anchor's radius")]
        [SerializeField, Range(0.2f, 1f)] private float _settleShare = 0.6f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;
        [Tooltip("Walks the way its faction sneaks (Void keeps to walls, Bio slips through bushes) instead of the shortest way")]
        [SerializeField] private bool _sneak;
        [Tooltip("Seconds to get back before giving up")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 8f;

        [NonSerialized] private float _givesUpAt;

        internal bool Sneak => _sneak;

        public override string Summary => $"Within {_settleShare:P0} of its radius";

        protected override void OnEnter(BtContext context)
        {
            _givesUpAt = context.Time + _giveUpAfter;
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!EnemyRoster.TryAnchorOf(Ai, out EnemyAI anchor, out float radius) || context.Time >= _givesUpAt)
                return BtStatus.Failure;

            Vector3 at = anchor.transform.position;
            Vector3 offset = at - Ai.transform.position;
            offset.y = 0f;
            float settle = radius * _settleShare;
            if (offset.sqrMagnitude <= settle * settle) return BtStatus.Success;

            bool hasThreat = Ai.Perception.Target != null;
            Ai.ReportState(hasThreat ? EnemyAI.AiState.Chase : EnemyAI.AiState.Patrol);
            if (hasThreat) Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Ai.Routing.SetDestination(at, WalkStyle(_sneak));
            return BtStatus.Running;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
