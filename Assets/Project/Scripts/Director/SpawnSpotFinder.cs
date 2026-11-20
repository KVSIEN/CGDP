using UnityEngine;
using UnityEngine.AI;
using CGD.Core;

namespace CGD.Director
{
    // Finds spots around the player for the director to place things on: on the NavMesh, a
    // walkable path away from the player, and hidden from or in sight of them as asked. With a
    // route (IRouteSpots), it tries rooms on the asked side of the player along the level's
    // route first, and anywhere around them only when none of those spots will do.
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

        public bool TryFind(Vector3 player, FloatRange distance, int attempts, SpotVisibility visibility, out Vector3 spot) =>
            TryFind(player, distance, attempts, visibility, null, RouteSide.Ahead, 0, out spot);

        // `route` null = anywhere around the player; `rooms`: how many rooms along it, that way.
        public bool TryFind(Vector3 player, FloatRange distance, int attempts, SpotVisibility visibility,
                            IRouteSpots route, RouteSide side, int rooms, out Vector3 spot)
        {
            spot = default;
            if (!NavMesh.SamplePosition(player, out NavMeshHit playerHit, NavMeshSearch, NavMesh.AllAreas)) return false;

            if (route != null)
                for (int i = 0; i < attempts; i++)
                {
                    if (!route.TryPick(player, side, rooms, _random, out Vector3 candidate)) break;
                    float away = Vector3.Distance(Flat(player), Flat(candidate));
                    if (away >= distance.Min && away <= distance.Max && TryAccept(candidate, player, playerHit, visibility, out spot)) return true;
                }

            for (int i = 0; i < attempts; i++)
            {
                Vector2 direction = _random.InsideUnitCircle().normalized;
                if (direction == Vector2.zero) continue;

                Vector3 candidate = player + new Vector3(direction.x, 0f, direction.y) * distance.Evaluate(_random);
                if (TryAccept(candidate, player, playerHit, visibility, out spot)) return true;
            }
            return false;
        }

        // On the NavMesh, seen or hidden as asked, and with a way to the player.
        private bool TryAccept(Vector3 candidate, Vector3 player, NavMeshHit playerHit, SpotVisibility visibility, out Vector3 spot)
        {
            spot = default;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSearch, NavMesh.AllAreas)) return false;
            if (!Matches(visibility, player, hit.position)) return false;
            if (!NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete) return false;

            spot = hit.position;
            return true;
        }

        private static Vector3 Flat(Vector3 point) => new(point.x, 0f, point.z);

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
