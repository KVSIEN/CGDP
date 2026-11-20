using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // Void's way of lying in wait: unseen in its cover, it stares at the target and now and then
    // makes a noise out beyond it, so the target turns to look the wrong way. Succeeds when the
    // target is looking away and within reach: the moment to strike (Siphon or a lunge). Fails
    // when it is given away or its patience runs out. The noise is always well clear of the
    // haunter itself, so it never gives its own spot away.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Haunt From Cover", "Lies hidden, staring, and makes noises beyond the target to turn its head. Succeeds when the target looks away within reach.")]
    public sealed class HauntFromCover : HiddenWait
    {
        [Tooltip("Metres: the target must be this close, and looking away, for it to strike")]
        [SerializeField, Min(0f)] private float _strikeRange = 8f;
        [Tooltip("Degrees either side of where the target faces that count as looking at it")]
        [SerializeField, Range(5f, 90f)] private float _watchedAngle = 35f;
        [Tooltip("Seconds between noises (random within the range)")]
        [SerializeField] private FloatRange _noiseEvery = new(2.5f, 4f);
        [Tooltip("Metres beyond the target the noise comes from")]
        [SerializeField, Min(1f)] private float _noiseBeyond = 5f;
        [Tooltip("Metres the noise carries")]
        [SerializeField, Min(1f)] private float _noiseRadius = 14f;

        [NonSerialized] private float _nextNoise;

        protected override void Begin(BtContext context) => _nextNoise = context.Time + _noiseEvery.EvaluateClamped();

        protected override BtStatus Watch(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return PatienceSpent(context) ? BtStatus.Failure : BtStatus.Running;

            Ai.Motion.KeepEyesOnTarget(context.DeltaTime);
            Vector3 self   = Ai.transform.position;
            Vector3 target = perception.TargetPosition;
            Vector3 away   = target - self;
            away.y = 0f;
            float distance = away.magnitude;

            if (perception.IsSensing && distance <= _strikeRange && !Gaze.IsLookedAt(Ai, _watchedAngle)) return BtStatus.Success;

            if (context.Time >= _nextNoise && distance > 0.01f)
            {
                _nextNoise = context.Time + _noiseEvery.EvaluateClamped();
                Noise.Emit(target + away / distance * _noiseBeyond, _noiseRadius, DamageSource.Of(Ai.gameObject));
            }
            return PatienceSpent(context) ? BtStatus.Failure : BtStatus.Running;
        }
    }
}
