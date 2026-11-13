using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Keeps close behind its squad's leader, off to one side by its place in the squad —
    // escorts, and squads that move as a group instead of each patrolling alone. Keeps
    // running until interrupted; fails if it has no squad or is the leader itself.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Follow Leader", "Keeps close behind its squad's leader. Runs until interrupted; fails for the leader itself.")]
    public sealed class FollowLeader : EnemyAction
    {
        private const float Repath = 0.3f;

        [SerializeField, Min(1f)] private float _distance = 3f;

        [NonSerialized] private float _nextPath;

        public override string Summary => $"{_distance:0.#} m behind";

        internal override bool NeverFinishes => true;

        protected override BtStatus OnTick(BtContext context)
        {
            EnemySquad squad  = Ai.Squad;
            EnemyAI    leader = squad != null ? squad.Leader : null;
            if (leader == null || leader == Ai) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Patrol);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed);
            if (context.Time < _nextPath) return BtStatus.Running;
            _nextPath = context.Time + Repath;

            Transform lead = leader.transform;
            int   place = Mathf.Max(1, IndexIn(squad));
            float side  = place % 2 == 0 ? 1f : -1f;
            Vector3 spot = lead.position - lead.forward * _distance + lead.right * (side * _distance * 0.5f * ((place + 1) / 2));

            float distance = Vector3.Distance(Ai.transform.position, spot);
            Ai.Agent.isStopped = distance < 0.8f;
            if (!Ai.Agent.isStopped) Ai.Routing.SetDestination(spot);
            return BtStatus.Running;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        private int IndexIn(EnemySquad squad)
        {
            var members = squad.Members;
            for (int i = 0; i < members.Count; i++)
                if (members[i] == Ai) return i;
            return 1;
        }
    }
}
