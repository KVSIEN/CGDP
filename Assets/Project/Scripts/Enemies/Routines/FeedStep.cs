using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // After a won fight, a faction that feeds (AftermathDoctrine › Feeding) goes to what it
    // killed and feeds on it instead of standing over it: it heals, and while its head is down
    // it notices far less — a window to slip past it or strike first. Ends once it has fed its
    // fill, the body is gone, or it can't reach it in time.
    internal sealed class FeedStep
    {
        private const float ReachDistance = 1.4f;
        private const float ReachTimeout  = 6f;

        private HealthManager _body;
        private float _feedLeft;
        private float _giveUpAt;
        private bool  _walking;
        private bool  _feeding;

        // Never its own kin (or pact) and never the player: Bio eats the other factions' dead.
        public static bool WantsToFeed(EnemyAI ai, HealthManager body) =>
            ai.Doctrine.Aftermath.Feeds && IsThere(body) && body.Team != Team.Player &&
            !EnemyRoster.IsKinTeam(ai.Data, ai.Health.Team, body.Team);

        public void Begin(HealthManager body, float feedSeconds, float time)
        {
            _body     = body;
            _feedLeft = feedSeconds;
            _giveUpAt = time + ReachTimeout;
            _walking  = false;
            _feeding  = false;
        }

        // True once it is done feeding (or gave up).
        public bool Tick(EnemyAI ai, float deltaTime, float time)
        {
            if (!IsThere(_body)) return true;

            Vector3 at = _body.transform.position;
            if (!_feeding && !Reached(ai, at))
            {
                if (time >= _giveUpAt) return true;
                if (!_walking)
                {
                    _walking = true;
                    ai.Agent.isStopped = false;
                    ai.Motion.SetSpeed(ai.Data.PatrolSpeed);
                    ai.Routing.SetDestination(at, RouteStyle.Direct);
                }
                return false;
            }

            if (!_feeding)
            {
                _feeding = true;
                ai.Agent.isStopped = true;
                ai.Perception.SetAttention(ai.Doctrine.Aftermath.FeedAttention);
            }

            AftermathDoctrine style = ai.Doctrine.Aftermath;
            ai.Motion.FaceTowards(at, deltaTime);
            ai.Health.Heal(ai.Health.MaxHealth * style.FeedHealPerSecond * deltaTime);
            _feedLeft -= deltaTime;
            return _feedLeft <= 0f;
        }

        // Head back up: it notices everything again.
        public void End(EnemyAI ai)
        {
            ai.Perception.SetAttention(1f);
            _body = null;
        }

        private static bool Reached(EnemyAI ai, Vector3 at)
        {
            Vector3 offset = at - ai.transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= ReachDistance * ReachDistance;
        }

        // A pooled body is released (inactive) some time after it dies.
        private static bool IsThere(HealthManager body) => body != null && body.IsDead && body.gameObject.activeInHierarchy;
    }
}
