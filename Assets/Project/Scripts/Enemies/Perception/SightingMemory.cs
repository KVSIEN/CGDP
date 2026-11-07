using UnityEngine;

namespace CGD.Enemies
{
    // What an enemy remembers of its target from seeing it: where it was, which way it faced,
    // and how it was moving — standing still, running, strafing or backing away. Movement
    // comes only from sightings close together in time, and is judged against the facing,
    // because a strafing or backpedalling target tends to stop or reverse while a running one
    // keeps going. Kept free of Unity physics so the rules can be tested.
    public sealed class SightingMemory
    {
        public const float StillSpeed = 0.5f;          // m/s: slower counts as standing still

        private const float MaxSpeed      = 8f;
        private const float TrackingGap   = 0.5f;      // seconds between sightings that still give a velocity
        private const float ForwardAngle  = 45f;       // degrees off the facing that still count as forward
        private const float BackwardAngle = 135f;      // and from where it counts as backing away

        private float _seenAt = float.NegativeInfinity;
        private bool  _hasVelocity;

        public Vector3 Position { get; private set; }
        // Flat, m/s.
        public Vector3 Velocity { get; private set; }
        // Flat unit vector; zero until seen.
        public Vector3 Facing   { get; private set; }
        public TargetMotion Motion { get; private set; }

        public bool IsStationary => Motion == TargetMotion.Stationary;

        // Which way it was going (flat unit vector), or zero when standing still or unknown.
        public Vector3 Heading =>
            Motion is TargetMotion.Unknown or TargetMotion.Stationary ? Vector3.zero : Velocity.normalized;

        public void Forget()
        {
            _seenAt      = float.NegativeInfinity;
            _hasVelocity = false;
            Velocity     = Vector3.zero;
            Facing       = Vector3.zero;
            Motion       = TargetMotion.Unknown;
        }

        public void Record(Vector3 position, Vector3 forward, float time)
        {
            float gap = time - _seenAt;
            if (gap <= TrackingGap)
            {
                Vector3 velocity = (position - Position) / Mathf.Max(0.01f, gap);
                velocity.y = 0f;
                Velocity = Vector3.ClampMagnitude(_hasVelocity ? Vector3.Lerp(Velocity, velocity, 0.5f) : velocity, MaxSpeed);
                _hasVelocity = true;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f) Facing = forward.normalized;

            Position = position;
            _seenAt  = time;
            Motion   = Classify();
        }

        // Where it probably is after slipping out of sight: further along the way it was going
        // the more committed it looked (strafeShare / backpedalShare of the full guess), and
        // right where it was if it stood still.
        public Vector3 Predict(float seconds, float strafeShare, float backpedalShare)
        {
            float share = Motion switch
            {
                TargetMotion.Forward  => 1f,
                TargetMotion.Sideways => strafeShare,
                TargetMotion.Backward => backpedalShare,
                _                     => 0f,
            };
            return Position + Velocity * (seconds * share);
        }

        private TargetMotion Classify()
        {
            if (!_hasVelocity) return TargetMotion.Unknown;
            if (Velocity.magnitude < StillSpeed) return TargetMotion.Stationary;
            if (Facing == Vector3.zero) return TargetMotion.Forward;

            float angle = Vector3.Angle(Facing, Velocity);
            if (angle <= ForwardAngle)  return TargetMotion.Forward;
            if (angle >= BackwardAngle) return TargetMotion.Backward;
            return TargetMotion.Sideways;
        }
    }
}
