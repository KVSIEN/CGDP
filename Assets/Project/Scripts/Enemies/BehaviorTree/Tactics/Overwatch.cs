using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Core;

namespace CGD.Enemies
{
    // Holds its ground and watches where it believes a lost target went, instead of walking
    // into its guns: a marksman covering the corner it slipped behind, a predator lying in
    // wait. Succeeds the moment the target shows itself again (so the tree can react to it)
    // or when time is up; fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Overwatch", "Stands still watching where the lost target is believed to be. Succeeds when it reappears or time is up.")]
    public sealed class Overwatch : EnemyAction
    {
        [Tooltip("Seconds to keep watching (random within the range)")]
        [SerializeField] private FloatRange _seconds = new(4f, 6f);

        [NonSerialized] private float _endsAt;

        public override string Summary => Mathf.Approximately(_seconds.Min, _seconds.Max)
            ? $"{_seconds.Min:0.#} s"
            : $"{_seconds.Min:0.#}–{_seconds.Max:0.#} s";

        protected override void OnEnter(BtContext context)
        {
            _endsAt = context.Time + _seconds.EvaluateClamped();
            if (Ai.Agent.isOnNavMesh) Ai.Agent.ResetPath();
        }

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;
            if (perception.IsTargetDetected || context.Time >= _endsAt) return BtStatus.Success;

            Ai.ReportState(EnemyAI.AiState.Hunt);
            Ai.FaceTowards(perception.TargetPosition, context.DeltaTime);
            return BtStatus.Running;
        }
    }
}
