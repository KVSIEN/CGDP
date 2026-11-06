using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // How an enemy looks into something it noticed, shared by the state machine (AlertState)
    // and behavior trees (Investigate):
    //   notice      stop and stare at what caught its attention (briefly when already hunting)
    //   approach    go to where it believes the thing is — the guess, not the truth
    //   look around stand and scan, then check a few spots nearby, leaning the way a lost
    //               target seemed to be heading; merely curious enemies check fewer
    // Something new on the way (another noise, a glimpse) makes it glance over and re-route.
    // Every pause, spot and count is randomised within the PerceptionProfile's ranges, so two
    // searches never play out the same.
    internal sealed class EnemySearch
    {
        private const float ArrivalDistance = 0.8f;
        private const float RenoticePause   = 0.2f;
        private const float HuntNoticeShare = 0.35f;
        private const float LeanAngle       = 60f;
        private const float LeanChance      = 0.65f;
        // An enemy that knows someone is about keeps at it longer than one that's merely curious.
        private const float HuntTimeFactor  = 2.5f;

        private enum Phase { Notice, Approach, LookAround, Done }

        private readonly EnemyAI       _ai;
        private readonly List<Vector3> _spots = new();

        private Phase   _phase = Phase.Done;
        private float   _timer;
        private float   _elapsed;
        private float   _timeLimit;
        private int     _spotIndex;
        private bool    _secondGlance;
        private Vector3 _lookAt;

        public EnemySearch(EnemyAI ai) => _ai = ai;

        public bool IsActive => _phase != Phase.Done;

        // Starts looking into the current lead. timeLimit 0 = EnemyData's alert duration (longer when hunting).
        public void Begin(float timeLimit = 0f)
        {
            _elapsed   = 0f;
            _timeLimit = (timeLimit > 0f ? timeLimit : _ai.Data.AlertDuration) * (IsHunting ? HuntTimeFactor : 1f);
            _ai.Perception.ConsumeLead();
            StartNotice(fresh: true);
        }

        // True once the search is over — found nothing, or ran out of time.
        public bool Tick(float deltaTime)
        {
            if (_phase == Phase.Done) return true;

            _elapsed += deltaTime;
            if (_ai.Perception.ConsumeLead()) StartNotice(fresh: false);
            if (_elapsed >= _timeLimit) return Finish();

            switch (_phase)
            {
                case Phase.Notice:     TickNotice(deltaTime);     break;
                case Phase.Approach:   TickApproach();            break;
                case Phase.LookAround: TickLookAround(deltaTime); break;
            }
            return _phase == Phase.Done;
        }

        public void Cancel()
        {
            if (_phase == Phase.Done) return;
            _phase = Phase.Done;
            _ai.Agent.isStopped = false;
        }

        private PerceptionProfile Profile => _ai.Perception.Profile;
        private bool IsHunting => _ai.Perception.Level == AwarenessLevel.Searching;

        // --- Phases -----------------------------------------------------------------

        private void StartNotice(bool fresh)
        {
            _phase = Phase.Notice;
            _timer = fresh ? Profile.NoticePause.EvaluateClamped() * (IsHunting ? HuntNoticeShare : 1f) : RenoticePause;
            _spots.Clear();
            _spotIndex = -1;
            _ai.Agent.isStopped = true;
        }

        private void TickNotice(float deltaTime)
        {
            _ai.FaceTowards(_ai.Perception.LookPosition, deltaTime);
            _timer -= deltaTime;
            if (_timer <= 0f) GoTo(_ai.Perception.LastKnownPosition);
        }

        private void TickApproach()
        {
            _ai.SetSpeed(IsHunting ? _ai.Data.ChaseSpeed * Profile.HuntSpeed : _ai.Data.PatrolSpeed);

            NavMeshAgent agent = _ai.Agent;
            if (agent.pathPending) return;

            bool arrived     = agent.hasPath && agent.remainingDistance <= ArrivalDistance;
            bool unreachable = !agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid;
            if (arrived || unreachable) StartLookAround();
        }

        private void StartLookAround()
        {
            _phase        = Phase.LookAround;
            _timer        = Profile.LookAroundPause.EvaluateClamped();
            _secondGlance = false;
            _lookAt       = PickLookPoint();
            _ai.Agent.isStopped = true;
        }

        private void TickLookAround(float deltaTime)
        {
            _ai.FaceTowards(_lookAt, deltaTime);
            _timer -= deltaTime;

            // Scan: one glance, then another the other way.
            if (!_secondGlance && _timer <= Profile.LookAroundPause.Min * 0.5f)
            {
                _secondGlance = true;
                _lookAt = PickLookPoint();
            }
            if (_timer > 0f) return;

            if (_spotIndex < 0) PlanSpots();
            _spotIndex++;
            if (_spotIndex >= _spots.Count) Finish();
            else GoTo(_spots[_spotIndex]);
        }

        private bool Finish()
        {
            _phase = Phase.Done;
            _ai.Agent.isStopped = false;
            return true;
        }

        // --- Where to go and look ---------------------------------------------------

        private void GoTo(Vector3 point)
        {
            Vector3 destination = NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : point;
            _phase = Phase.Approach;
            _ai.Agent.isStopped = false;
            _ai.Agent.SetDestination(destination);
        }

        // A few reachable spots around the belief, most of them the way the target was going.
        private void PlanSpots()
        {
            int count = Profile.SearchPoints.Evaluate();
            if (!IsHunting) count = Mathf.Min(count, 1);

            Vector3 center = _ai.Perception.LastKnownPosition;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = SpotDirection();
                Vector3 candidate = center + direction * (Profile.SearchRadius * Random.Range(0.4f, 1f));
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) _spots.Add(hit.position);
            }
        }

        private Vector3 SpotDirection()
        {
            Vector3 lean = _ai.Perception.SearchDirection;
            if (lean != Vector3.zero && Random.value < LeanChance)
                return Quaternion.Euler(0f, Random.Range(-LeanAngle, LeanAngle), 0f) * lean;

            Vector2 random = Random.insideUnitCircle.normalized;
            return new Vector3(random.x, 0f, random.y);
        }

        private Vector3 PickLookPoint()
        {
            Vector3 forward = _ai.transform.forward;
            float   turn    = Random.Range(50f, 120f) * (Random.value < 0.5f ? -1f : 1f);
            return _ai.transform.position + Quaternion.Euler(0f, turn, 0f) * forward * 5f;
        }
    }
}
