using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // Going over low cover: where the NavMesh has a link across a low wall or barrier (a
    // generated level's VaultLinks), the agent stops gliding and hops it instead — up and over
    // in an arc, a little slower than it walks, then on along its path. The agent's own link
    // traversal is turned off so this is the only way across.
    internal sealed class EnemyVault
    {
        // How high above the line between the two ends the hop peaks, and its time limits.
        private const float Rise        = 0.7f;
        private const float MinDuration = 0.35f;
        private const float MaxDuration = 0.9f;
        // Slower than walking the same distance.
        private const float PaceShare = 0.75f;

        private readonly NavMeshAgent _agent;
        private readonly Transform    _self;

        private bool    _vaulting;
        private Vector3 _from, _to;
        private float   _elapsed, _duration;

        public EnemyVault(NavMeshAgent agent, Transform self)
        {
            _agent = agent;
            _self  = self;
            _agent.autoTraverseOffMeshLink = false;
        }

        public bool IsVaulting => _vaulting;

        public void Tick(float deltaTime)
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;
            if (!_vaulting)
            {
                if (_agent.isOnOffMeshLink) Begin();
                return;
            }

            // Warped off the link (respawned from the pool, knocked back): the hop is over.
            if (!_agent.isOnOffMeshLink)
            {
                _vaulting = false;
                return;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            _self.position = Arc(_from, _to, t, Rise);
            if (t < 1f) return;

            _vaulting = false;
            _agent.CompleteOffMeshLink();
        }

        private void Begin()
        {
            OffMeshLinkData link = _agent.currentOffMeshLinkData;
            _from     = _self.position;
            _to       = link.endPos + Vector3.up * _agent.baseOffset;
            _elapsed  = 0f;
            float pace = Mathf.Max(1f, _agent.speed * PaceShare);
            _duration = Mathf.Clamp(Vector3.Distance(_from, _to) / pace, MinDuration, MaxDuration);
            _vaulting = true;
        }

        // A point `t` of the way along the hop: straight between the ends, lifted in a parabola
        // that peaks `rise` above the middle.
        internal static Vector3 Arc(Vector3 from, Vector3 to, float t, float rise) =>
            Vector3.Lerp(from, to, t) + Vector3.up * (rise * 4f * t * (1f - t));
    }
}
