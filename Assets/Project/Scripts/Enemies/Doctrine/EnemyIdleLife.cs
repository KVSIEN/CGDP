using UnityEngine;
using UnityEngine.AI;
using CGD.Core;

namespace CGD.Enemies
{
    // What an enemy does with its time while nothing is going on, in its faction's manner
    // (FactionDoctrine › Idle): where its eyes go, how it walks its route and stops along it,
    // whether it wanders off its post, and what it mutters. The post and the Patrol node ask
    // it; it never decides anything about a fight. A plain doctrine gives the old behavior:
    // random glances, a steady walk, no stops.
    internal sealed class EnemyIdleLife
    {
        // A gaze that has not been asked for this long starts afresh (a new stop, a new watch).
        private const float GazeGap          = 1f;
        private const float RoamArrival      = 0.6f;
        private const int   RoamAttempts     = 4;
        private const float PaceDriftSpeed   = 0.35f;
        private const float MinPaceScale     = 0.3f;
        private static readonly FloatRange RoamLinger = new(1f, 2.5f);

        private enum Roam { None, Walking, Lingering }

        private readonly EnemyAI _ai;
        private readonly float   _paceSeed = Random.value * 100f;

        // Gaze.
        private float      _lastGazeAt = float.NegativeInfinity;
        private float      _gazeStart;
        private float      _nextLook;
        private float      _lookAngle;

        // Stops along its route (waypoint dwells and halts).
        private bool       _pausing;
        private float      _pauseUntil;
        private Quaternion _pauseFacing;

        // Wandering off its post.
        private Roam       _roam;
        private float      _nextRoam = float.NegativeInfinity;
        private float      _roamLingerUntil;
        private Quaternion _roamFacing;

        private float      _nextMutter = float.NegativeInfinity;

        public EnemyIdleLife(EnemyAI ai) => _ai = ai;

        private IdleDoctrine Style => _ai.Doctrine.Idle;

        // Back to a clean slate: it was doing something else (a fight, a respawn).
        public void Cancel()
        {
            _pausing    = false;
            _roam       = Roam.None;
            _lastGazeAt = float.NegativeInfinity;
            _nextRoam   = float.NegativeInfinity;
        }

        // --- Eyes -------------------------------------------------------------------

        // Looks about, centred on baseFacing, in the faction's way. Call every tick while idling.
        public void Gaze(Quaternion baseFacing, float deltaTime, float time)
        {
            IdleDoctrine style   = Style;
            PerceptionProfile profile = _ai.Perception.Profile;
            float halfArc = Mathf.Min(180f, profile.GuardLookAngle * style.LookAngleScale);

            if (time - _lastGazeAt > GazeGap)
            {
                _gazeStart = time;
                _nextLook  = time;
                _lookAngle = 0f;
            }
            _lastGazeAt = time;

            float angle;
            if (style.Gaze == GazePattern.Sweep)
            {
                // Starts from straight ahead and works out to one side, then the other. Aims a
                // tick ahead so the turn is under way before the point arrives.
                float sweep = (time + deltaTime - _gazeStart) * style.SweepSpeed;
                angle = Mathf.PingPong(sweep + halfArc, 2f * halfArc) - halfArc;
            }
            else
            {
                if (time >= _nextLook)
                {
                    float interval = profile.GuardLookInterval.EvaluateClamped() * style.LookIntervalScale;
                    if (_ai.Perception.IsCautious) interval *= 0.5f;
                    _nextLook  = time + interval;
                    _lookAngle = Random.Range(-halfArc, halfArc);
                    // The first look of a watch turns normally; after that a fixed stare jumps.
                    if (style.Gaze == GazePattern.Fixate && time > _gazeStart)
                        _ai.SnapFacing(PointAt(baseFacing, _lookAngle));
                }
                angle = _lookAngle;
            }
            _ai.FaceTowards(PointAt(baseFacing, angle), deltaTime);
        }

        private Vector3 PointAt(Quaternion baseFacing, float angle) =>
            _ai.transform.position + baseFacing * Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        // --- Walking a route --------------------------------------------------------

        // Scale on its patrol speed: the faction's pace, drifting if it is the unsteady sort.
        public float PaceScale(float time)
        {
            IdleDoctrine style = Style;
            float drift = style.PaceDrift <= 0f ? 0f
                : (Mathf.PerlinNoise(_paceSeed, time * PaceDriftSpeed) * 2f - 1f) * style.PaceDrift;
            return Mathf.Max(MinPaceScale, style.PatrolSpeedScale * (1f + drift));
        }

        // It reached a waypoint: it may wait there, looking about.
        public void BeginDwell(float time)
        {
            float dwell = Style.WaypointDwell.Max > 0f ? Style.WaypointDwell.EvaluateClamped() : 0f;
            if (dwell > 0f) BeginPause(time, dwell);
        }

        // Between waypoints it may stop dead for a while. Rolled every tick it walks.
        public void MaybeHalt(float deltaTime, float time)
        {
            float perSecond = Style.HaltsPerMinute / 60f;
            if (perSecond <= 0f || Random.value >= perSecond * deltaTime) return;
            BeginPause(time, Style.HaltDuration.EvaluateClamped());
        }

        // True while it stands paused (a dwell or a halt): it holds still and looks about. When
        // the pause is over it sets off again and this returns false.
        public bool TickPause(float deltaTime, float time)
        {
            if (!_pausing) return false;

            if (time >= _pauseUntil)
            {
                _pausing = false;
                _ai.Agent.isStopped = false;
                return false;
            }

            _ai.Agent.isStopped = true;
            Gaze(_pauseFacing, deltaTime, time);
            return true;
        }

        private void BeginPause(float time, float duration)
        {
            _pausing     = true;
            _pauseUntil  = time + duration;
            _pauseFacing = _ai.transform.rotation;
            _ai.Agent.isStopped = true;
        }

        // --- Standing watch ---------------------------------------------------------

        // True while it is off wandering from its post (or lingering where it wandered), so the
        // post leaves it alone; once that is over the post walks it home.
        public bool TickRoam(Vector3 post, float deltaTime, float time)
        {
            IdleDoctrine style = Style;
            if (style.RoamRadius <= 0f) return false;

            NavMeshAgent agent = _ai.Agent;
            switch (_roam)
            {
                case Roam.Walking:
                    if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= RoamArrival))
                    {
                        _roam            = Roam.Lingering;
                        _roamLingerUntil = time + RoamLinger.EvaluateClamped();
                        _roamFacing      = _ai.transform.rotation;
                        agent.isStopped  = true;
                    }
                    return true;

                case Roam.Lingering:
                    if (time >= _roamLingerUntil)
                    {
                        _roam            = Roam.None;
                        _nextRoam        = time + style.RoamEvery.EvaluateClamped();
                        agent.isStopped  = false;
                        return false;
                    }
                    Gaze(_roamFacing, deltaTime, time);
                    return true;
            }

            if (_nextRoam == float.NegativeInfinity) _nextRoam = time + style.RoamEvery.EvaluateClamped();
            if (time < _nextRoam) return false;

            _nextRoam = time + style.RoamEvery.EvaluateClamped();
            for (int i = 0; i < RoamAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * style.RoamRadius;
                var spot = post + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) continue;
                if (!_ai.Routing.SetDestination(hit.position)) continue;

                agent.isStopped = false;
                _roam = Roam.Walking;
                return true;
            }
            return false;
        }

        // --- Voice ------------------------------------------------------------------

        // Mutters to itself every so often while it has nothing on its mind.
        public void Mutter(float time)
        {
            var every = Style.MutterEvery;
            if (every.Max <= 0f) return;

            if (_nextMutter == float.NegativeInfinity) _nextMutter = time + every.EvaluateClamped();
            if (time < _nextMutter) return;

            _nextMutter = time + every.EvaluateClamped();
            _ai.Bark(BarkKind.Ambient);
        }
    }
}
