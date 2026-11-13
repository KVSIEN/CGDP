using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Comes closer only while its target isn't looking. The moment the target looks its way it
    // stops dead and stares back; look away and it is nearer every time you look again. Heads
    // for where it believes the target is, through bushes where it can. Succeeds once within
    // the stop distance; fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Creep Unwatched", "Approaches only while the target looks away; freezes and stares back when it looks. Succeeds when close enough.")]
    public sealed class CreepUnwatched : EnemyAction
    {
        [Tooltip("Metres from the target at which it has crept close enough")]
        [SerializeField, Min(0.5f)] private float _stopDistance = 10f;
        [Tooltip("Degrees either side of where the target faces that count as being looked at")]
        [SerializeField, Range(5f, 90f)] private float _lookAngle = 35f;
        [Tooltip("Speed while unwatched, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 0.9f;

        [NonSerialized] private bool _frozen;

        public override string Summary => $"Until {_stopDistance:0.#} m";

        protected override void OnEnter(BtContext context) => _frozen = false;

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;

            Vector3 target = perception.TargetPosition;
            Vector3 offset = target - Ai.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= _stopDistance * _stopDistance) return BtStatus.Success;

            Ai.ReportState(EnemyAI.AiState.Chase);   // in the fight, so its step (Approach / Freeze) shows
            if (Gaze.IsLookedAt(Ai, _lookAngle))
            {
                Freeze(target, context.DeltaTime);
                return BtStatus.Running;
            }

            _frozen = false;
            Ai.ReportStep(EngageStep.Approach);
            Ai.Agent.isStopped = false;
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            Ai.Routing.SetDestination(target, RouteStyle.ThroughBushes);
            Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
            return BtStatus.Running;
        }

        // Stops on the spot — no slowing down — and holds its stare.
        private void Freeze(Vector3 target, float deltaTime)
        {
            if (!_frozen)
            {
                _frozen = true;
                Ai.Routing.Stop();
                if (Ai.Agent.isOnNavMesh) Ai.Agent.velocity = Vector3.zero;
                Ai.Agent.isStopped = true;
            }
            Ai.ReportStep(EngageStep.Freeze);
            Ai.Motion.FaceTowards(target, deltaTime);
        }
    }
}
