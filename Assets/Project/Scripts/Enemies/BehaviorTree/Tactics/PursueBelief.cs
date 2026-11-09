using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // One step of following up on a lost target: moves to the place its belief picks (by style),
    // looks there for a moment, and marks it checked so the next step goes somewhere new. Succeeds
    // after the look; fails when it has nowhere left worth going or can't get there in time, so a
    // Selector can fall back to its old search.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Pursue Belief", "Goes to the likeliest unchecked place the target could be, looks, and marks it checked. Fails if none.")]
    public sealed class PursueBelief : EnemyAction
    {
        private const float ArrivalDistance = 0.8f;
        private const float DeliberateSpeed = 0.6f;   // share of chase speed for the unhurried styles

        [SerializeField] private BeliefStyle _style = BeliefStyle.Sweep;
        [Tooltip("Seconds spent looking once there")]
        [SerializeField, Min(0f)] private float _look = 1.5f;
        [Tooltip("Give up if not there after this many seconds")]
        [SerializeField, Min(1f)] private float _giveUpAfter = 10f;

        [NonSerialized] private bool    _moving;
        [NonSerialized] private bool    _arrived;
        [NonSerialized] private int     _slot;
        [NonSerialized] private Vector3 _watch;
        [NonSerialized] private float   _givesUpAt;
        [NonSerialized] private float   _lookUntil;

        public override string Summary => _style.ToString();

        protected override void OnEnter(BtContext context)
        {
            _moving    = false;
            _arrived   = false;
            _slot      = -1;
            _givesUpAt = context.Time + _giveUpAfter;
            if (!TryPick(out Vector3 goal)) return;
            if (!NavMesh.SamplePosition(goal, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return;

            _moving = Ai.Agent.SetDestination(hit.position);
            if (_moving) Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_moving || context.Time >= _givesUpAt) return BtStatus.Failure;

            if (!_arrived)
            {
                Ai.ReportState(EnemyAI.AiState.Hunt);
                Ai.ReportStep(EngageStep.Approach);
                Ai.SetSpeed(_style == BeliefStyle.Sweep ? Ai.Data.ChaseSpeed : Ai.Data.ChaseSpeed * DeliberateSpeed);
                if (!HasArrived(Ai.Agent, ArrivalDistance)) return BtStatus.Running;

                _arrived   = true;
                _lookUntil = context.Time + _look;
                Ai.Belief.MarkChecked(_slot);
            }

            Ai.ReportStep(EngageStep.Hold);
            Ai.Agent.isStopped = true;
            Ai.FaceTowards(_watch, context.DeltaTime);
            return context.Time >= _lookUntil ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        private bool TryPick(out Vector3 goal)
        {
            goal = default;
            EnemyBelief belief = Ai.Belief;
            switch (_style)
            {
                case BeliefStyle.Overwatch:
                    return belief.TryVantage(out goal, out _watch);

                case BeliefStyle.Haunt:
                    if (!belief.TryWeightedSpot(out BeliefSpot random)) return false;
                    (goal, _watch, _slot) = (random.Position, random.Position, random.Slot);
                    return true;

                default:
                    if (!belief.TrySweepSpot(out BeliefSpot best)) return false;
                    (goal, _watch, _slot) = (best.Position, best.Position, best.Slot);
                    return true;
            }
        }
    }
}
