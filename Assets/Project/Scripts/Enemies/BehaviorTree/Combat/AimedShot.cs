using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A marksman's shot: it stands still and draws a bead for a moment — reporting the Aim step,
    // so an Aim Laser on its prefab shows the line — then fires one burst. The telegraph is the
    // fairness: break its line of sight while the laser is on you and the shot never comes
    // (the node fails, and the aim starts over next time). Fails at once if its weapon isn't
    // ready or it can't see the target.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Aimed Shot", "Stands still and aims (a visible telegraph), then fires one burst. Fails if line of sight breaks while aiming.")]
    public sealed class AimedShot : EnemyAction
    {
        [Tooltip("Seconds it aims before firing — the warning the target gets")]
        [SerializeField, Min(0f)] private float _aimTime = 1f;
        [Tooltip("Seconds of broken line of sight it tolerates while aiming before giving up")]
        [SerializeField, Min(0f)] private float _sightGrace = 0.15f;

        [NonSerialized] private float _firesAt;
        [NonSerialized] private float _blindFor;
        [NonSerialized] private bool  _aiming;
        [NonSerialized] private bool  _fired;

        public override string Summary => $"Aims {_aimTime:0.##} s";

        protected override void OnEnter(BtContext context)
        {
            _blindFor = 0f;
            _aiming   = false;
            _fired    = false;
        }

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override void OnAbort(BtContext context) => Ai.RangedCombat.CancelBurst();

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            EnemyRangedCombat combat = Ai.RangedCombat;
            if (perception.Target == null) return BtStatus.Failure;
            if (_fired) return combat.IsBursting ? BtStatus.Running : BtStatus.Success;

            Vector3 target = perception.TargetPosition;
            if (!combat.HasLineOfSight(target))
            {
                _blindFor += context.DeltaTime;
                if (_blindFor > _sightGrace) return BtStatus.Failure;
            }
            else _blindFor = 0f;

            // Only starts drawing a bead with its weapon ready and the target in view, so the shot follows the warning.
            if (!_aiming)
            {
                if (!combat.CanFire || _blindFor > 0f) return BtStatus.Failure;
                _aiming  = true;
                _firesAt = context.Time + _aimTime;
            }

            if (context.Time < _firesAt)
            {
                Ai.ReportState(EnemyAI.AiState.Chase);
                Ai.ReportStep(EngageStep.Aim);
                Ai.Agent.isStopped = true;
                Ai.Motion.FaceTowards(target, context.DeltaTime);
                return BtStatus.Running;
            }

            _fired = combat.TryStartBurst();
            return _fired ? BtStatus.Running : BtStatus.Failure;
        }
    }
}
