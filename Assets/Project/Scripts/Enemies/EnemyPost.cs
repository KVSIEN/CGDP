using UnityEngine;

namespace CGD.Enemies
{
    // Where an enemy belongs: the spot and facing it started at, and its patrol route.
    // Guards (no route) stand here scanning their surroundings; anyone who gave up a search
    // heads back here (or to the nearest point of its route).
    internal sealed class EnemyPost
    {
        private const float AtPostDistance = 1f;

        private readonly EnemyAI _ai;
        private Vector3    _position;
        private Quaternion _rotation;
        private Quaternion _lookAt;
        private float      _nextLook;

        public EnemyPost(EnemyAI ai)
        {
            _ai = ai;
            Remember();
        }

        // Takes the current spot as its post (on spawn, or when placed by a spawner).
        public void Remember()
        {
            _position = _ai.transform.position;
            _rotation = _ai.transform.rotation;
            _lookAt   = _rotation;
            _nextLook = 0f;
        }

        // Where to head back to: the nearest point of its route, or its post.
        public Vector3 ReturnPoint()
        {
            Transform[] route = _ai.Waypoints;
            if (route.Length == 0) return _position;

            Vector3 self = _ai.transform.position;
            Vector3 best = route[0] != null ? route[0].position : _position;
            foreach (Transform waypoint in route)
                if (waypoint != null && (waypoint.position - self).sqrMagnitude < (best - self).sqrMagnitude)
                    best = waypoint.position;
            return best;
        }

        // Guard duty: walk back if it drifted off, otherwise stand and look around now and then.
        public void TickGuard(float deltaTime, float time)
        {
            var agent = _ai.Agent;
            if ((_ai.transform.position - _position).sqrMagnitude > AtPostDistance * AtPostDistance)
            {
                agent.isStopped = false;
                if (!agent.hasPath || (agent.destination - _position).sqrMagnitude > 0.25f) agent.SetDestination(_position);
                return;
            }

            agent.isStopped = true;
            PerceptionProfile profile = _ai.Perception.Profile;
            if (time >= _nextLook)
            {
                float interval = profile.GuardLookInterval.EvaluateClamped();
                if (_ai.Perception.IsCautious) interval *= 0.5f;
                _nextLook = time + interval;

                float angle = profile.GuardLookAngle;
                _lookAt = _rotation * Quaternion.Euler(0f, Random.Range(-angle, angle), 0f);
            }
            _ai.FaceTowards(_ai.transform.position + _lookAt * Vector3.forward, deltaTime);
        }

        public bool IsNear(Vector3 point, float radius) => (point - _position).sqrMagnitude <= radius * radius;

        public Vector3 Position => _position;
    }
}
