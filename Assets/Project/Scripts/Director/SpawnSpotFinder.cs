using UnityEngine;
using UnityEngine.AI;
using CGD.Core;

namespace CGD.Director
{
    // Finds spots around the player for the director to place things on: on the NavMesh, a
    // walkable path away from the player, and hidden from or in sight of them as asked.
    public class SpawnSpotFinder
    {
        private const float EyeHeight     = 1.6f;
        private const float NavMeshSearch = 4f;

        private readonly RandomStream _random;
        private readonly LayerMask    _obstacleMask;
        private readonly NavMeshPath  _path = new();

        public SpawnSpotFinder(RandomStream random, LayerMask obstacleMask)
        {
            _random       = random;
            _obstacleMask = obstacleMask;
        }

        public bool TryFind(Vector3 player, FloatRange distance, int attempts, SpotVisibility visibility, out Vector3 spot)
        {
            spot = default;
            if (!NavMesh.SamplePosition(player, out NavMeshHit playerHit, NavMeshSearch, NavMesh.AllAreas)) return false;

            for (int i = 0; i < attempts; i++)
            {
                Vector2 direction = _random.InsideUnitCircle().normalized;
                if (direction == Vector2.zero) continue;

                Vector3 candidate = player + new Vector3(direction.x, 0f, direction.y) * distance.Evaluate(_random);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSearch, NavMesh.AllAreas)) continue;
                if (!Matches(visibility, player, hit.position)) continue;
                if (!NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete) continue;

                spot = hit.position;
                return true;
            }
            return false;
        }

        // A spot within `radius` of `spot`, still on the NavMesh — for spreading out a group.
        public Vector3 Scatter(Vector3 spot, float radius)
        {
            Vector2 offset = _random.InsideUnitCircle() * radius;
            Vector3 position = spot + new Vector3(offset.x, 0f, offset.y);
            return NavMesh.SamplePosition(position, out NavMeshHit hit, radius, NavMesh.AllAreas) ? hit.position : spot;
        }

        private bool Matches(SpotVisibility visibility, Vector3 player, Vector3 position) => visibility switch
        {
            SpotVisibility.Hidden  => !IsVisible(player, position),
            SpotVisibility.InSight => IsVisible(player, position),
            _                      => true,
        };

        private bool IsVisible(Vector3 player, Vector3 position) =>
            !Physics.Linecast(player + Vector3.up * EyeHeight, position + Vector3.up * EyeHeight,
                              _obstacleMask, QueryTriggerInteraction.Ignore);
    }
}
