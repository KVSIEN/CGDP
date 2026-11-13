using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // An enemy thinking a step ahead, in its faction's manner (FactionDoctrine › Foresight):
    //   intercept  a chaser runs to where a moving target will be, not where it is
    //   aim        a zone or a bolt is thrown ahead of a moving target, by its wind-up
    //   approach   on its way to somewhere it has not seen, it reads the route: holds at a
    //              corner to look down the next stretch, rushes a long clear one, stops to
    //              listen
    // Every lead stops at the last walkable point toward where the target is heading, so it
    // never counts on a target walking through a wall. It only ever uses what the enemy
    // senses: a target's velocity is known only while it is in sight.
    internal sealed class EnemyForesight
    {
        // A steering target that moves further than this is a new corner of the route.
        private const float CornerShift   = 0.5f;
        // Not at the destination itself: pausing at the last corner would just be arriving.
        private const float NearDestination = 1.5f;
        // After a stop it walks on for at least this long before stopping again.
        private const float MinWalkBetweenStops = 2f;
        private const float NavSampleRadius = 2f;

        private readonly EnemyAI _ai;

        private bool    _holding;
        private float   _holdUntil;
        private float   _nextStopAllowed;
        private Vector3 _holdLook;
        private Vector3 _lastSteer;
        private bool    _hasSteer;

        public EnemyForesight(EnemyAI ai) => _ai = ai;

        private ForesightDoctrine Style => _ai.Doctrine.Foresight;

        // --- Leading a moving target ------------------------------------------------

        // Where to run to meet a target moving at `velocity` (flat), `distance` metres away.
        public Vector3 Intercept(Vector3 targetPosition, Vector3 velocity, float distance)
        {
            ForesightDoctrine style = Style;
            if (style.InterceptShare <= 0f) return targetPosition;

            float seconds = distance / Mathf.Max(1f, _ai.Data.ChaseSpeed) * style.InterceptShare;
            return Lead(targetPosition, velocity, seconds);
        }

        // Where to aim an ability that takes `castTime` seconds to go off, at a target moving at `velocity`.
        public Vector3 LeadAim(Vector3 targetPosition, Vector3 velocity, float castTime)
        {
            ForesightDoctrine style = Style;
            if (style.ZoneLeadShare <= 0f) return targetPosition;
            return Lead(targetPosition, velocity, castTime * style.ZoneLeadShare);
        }

        private Vector3 Lead(Vector3 position, Vector3 velocity, float seconds)
        {
            if (seconds <= 0f || new Vector2(velocity.x, velocity.z).sqrMagnitude < 0.25f) return position;

            Vector3 led = ThrowArc.Lead(position, velocity, seconds, Style.MaxLead);
            if (!NavMesh.SamplePosition(position, out NavMeshHit from, NavSampleRadius, NavMesh.AllAreas)) return position;

            // Blocked on the way: it stops at the last walkable point, where the target would be held up.
            if (NavMesh.Raycast(from.position, led, out NavMeshHit edge, NavMesh.AllAreas)) return edge.position;
            return NavMesh.SamplePosition(led, out NavMeshHit to, NavSampleRadius, NavMesh.AllAreas) ? to.position : position;
        }

        // --- Reading the route ------------------------------------------------------

        // A fresh walk to somewhere new: forgets the last route's corners.
        public void BeginApproach()
        {
            _hasSteer = false;
            _holding  = false;
        }

        // Call every tick of an approach. True while it holds still (at a corner, or listening);
        // otherwise speedScale is how fast to walk.
        public bool TickApproach(float deltaTime, float time, out float speedScale)
        {
            speedScale = 1f;
            ForesightDoctrine style = Style;
            NavMeshAgent agent = _ai.Agent;

            if (_holding && time < _holdUntil)
            {
                agent.isStopped = true;
                _ai.Motion.FaceTowards(_holdLook, deltaTime);
                return true;
            }
            if (_holding)
            {
                _holding = false;
                agent.isStopped = false;
            }
            if (agent.pathPending || !agent.hasPath) return false;

            Vector3 steer = agent.steeringTarget;
            bool rounded = _hasSteer && FlatDistance(steer, _lastSteer) > CornerShift;
            _lastSteer = steer;
            _hasSteer  = true;

            if (time >= _nextStopAllowed)
            {
                if (rounded && style.CornerPause.Max > 0f && FlatDistance(steer, agent.destination) > NearDestination)
                {
                    Hold(time, style.CornerPause.EvaluateClamped(), steer);
                    return true;
                }
                if (style.ListenStopsPerMinute > 0f && Random.value < style.ListenStopsPerMinute / 60f * deltaTime)
                {
                    Hold(time, style.ListenDuration.EvaluateClamped(), _ai.transform.position + _ai.transform.forward * 5f);
                    return true;
                }
            }

            if (FlatDistance(_ai.transform.position, steer) >= style.StraightLength) speedScale = style.StraightPace;
            return false;
        }

        private void Hold(float time, float duration, Vector3 look)
        {
            _holding         = true;
            _holdUntil       = time + duration;
            _holdLook        = look;
            _nextStopAllowed = time + duration + MinWalkBetweenStops;
            _ai.Agent.isStopped = true;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        public void Reset()
        {
            _holding         = false;
            _hasSteer        = false;
            _nextStopAllowed = 0f;
        }
    }
}
