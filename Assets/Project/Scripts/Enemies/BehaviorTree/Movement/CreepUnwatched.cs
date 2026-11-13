using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Comes closer only while its target isn't looking. The moment the target looks its way it
    // stops dead and stares back; look away and it is nearer every time you look again. Heads
    // for where it believes the target is, through bushes where it can. Stared at too long, it
    // can lurch forward anyway for a moment (Lurch After) — a horde can't be held off by looking
    // alone, and the jolt is the scare. Succeeds once within the stop distance; fails without a target.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Creep Unwatched", "Approaches only while the target looks away; freezes and stares back when it looks. Succeeds when close enough.")]
    public sealed class CreepUnwatched : EnemyAction
    {
        private const float LurchSpeed = 1.3f;   // share of chase speed

        [Tooltip("Metres from the target at which it has crept close enough")]
        [SerializeField, Min(0.5f)] private float _stopDistance = 10f;
        [Tooltip("Degrees either side of where the target faces that count as being looked at")]
        [SerializeField, Range(5f, 90f)] private float _lookAngle = 35f;
        [Tooltip("Speed while unwatched, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 0.9f;
        [Tooltip("Seconds of being looked at after which it lurches forward anyway. 0 = never: it waits as long as it is watched")]
        [SerializeField, Min(0f)] private float _lurchAfter;
        [Tooltip("Seconds a lurch lasts")]
        [SerializeField, Min(0.05f)] private float _lurchFor = 0.4f;

        [NonSerialized] private bool  _frozen;
        [NonSerialized] private float _frozenFor;
        [NonSerialized] private float _lurchLeft;

        public override string Summary => $"Until {_stopDistance:0.#} m";

        protected override void OnEnter(BtContext context)
        {
            _frozen    = false;
            _frozenFor = 0f;
            _lurchLeft = 0f;
        }

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
            if (_lurchLeft > 0f) _lurchLeft -= context.DeltaTime;
            else if (Gaze.IsLookedAt(Ai, _lookAngle))
            {
                _frozenFor += context.DeltaTime;
                if (_lurchAfter <= 0f || _frozenFor < _lurchAfter)
                {
                    Freeze(target, context.DeltaTime);
                    return BtStatus.Running;
                }
                _lurchLeft = _lurchFor;   // too long under the stare: it moves anyway
                _frozenFor = 0f;
            }
            else _frozenFor = 0f;

            _frozen = false;
            Ai.ReportStep(EngageStep.Approach);
            Ai.Agent.isStopped = false;
            // A lurch is a jolt, not a stroll.
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * (_lurchLeft > 0f ? Mathf.Max(_speed, LurchSpeed) : _speed));
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
