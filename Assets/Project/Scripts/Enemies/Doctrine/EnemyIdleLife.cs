using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // What an enemy does with its time while nothing is going on, in its faction's manner
    // (FactionDoctrine › Idle): a routine of beats — look about in some way for a while, maybe
    // after a short walk, maybe saying something — worked through while it stands watch and at
    // stops along its route; how it walks; and how it spends the quiet after a fight. The
    // post, the Patrol node and the return step ask it; it never decides anything about a
    // fight. Without beats it does what a plain enemy does: random glances, a steady walk.
    internal sealed class EnemyIdleLife
    {
        private const float RoamArrival     = 0.6f;
        private const int   RoamAttempts    = 4;
        private const float PaceDriftSpeed  = 0.35f;
        private const float MinPaceScale    = 0.3f;
        // Glances round a body while it stands over it, either side of the way to it.
        private const float BodyGlanceArc   = 30f;
        private const float OnEdgeGlanceScale = 0.5f;

        private enum Walk { None, Going, Performing }

        private readonly EnemyAI _ai;
        private readonly float   _paceSeed = Random.value * 100f;

        // The beat in progress.
        private IdleBeat   _beat;
        private bool       _hadBeat;
        private float      _beatStart;
        private float      _beatEnd;
        private Quaternion _beatFacing;
        private float      _lookAngle;
        private float      _nextLook;
        private int        _cycleIndex = -1;

        // A beat that is a stop on its route, and one that starts with a walk from its post.
        private bool       _stopping;
        private Walk       _walk;
        private IdleBeat   _walkBeat;

        // With no beats: plain random glances.
        private float      _plainNextLook;
        private float      _plainAngle;

        // Standing over a body or the spot a search ended.
        private float      _aftermathStart;

        public EnemyIdleLife(EnemyAI ai) => _ai = ai;

        private IdleDoctrine Style => _ai.Doctrine.Idle;

        // Back to a clean slate: it was doing something else (a fight, a respawn).
        public void Cancel()
        {
            _beat     = null;
            _stopping = false;
            _walk     = Walk.None;
            _hadBeat  = false;
        }

        // --- Walking a route --------------------------------------------------------

        // Scale on its patrol speed: the faction's pace, drifting if it is the unsteady sort.
        public float PaceScale(float time)
        {
            IdleDoctrine style = Style;
            float drift = style.PaceDrift <= 0f ? 0f
                : (Mathf.PerlinNoise(_paceSeed, time * PaceDriftSpeed) * 2f - 1f) * style.PaceDrift;
            return Mathf.Max(MinPaceScale, style.PatrolSpeedScale * (1f + drift));
        }

        // It reached a waypoint: it may stop and do a beat.
        public void BeginStop(float time)
        {
            if (Random.value >= Style.WaypointStopChance) return;
            StartStop(time);
        }

        // Between waypoints it may stop dead for a beat. Rolled every tick it walks.
        public void MaybeHalt(float deltaTime, float time)
        {
            float perSecond = Style.HaltsPerMinute / 60f;
            if (perSecond <= 0f || Random.value >= perSecond * deltaTime) return;
            StartStop(time);
        }

        private void StartStop(float time)
        {
            IdleBeat beat = ChooseBeat(allowWalk: false);
            if (beat == null) return;

            StartBeat(beat, _ai.transform.rotation, time);
            _stopping = true;
            _ai.Agent.isStopped = true;
        }

        // True while it stands at a stop doing its beat. When the beat is over it sets off again.
        public bool TickStop(float deltaTime, float time)
        {
            if (!_stopping) return false;

            if (time >= _beatEnd)
            {
                _stopping = false;
                _beat     = null;
                _ai.Agent.isStopped = false;
                return false;
            }

            _ai.Agent.isStopped = true;
            PerformBeat(deltaTime, time);
            return true;
        }

        // --- Standing watch ---------------------------------------------------------

        // True while it is off on a beat that starts with a walk from its post (walking there,
        // or doing the beat once there), so the post leaves it alone. Once the beat is over
        // the post walks it home.
        public bool TickWalkingBeat(float deltaTime, float time)
        {
            NavMeshAgent agent = _ai.Agent;
            switch (_walk)
            {
                case Walk.Going:
                    if (agent.pathPending || (agent.hasPath && agent.remainingDistance > RoamArrival)) return true;
                    _walk = Walk.Performing;
                    StartBeat(_walkBeat, _ai.transform.rotation, time);
                    agent.isStopped = true;
                    return true;

                case Walk.Performing:
                    if (time >= _beatEnd)
                    {
                        _walk = Walk.None;
                        _beat = null;
                        agent.isStopped = false;
                        return false;
                    }
                    PerformBeat(deltaTime, time);
                    return true;
            }
            return false;
        }

        // At its post: works through its routine, or just glances about if it has none.
        public void TickWatch(Quaternion postFacing, Vector3 post, float deltaTime, float time)
        {
            if (Style.Beats.Length == 0)
            {
                PlainGaze(postFacing, deltaTime, time);
                return;
            }

            if (_beat == null || time >= _beatEnd)
            {
                IdleBeat next = ChooseBeat(allowWalk: true);
                if (next != null && next.WalkRadius > 0f && TryBeginWalk(post, next)) return;
                if (next == null) { PlainGaze(postFacing, deltaTime, time); return; }

                StartBeat(next, postFacing, time);
            }
            PerformBeat(deltaTime, time);
        }

        private bool TryBeginWalk(Vector3 post, IdleBeat beat)
        {
            for (int i = 0; i < RoamAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * beat.WalkRadius;
                var spot = post + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) continue;
                if (!_ai.Routing.SetDestination(hit.position)) continue;

                _ai.Agent.isStopped = false;
                _walk     = Walk.Going;
                _walkBeat = beat;
                _beat     = null;
                return true;
            }
            return false;
        }

        // --- Beats ------------------------------------------------------------------

        // The next beat: in turn, or by weight. Beats that need a walk are left out when it
        // cannot walk (it is at a stop on its route).
        private IdleBeat ChooseBeat(bool allowWalk)
        {
            IdleBeat[] beats = Style.Beats;
            if (beats.Length == 0) return null;

            if (Style.Order == IdleOrder.Cycle)
            {
                for (int i = 0; i < beats.Length; i++)
                {
                    _cycleIndex = (_cycleIndex + 1) % beats.Length;
                    if (beats[_cycleIndex] != null && (allowWalk || beats[_cycleIndex].WalkRadius <= 0f)) return beats[_cycleIndex];
                }
                return null;
            }

            float total = 0f;
            foreach (IdleBeat beat in beats)
                if (beat != null && (allowWalk || beat.WalkRadius <= 0f)) total += beat.Weight;
            if (total <= 0f) return null;

            float roll = Random.value * total;
            foreach (IdleBeat beat in beats)
            {
                if (beat == null || (!allowWalk && beat.WalkRadius > 0f)) continue;
                roll -= beat.Weight;
                if (roll <= 0f) return beat;
            }
            return null;
        }

        private void StartBeat(IdleBeat beat, Quaternion facing, float time)
        {
            float length = beat.Duration.EvaluateClamped();
            if (_ai.Perception.IsCautious) length *= Style.EdgeBeatScale;

            _beat       = beat;
            _beatStart  = time;
            _beatEnd    = time + Mathf.Max(0.1f, length);
            _beatFacing = facing;
            _nextLook   = time;
            _lookAngle  = Random.Range(-HalfArc(beat), HalfArc(beat));

            // A stare or a turned back is a jump, not a turn — but the very first beat just turns.
            if (_hadBeat && beat.Gaze is GazePattern.Fixate or GazePattern.Away)
                _ai.SnapFacing(PointAt(facing, beat.Gaze == GazePattern.Away ? 180f : _lookAngle));
            _hadBeat = true;

            if (beat.Bark != BarkKind.None) _ai.Bark(beat.Bark);
        }

        private void PerformBeat(float deltaTime, float time)
        {
            IdleBeat beat = _beat;
            float angle;
            switch (beat.Gaze)
            {
                case GazePattern.Sweep:
                    angle = SweepAngle(time + deltaTime - _beatStart, HalfArc(beat));
                    break;
                case GazePattern.Fixate:
                    angle = _lookAngle;
                    break;
                case GazePattern.Away:
                    angle = 180f;
                    break;
                case GazePattern.Hold:
                    angle = 0f;
                    break;
                default:
                    if (time >= _nextLook)
                    {
                        float interval = beat.LookInterval.Max > 0f
                            ? beat.LookInterval.EvaluateClamped()
                            : _ai.Perception.Profile.GuardLookInterval.EvaluateClamped();
                        if (_ai.Perception.IsCautious) interval *= OnEdgeGlanceScale;
                        _nextLook  = time + interval;
                        _lookAngle = Random.Range(-HalfArc(beat), HalfArc(beat));
                    }
                    angle = _lookAngle;
                    break;
            }
            _ai.FaceTowards(PointAt(_beatFacing, angle), deltaTime);
        }

        private float HalfArc(IdleBeat beat) => Mathf.Min(180f, _ai.Perception.Profile.GuardLookAngle * beat.ArcScale);

        // Starts from straight ahead and works out to one side, then the other. The caller
        // aims a tick ahead so the turn is under way before the point arrives.
        private float SweepAngle(float elapsed, float halfArc)
        {
            float speed = Style.SweepSpeed * (_ai.Perception.IsCautious ? Style.EdgeSweepScale : 1f);
            return Mathf.PingPong(elapsed * speed + halfArc, 2f * halfArc) - halfArc;
        }

        private Vector3 PointAt(Quaternion facing, float angle) =>
            _ai.transform.position + facing * Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        // A plain enemy's idle: a random glance every so often, twice as often when on edge.
        private void PlainGaze(Quaternion facing, float deltaTime, float time)
        {
            PerceptionProfile profile = _ai.Perception.Profile;
            if (time >= _plainNextLook)
            {
                float interval = profile.GuardLookInterval.EvaluateClamped();
                if (_ai.Perception.IsCautious) interval *= OnEdgeGlanceScale;
                _plainNextLook = time + interval;
                _plainAngle    = Random.Range(-profile.GuardLookAngle, profile.GuardLookAngle);
            }
            _ai.FaceTowards(PointAt(facing, _plainAngle), deltaTime);
        }

        // --- After a fight ----------------------------------------------------------

        // It starts standing over what it killed (or where a search ended).
        public void BeginAftermath(float time) => _aftermathStart = time;

        // Its eyes while it stands there, in its faction's manner: staring at the body, scanning
        // round it, or sniffing about it. Only a won fight has a manner; after a search it
        // simply looks where it was told.
        public void TickAftermath(bool victory, float deltaTime, float time)
        {
            Vector3 lookAt = _ai.ReturnLookAt;
            Vector3 toLook = lookAt - _ai.transform.position;
            toLook.y = 0f;
            GazePattern gaze = victory ? _ai.Doctrine.Aftermath.VictoryGaze : GazePattern.Hold;
            if (gaze == GazePattern.Hold || toLook.sqrMagnitude < 0.0001f)
            {
                _ai.FaceTowards(lookAt, deltaTime);
                return;
            }

            Quaternion toBody = Quaternion.LookRotation(toLook);
            float angle;
            if (gaze == GazePattern.Sweep)
            {
                float half = _ai.Perception.Profile.GuardLookAngle;
                angle = SweepAngle(time + deltaTime - _aftermathStart, half);
            }
            else
            {
                if (time >= _nextLook)
                {
                    _nextLook  = time + _ai.Perception.Profile.GuardLookInterval.EvaluateClamped() * OnEdgeGlanceScale;
                    _lookAngle = Random.Range(-BodyGlanceArc, BodyGlanceArc);
                }
                angle = _lookAngle;
            }
            _ai.FaceTowards(PointAt(toBody, angle), deltaTime);
        }
    }
}
