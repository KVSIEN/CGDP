using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Level
{
    // Brings enemies into a room mid-encounter: on spots set aside when the level was
    // built, from the room's faction roster at the room's tier odds (or, for a room held by
    // two realities, the second one), patrolling the room's route.
    public class EncounterSpawner
    {
        private const float NavMeshSearch = 3f;

        private readonly IReadOnlyList<Vector3> _spots;
        private readonly RoomRoster   _roster;
        private readonly RoomRoster   _secondRoster;
        private readonly Transform[]  _route;
        private readonly RandomStream _random;

        public EncounterSpawner(IReadOnlyList<Vector3> spots, RoomRoster roster, RoomRoster secondRoster, Transform[] route, RandomStream random)
        {
            _spots        = spots;
            _roster       = roster;
            _secondRoster = secondRoster != null && !secondRoster.IsEmpty ? secondRoster : roster;
            _route        = route;
            _random       = random;
        }

        public bool CanSpawn => _spots.Count > 0 && !_roster.IsEmpty;

        public List<HealthManager> Spawn(int count, bool fromSecondRoster = false)
        {
            var spawned = new List<HealthManager>();
            if (!CanSpawn) return spawned;

            RoomRoster roster = fromSecondRoster ? _secondRoster : _roster;
            int start = _random.Range(0, _spots.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 spot = _spots[(start + i) % _spots.Count];
                if (NavMesh.SamplePosition(spot, out NavMeshHit hit, NavMeshSearch, NavMesh.AllAreas)) spot = hit.position;

                GameObject prefab = roster.Pick(_random);
                if (prefab == null) continue;
                GameObject enemy = PrefabPool.Spawn(prefab, spot, Quaternion.Euler(0f, _random.Range(0, 360), 0f));
                if (enemy.TryGetComponent(out EnemyAI ai)) ai.SetWaypoints(_route);
                if (enemy.TryGetComponent(out HealthManager health)) spawned.Add(health);
            }
            return spawned;
        }
    }
}
