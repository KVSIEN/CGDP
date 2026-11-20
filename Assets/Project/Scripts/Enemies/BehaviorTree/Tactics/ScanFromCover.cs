using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Tech's way of lying in wait: from cover it sweeps its gaze back and forth across where it
    // believes the target is, taking in what it can see. If it picked the target out at any point
    // it passes the sighting on to its kin (a callout, by its social style) and succeeds — it
    // knows where everyone is, so now it can pick its move (Flank). Fails when it saw nothing, or
    // when it is given away.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Scan From Cover", "Lies hidden, sweeping its gaze over the target's area. Succeeds (and tells its kin what it saw) if it sensed the target; fails if not or given away.")]
    public sealed class ScanFromCover : HiddenWait
    {
        private const float LookDistance = 10f;

        [Tooltip("Degrees either side of the target's direction the gaze sweeps")]
        [SerializeField, Range(10f, 120f)] private float _sweepAngle = 65f;
        [Tooltip("Seconds for one sweep, side to side and back")]
        [SerializeField, Min(0.5f)] private float _sweepPeriod = 2.8f;

        [NonSerialized] private bool _sawTarget;

        protected override void Begin(BtContext context) => _sawTarget = false;

        protected override BtStatus Watch(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            Vector3 self = Ai.transform.position;
            Vector3 toward = perception.LookPosition - self;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.01f) toward = Ai.transform.forward;

            float angle = Mathf.Sin(Elapsed(context) / _sweepPeriod * Mathf.PI * 2f) * _sweepAngle;
            Vector3 look = Quaternion.AngleAxis(angle, Vector3.up) * toward.normalized;
            Ai.Motion.FaceTowards(self + look * LookDistance, context.DeltaTime);

            if (perception.IsSensing) _sawTarget = true;
            if (!PatienceSpent(context)) return BtStatus.Running;
            if (!_sawTarget) return BtStatus.Failure;

            perception.ShareSighting();
            return BtStatus.Success;
        }
    }
}
