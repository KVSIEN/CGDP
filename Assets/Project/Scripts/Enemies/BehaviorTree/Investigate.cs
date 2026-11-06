using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Looks into a lead with EnemySearch: stops to stare at what it noticed, goes to where it
    // believes it is (its guess, not the truth) and searches a few spots around it, leaning
    // the way a lost target seemed to go. New leads on the way re-route it. Succeeds when the
    // search turns up nothing (the enemy calms down and forgets); interrupt it from above when
    // the target is found.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Investigate", "Stops to look, goes to the believed position and searches around it. Succeeds when nothing turns up.")]
    public sealed class Investigate : EnemyAction
    {
        [Tooltip("Seconds before giving up. 0 = EnemyData's alert duration")]
        [SerializeField, Min(0f)] private float _duration;

        public override string Summary => _duration > 0f ? $"{_duration:0.#} s" : null;

        protected override void OnEnter(BtContext context) => Ai.Search.Begin(_duration);

        protected override BtStatus OnTick(BtContext context)
        {
            Ai.ReportState(EnemyAI.AiState.Alert);
            if (!Ai.Search.Tick(context.DeltaTime)) return BtStatus.Running;

            Ai.Perception.LoseTarget();
            return BtStatus.Success;
        }

        protected override void OnAbort(BtContext context) => Ai.Search.Cancel();
    }
}
