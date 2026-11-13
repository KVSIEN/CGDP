using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // How an enemy looks into something it noticed (the Investigate node runs it):
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
        // Share of a hunting search spent checking cover someone could be hiding behind.
        private const float HidingShare     = 0.5f;
        // Glances prefer directions that stay open this far.
        private const float OpenLookDistance = 4f;
        private const int   LookTries        = 5;

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

        // Starts looking into the current lead. timeLimit 0 = EnemyData's alert duration (longer when
        // hunting). The director's pacing stretches it at the peak and cuts it short in the calm.
        public void Begin(float timeLimit = 0f)
        {
            _elapsed   = 0f;
            _timeLimit = (timeLimit > 0f ? timeLimit : _ai.Data.AlertDuration) * (IsHunting ? HuntTimeFactor : 1f)
                       * EnemyPacing.Current.SearchScale;
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
                case Phase.Approach:   TickApproach(deltaTime);   break;
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
            _timer = fresh ? NoticeHold() * (IsHunting ? HuntNoticeShare : 1f) : RenoticePause;
            if (fresh && _ai.Doctrine.Tells.NoticeSnapsToLead) _ai.Motion.SnapFacing(_ai.Perception.LookPosition);
            _spots.Clear();
            _spotIndex = -1;
            _ai.Agent.isStopped = true;
        }

        // How long it takes something in before going to look: its faction's way, else the profile's.
        private float NoticeHold()
        {
            var hold = _ai.Doctrine.Tells.NoticeHold;
            return hold.Max > 0f ? hold.EvaluateClamped() : Profile.NoticePause.EvaluateClamped();
        }

        private void TickNotice(float deltaTime)
        {
            _ai.Motion.FaceTowards(_ai.Perception.LookPosition, deltaTime);
            _timer -= deltaTime;
            if (_timer <= 0f) GoTo(_ai.Perception.LastKnownPosition);
        }

        private void TickApproach(float deltaTime)
        {
            // Reading the route: a hold at a corner or to listen, a rush on a clear stretch.
            if (_ai.Foresight.TickApproach(deltaTime, Time.time, out float pace)) return;
            _ai.Motion.SetSpeed((IsHunting ? _ai.Data.ChaseSpeed * Profile.HuntSpeed : _ai.Data.PatrolSpeed) * pace);

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
            _ai.Motion.FaceTowards(_lookAt, deltaTime);
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
            _ai.Foresight.BeginApproach();
            _ai.Agent.isStopped = false;
            _ai.Routing.SetDestination(destination);
        }

        // A few reachable spots around the belief: when hunting, first the known cover nearby
        // that is screened from it (where someone would hide), then spots the way the target
        // was going.
        private void PlanSpots()
        {
            int count = Profile.SearchPoints.Evaluate();
            if (!IsHunting) count = Mathf.Min(count, 1);

            Vector3 center = _ai.Perception.LastKnownPosition;
            if (IsHunting)
            {
                _ai.Tactics.HidingSpots(center, Profile.SearchRadius, _spots, count * 2);
                Shuffle(_spots);
                int keep = Mathf.Min(_spots.Count, Mathf.CeilToInt(count * HidingShare));
                _spots.RemoveRange(keep, _spots.Count - keep);
            }

            for (int i = _spots.Count; i < count; i++)
            {
                Vector3 direction = SpotDirection();
                Vector3 candidate = center + direction * (Profile.SearchRadius * Random.Range(0.4f, 1f));
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) _spots.Add(hit.position);
            }
        }

        private static void Shuffle(List<Vector3> spots)
        {
            for (int i = spots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (spots[i], spots[j]) = (spots[j], spots[i]);
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

        // A direction to glance in: a turn of 50–120° either way, preferring one with open
        // space ahead so it scans the room, not the wall it happens to stand against.
        private Vector3 PickLookPoint()
        {
            Vector3 position = _ai.transform.position;
            Vector3 eye      = position + Vector3.up * 1.5f;
            Vector3 forward  = _ai.transform.forward;
            Vector3 fallback = position + forward * 5f;

            for (int i = 0; i < LookTries; i++)
            {
                float   turn      = Random.Range(50f, 120f) * (Random.value < 0.5f ? -1f : 1f);
                Vector3 direction = Quaternion.Euler(0f, turn, 0f) * forward;
                if (i == 0) fallback = position + direction * 5f;
                if (!Physics.Raycast(eye, direction, OpenLookDistance, _ai.ObstacleMask, QueryTriggerInteraction.Ignore))
                    return position + direction * 5f;
            }
            return fallback;
        }
    }
}
