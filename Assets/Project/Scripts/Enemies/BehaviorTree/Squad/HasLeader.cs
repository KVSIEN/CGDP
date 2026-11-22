using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it is in a squad someone else leads and nothing has caught its attention —
    // the gate in front of Follow Leader, so the leader itself (and an enemy on its own) goes on
    // to its own routine instead, and so does a follower that heard something or was hit: it
    // goes to look rather than keeping its place in the column. With Farther Than set, it only
    // succeeds once the leader has got that far away: until then the follower explores on its
    // own nearby and only closes up when the squad is drifting apart.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Has Leader", "Succeeds while it is in a squad led by someone else (optionally only once the leader is far off) and is not suspicious.")]
    public sealed class HasLeader : EnemyCondition
    {
        [Tooltip("Metres: only succeed once the leader is farther away than this, so it explores on its own nearby (0 = whenever it has a leader)")]
        [SerializeField, Min(0f)] private float _fartherThan;

        public override string Summary => _fartherThan > 0f ? $"Farther than {_fartherThan:0.#} m" : "";

        protected override bool Check(BtContext context)
        {
            EnemyAI leader = Ai.Squad != null ? Ai.Squad.Leader : null;
            if (leader == null || leader == Ai || Ai.Perception.IsSuspicious) return false;
            if (_fartherThan <= 0f) return true;

            Vector3 gap = leader.transform.position - Ai.transform.position;
            gap.y = 0f;
            return gap.sqrMagnitude > _fartherThan * _fartherThan;
        }
    }
}
