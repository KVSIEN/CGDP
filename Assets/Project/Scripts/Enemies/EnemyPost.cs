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

        // Guard duty: walk back if it drifted off, otherwise stand and look around now and then
        // (how, and whether it wanders a little, is its faction's idle manner).
        public void TickGuard(float deltaTime, float time)
        {
            if (_ai.Idle.TickRoam(_position, deltaTime, time)) return;

            var agent = _ai.Agent;
            if ((_ai.transform.position - _position).sqrMagnitude > AtPostDistance * AtPostDistance)
            {
                agent.isStopped = false;
                if (!agent.hasPath || (agent.destination - _position).sqrMagnitude > 0.25f) _ai.Routing.SetDestination(_position);
                return;
            }

            agent.isStopped = true;
            _ai.Idle.Gaze(_rotation, deltaTime, time);
        }

        public bool IsNear(Vector3 point, float radius) => (point - _position).sqrMagnitude <= radius * radius;

        public Vector3 Position => _position;
    }
}
