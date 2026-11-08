using UnityEngine;
using CGD.Abilities;

namespace CGD.Enemies
{
    // How an enemy vanishes for Phase Shift: takes the best known full-height cover that no
    // hostile around it can see (EnemyTactics), and warps there. Rivals simply lose sight of
    // it and go on believing it is where they last saw it.
    internal sealed class EnemyPhaseShift : IUnseenRelocator
    {
        // The tree asks "can it?" every tick while deciding; the raycasts behind the answer run
        // at most this often.
        private const float RecheckEvery = 0.5f;

        private readonly EnemyAI _ai;

        private float _checkedAt = float.NegativeInfinity;
        private float _checkedDistance;
        private bool  _found;

        public EnemyPhaseShift(EnemyAI ai) => _ai = ai;

        public bool CanRelocateUnseen(float maxDistance)
        {
            if (Time.time - _checkedAt < RecheckEvery && Mathf.Approximately(_checkedDistance, maxDistance)) return _found;

            _checkedAt       = Time.time;
            _checkedDistance = maxDistance;
            _found = _ai.Agent.isOnNavMesh &&
                     _ai.Tactics.TryFindUnseenCover(maxDistance, _ai.Perception.HostilesAround, out _);
            return _found;
        }

        public bool TryRelocateUnseen(float maxDistance)
        {
            _checkedAt = float.NegativeInfinity;   // whatever was found before is stale now
            if (!_ai.Agent.isOnNavMesh) return false;
            if (!_ai.Tactics.TryFindUnseenCover(maxDistance, _ai.Perception.HostilesAround, out int cover)) return false;
            if (!_ai.Tactics.TryClaimCover(cover, out Vector3 spot)) return false;

            Vector3 from = _ai.transform.position;
            _ai.Agent.Warp(spot);
            _ai.Agent.ResetPath();
            _ai.NotifyRelocated(from);
            return true;
        }
    }
}
