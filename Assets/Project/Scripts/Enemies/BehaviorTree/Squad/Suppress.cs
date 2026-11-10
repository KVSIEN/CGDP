using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Fires one burst at where it believes the target is, whether or not it can see it —
    // shots that keep a target's head down behind cover while squadmates flank. Succeeds when
    // the burst is out. Ranged enemies only; respects how many may shoot at once. Suppressing
    // pins a target that just ducked out of sight: once it has been unseen for longer than
    // Max Blind Time it may well have moved on, so it stops pouring fire into an empty spot.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Suppress", "Fires a burst at the (believed) target position, seen or not, to pin it down. Fails once it has been unseen too long.")]
    public sealed class Suppress : EnemyAction
    {
        [Tooltip("Seconds after losing sight of the target it still fires at where it was")]
        [SerializeField, Min(0f)] private float _maxBlindTime = 4f;

        [NonSerialized] private bool _fired;

        public override string Summary => $"Up to {_maxBlindTime:0.#} s unseen";

        protected override void OnEnter(BtContext context) => _fired = false;

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;
            // A burst already going finishes; a new one needs a recent sighting.
            if (!_fired && perception.TimeSinceSensed > _maxBlindTime) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Suppress);
            Ai.Agent.isStopped = true;
            Ai.FaceTowards(perception.TargetPosition, context.DeltaTime);

            var combat = Ai.RangedCombat;
            // Pinning a target down means hitting its cover, not the wall in front of the shooter.
            if (!_fired && !combat.HasClearShot(perception.TargetPosition + Vector3.up)) return BtStatus.Failure;
            if (!_fired) _fired = combat.TryStartBurst();
            return _fired && !combat.IsBursting ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.RangedCombat.CancelBurst();

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
