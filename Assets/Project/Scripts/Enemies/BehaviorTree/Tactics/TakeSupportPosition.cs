using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A supporter's place in a fight: where it can reach the most allies of its own side with its
    // help, behind the ally nearest the threat instead of out in front of the line (or, set to
    // hold the Front, just ahead of it, between the threat and its allies), in cover that faces
    // the threat if it knows some. It weighs the known free cover round the ideal spot (on the
    // line from the threat through its allies' centre) and that spot itself (see SupportSpot),
    // claims the best cover, and goes there facing the threat. It plans again every few seconds
    // as the fight moves, so it keeps up with an advancing or retreating line. Never finishes
    // while it has allies near; fails without any. Pair it in a Parallel with what it does from
    // there (Shoot Burst, a Repeat of its abilities).
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Take Support Position", "Holds the spot that reaches the most allies, behind their line from the threat (or holding its front), in cover facing it if it knows some. Plans again as the fight moves. Fails with no allies near.")]
    public sealed class TakeSupportPosition : EnemyAction
    {
        private const float InPlace = 0.8f;

        private static readonly List<Vector3> Allies     = new();
        private static readonly List<int>     Candidates = new();

        [Tooltip("Metres it looks for allies of its own side in")]
        [SerializeField, Min(1f)] private float _allyRadius = 20f;
        [Tooltip("Metres its help reaches (keep it at its Support ability's radius): it wants as many allies as possible within this")]
        [SerializeField, Min(1f)] private float _reach = 8f;
        [Tooltip("Behind: stays behind its allies' line (a medic, a warden). Front: holds the line out in front of them (a shield-bearer)")]
        [SerializeField] private SupportLine _line;
        [Tooltip("Metres from the ally nearest the threat that it would ideally stand: behind it, or in front of it when holding the Front")]
        [SerializeField, Min(0f)] private float _fromLine = 4f;
        [Tooltip("Metres round its ideal spot it looks for cover")]
        [SerializeField, Min(1f)] private float _searchRadius = 10f;
        [Tooltip("Seconds between plans")]
        [SerializeField, Min(0.5f)] private float _replanEvery = 2f;
        [Tooltip("Speed getting there, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;

        [NonSerialized] private Vector3 _goal;
        [NonSerialized] private float   _replanAt;

        public override string Summary =>
            $"Reach {_reach:0.#} m, {_fromLine:0.#} m {(_line == SupportLine.Front ? "ahead of" : "behind")} the line";

        internal SupportLine Line => _line;

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            _replanAt = 0f;
            _goal     = Ai.transform.position;
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (context.Time >= _replanAt)
            {
                _replanAt = context.Time + _replanEvery;
                if (!Plan()) return BtStatus.Failure;
            }

            EnemyPerception perception = Ai.Perception;
            bool hasThreat = perception.Target != null;
            Ai.ReportState(hasThreat ? EnemyAI.AiState.Chase : EnemyAI.AiState.Patrol);
            Ai.ReportStep(EngageStep.Hold);
            if (hasThreat) Ai.Motion.FaceTowards(perception.TargetPosition, context.DeltaTime);

            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Vector3 offset = _goal - Ai.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > InPlace * InPlace) Ai.Routing.SetDestination(_goal);
            else Ai.Routing.Stop();
            return BtStatus.Running;
        }

        // Picks the best spot and claims it if it is cover. False when no ally is near.
        private bool Plan()
        {
            GatherAllies();
            if (Allies.Count == 0) return false;

            EnemyPerception perception = Ai.Perception;
            bool hasThreat = perception.Target != null;
            var situation = new SupportSituation(Allies, perception.TargetPosition, hasThreat, _reach, _line);
            Vector3 self  = Ai.transform.position;
            Vector3 ideal = SupportSpot.Ideal(situation, _fromLine);
            if (NavMesh.SamplePosition(ideal, out NavMeshHit hit, 3f, NavMesh.AllAreas)) ideal = hit.position;

            int     bestCover = -1;
            Vector3 best      = ideal;
            float   bestScore = SupportSpot.Score(ideal, false, false, SupportSpot.FlatDistance(self, ideal), situation);

            EnemyTactics tactics = Ai.Tactics;
            tactics.FreeKnownCover(ideal, _searchRadius, Candidates);
            foreach (int index in Candidates)
            {
                CoverPoint cover = tactics.CoverAt(index);
                bool faces  = hasThreat && tactics.CoverFaces(index, situation.Threat);
                float score = SupportSpot.Score(cover.Position, faces, cover.IsHigh,
                                                SupportSpot.FlatDistance(self, cover.Position), situation);
                if (score <= bestScore) continue;

                bestScore = score;
                bestCover = index;
                best      = cover.Position;
            }

            if (bestCover >= 0 && tactics.TryClaimCover(bestCover, out Vector3 claimed)) best = claimed;
            else if (bestCover >= 0) best = ideal;
            _goal = best;
            return true;
        }

        private void GatherAllies()
        {
            Allies.Clear();
            Vector3 self = Ai.transform.position;
            float radiusSq = _allyRadius * _allyRadius;
            var all = EnemyRoster.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnemyAI other = all[i];
                if (other == Ai || other.Health == null || other.Health.IsDead || other.Health.Team != Ai.Health.Team) continue;
                if (!other.isActiveAndEnabled) continue;

                Vector3 at = other.transform.position;
                Vector3 offset = at - self;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSq) Allies.Add(at);
            }
        }
    }
}
