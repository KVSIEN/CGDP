using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;

namespace CGD.Enemies
{
    // How an enemy vanishes for Phase Shift: takes the best known full-height cover that no
    // hostile around it can see (EnemyTactics), and warps there. Rivals simply lose sight of
    // it and go on believing it is where they last saw it. Also how a burrower surfaces: next
    // to where it believes its target is (TryRelocateNearTarget).
    internal sealed class EnemyPhaseShift : IUnseenRelocator
    {
        // The tree asks "can it?" every tick while deciding; the raycasts behind the answer run
        // at most this often.
        private const float RecheckEvery = 0.5f;
        private const int   SurfaceAttempts = 6;

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
            _ai.Routing.Stop();
            _ai.NotifyRelocated(from);
            return true;
        }

        public bool TryRelocateNearTarget(float maxDistance, float within)
        {
            EnemyPerception perception = _ai.Perception;
            if (perception.Target == null || !_ai.Agent.isOnNavMesh) return false;

            Vector3 from   = _ai.transform.position;
            Vector3 target = perception.TargetPosition;
            if ((target - from).sqrMagnitude > maxDistance * maxDistance) return false;

            for (int attempt = 0; attempt < SurfaceAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(within * 0.5f, within);
                Vector3 option = target + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(option, out NavMeshHit hit, within, NavMesh.AllAreas)) continue;

                _ai.Agent.Warp(hit.position);
                _ai.Routing.Stop();
                _ai.Motion.SnapFacing(target);
                _ai.NotifyRelocated(from);
                return true;
            }
            return false;
        }
    }
}
