using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Keeps loosely behind its squad's leader, off to one side by its place in the squad —
    // escorts, and squads that move as a group instead of each patrolling alone. It only
    // re-picks its spot once the leader has moved well away from the last one, and stands still
    // while it is near it, glancing about, so it trails the leader instead of shadowing its every turn. Keeps
    // running until interrupted; fails if it has no squad or is the leader itself.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Follow Leader", "Keeps loosely behind its squad's leader. Runs until interrupted; fails for the leader itself.")]
    public sealed class FollowLeader : EnemyAction
    {
        [SerializeField, Min(1f)] private float _distance = 3f;

        [NonSerialized] private StationKeeper _station;
        // Where it was facing when it settled, so its glances stay about that.
        [NonSerialized] private Quaternion _restFacing;
        [NonSerialized] private bool       _wasMoving;

        public override string Summary => $"{_distance:0.#} m behind";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            _station ??= new StationKeeper(retarget: 3f, arrive: 1.2f, depart: 3f);
            _station.Reset();
            _wasMoving = true;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            EnemySquad squad  = Ai.Squad;
            EnemyAI    leader = squad != null ? squad.Leader : null;
            if (leader == null || leader == Ai) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Patrol);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed);

            Transform lead = leader.transform;
            int   place = Mathf.Max(1, IndexIn(squad));
            float side  = place % 2 == 0 ? 1f : -1f;
            Vector3 spot = lead.position - lead.forward * _distance + lead.right * (side * _distance * 0.5f * ((place + 1) / 2));

            _station.Update(spot, Ai.transform.position);
            Ai.Agent.isStopped = !_station.Moving;
            if (_station.Moving)
            {
                Ai.Routing.SetDestination(_station.Goal);
                _wasMoving = true;
                return BtStatus.Running;
            }

            // Waiting on the squad: looks about for anything worth reporting, not just down the way it came.
            if (_wasMoving) _restFacing = Ai.transform.rotation;
            _wasMoving = false;
            Ai.Idle.Glance(_restFacing, context.DeltaTime, Time.time);
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
