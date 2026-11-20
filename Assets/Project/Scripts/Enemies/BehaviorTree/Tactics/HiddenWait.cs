using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Core;

namespace CGD.Enemies
{
    // What an enemy does with the time it is hidden: base of the factions' own ways of lying
    // in wait (Ambush From Cover, Scan From Cover, Haunt From Cover). It stays where it is, as
    // still as the cover lets it, and its subclass watches the target and decides when the wait
    // has paid off (Success: now is the moment) or come to nothing (Failure). It fails at once
    // when it is no longer hidden: whatever it was waiting for needs the cover.
    [Serializable]
    public abstract class HiddenWait : EnemyAction
    {
        [Tooltip("Seconds it will lie in wait before giving up or finishing (random within the range)")]
        [SerializeField] private FloatRange _patience = new(5f, 8f);

        [NonSerialized] private float _startedAt;
        [NonSerialized] private float _until;

        public override string Summary => $"{_patience.Min:0.#}–{_patience.Max:0.#} s";

        protected float Elapsed(BtContext context) => context.Time - _startedAt;
        protected bool  PatienceSpent(BtContext context) => context.Time >= _until;

        protected sealed override void OnEnter(BtContext context)
        {
            _startedAt = context.Time;
            _until     = context.Time + _patience.EvaluateClamped();
            Ai.Routing.Stop();
            Begin(context);
        }

        protected sealed override BtStatus OnTick(BtContext context)
        {
            if (!Ai.Concealment.IsHidden) return BtStatus.Failure;

            Ai.ReportState(EnemyAI.AiState.Chase);   // still in the fight, so the Hold step shows
            Ai.ReportStep(EngageStep.Hold);
            Ai.Agent.isStopped = true;
            return Watch(context);
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected virtual void Begin(BtContext context) { }

        protected abstract BtStatus Watch(BtContext context);
    }
}
