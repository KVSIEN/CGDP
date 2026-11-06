using System.Collections.Generic;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Director
{
    // The enemies around a point, found by their colliders and listed once each however many
    // hitboxes they have. For the director's occasional questions (who could probe, who could
    // shout) — not for per-frame use. Reuses its buffers, so asking allocates nothing.
    internal sealed class NearbyEnemies
    {
        private readonly Collider[]    _hits;
        private readonly List<EnemyAI> _found = new();

        public NearbyEnemies(int maxColliders = 64) => _hits = new Collider[maxColliders];

        // Active, living enemies within the radius. The list is reused by the next call.
        public IReadOnlyList<EnemyAI> Around(Vector3 center, float radius, LayerMask mask)
        {
            _found.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, _hits, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                EnemyAI enemy = _hits[i].GetComponentInParent<EnemyAI>();
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.State == EnemyAI.AiState.Dead) continue;
                if (!_found.Contains(enemy)) _found.Add(enemy);
            }
            return _found;
        }
    }
}
