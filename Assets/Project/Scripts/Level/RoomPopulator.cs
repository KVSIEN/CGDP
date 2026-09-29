using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Level
{
    // Fills rooms with what their RoomContentRule asks for. Props and centrepieces go in
    // before the NavMesh is built (so it walks around them), enemies after (they need it).
    // Every placement comes from the level's seed, so the same seed gives the same level.
    public class RoomPopulator
    {
        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;
        private readonly Transform _parent;
        private readonly RandomStream _random;

        // Tiles still free to spawn on, per room, in shuffled order.
        private readonly Dictionary<int, List<Vector2Int>> _freeTiles = new();

        public RoomPopulator(LevelBuildSettings settings, LevelLayout layout, Transform parent, RandomStream random)
        {
            _settings = settings;
            _layout   = layout;
            _parent   = parent;
            _random   = random;
        }

        public void PlaceProps()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                if (rule == null) continue;

                if (rule.Centerpiece != null)
                    Object.Instantiate(rule.Centerpiece, RoomCenter(room), RandomYaw(), _parent);

                if (rule.Props.Length == 0) continue;
                int count = rule.PropCount.Evaluate(_random);
                for (int i = 0; i < count && TryTakeTile(room, out Vector3 position); i++)
                    Object.Instantiate(_random.Pick(rule.Props), position, RandomYaw(), _parent);
            }
        }

        public void SpawnEnemies()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                if (rule == null || rule.Enemies.Length == 0) continue;

                int count = rule.EnemyCount.Lerp(room.Node.Intensity);
                if (count <= 0) continue;

                Transform[] route = CreatePatrolRoute(room);
                for (int i = 0; i < count && TryTakeTile(room, out Vector3 position); i++)
                {
                    GameObject enemy = PrefabPool.Spawn(_random.Pick(rule.Enemies), OnNavMesh(position), RandomYaw());
                    if (enemy.TryGetComponent(out EnemyAI ai)) ai.SetWaypoints(route);
                }
            }
        }

        public Vector3 RoomCenter(LevelRoom room) => _parent.TransformPoint(_layout.RoomCenterLocal(room));

        // A loop around the inside of the room, a spawn margin in from the walls.
        private Transform[] CreatePatrolRoute(LevelRoom room)
        {
            var holder = new GameObject($"PatrolRoute_{room.Node.Id}").transform;
            holder.SetParent(_parent, false);

            RectInt inner = Inner(room);
            Vector2Int[] corners =
            {
                new(inner.xMin, inner.yMin), new(inner.xMax - 1, inner.yMin),
                new(inner.xMax - 1, inner.yMax - 1), new(inner.xMin, inner.yMax - 1),
            };

            var route = new Transform[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                route[i] = new GameObject($"Waypoint{i}").transform;
                route[i].SetParent(holder, false);
                route[i].localPosition = _layout.TileToLocal(corners[i]);
            }
            return route;
        }

        private bool TryTakeTile(LevelRoom room, out Vector3 position)
        {
            if (!_freeTiles.TryGetValue(room.Node.Id, out List<Vector2Int> free))
            {
                free = SpawnableTiles(room);
                _random.Shuffle(free);
                _freeTiles[room.Node.Id] = free;
            }

            if (free.Count == 0)
            {
                position = default;
                return false;
            }

            Vector2Int tile = free[free.Count - 1];
            free.RemoveAt(free.Count - 1);
            position = _parent.TransformPoint(_layout.TileToLocal(tile));
            return true;
        }

        // Inside the spawn margin, away from doorways and the centrepiece spot.
        private List<Vector2Int> SpawnableTiles(LevelRoom room)
        {
            var tiles = new List<Vector2Int>();
            Vector2 centre = room.Center;
            foreach (Vector2Int tile in Inner(room).allPositionsWithin)
            {
                if (Vector2.Distance(tile + Vector2.one * 0.5f, centre) < 1.5f) continue;
                if (NearDoorway(room, tile)) continue;
                tiles.Add(tile);
            }
            return tiles;
        }

        private bool NearDoorway(LevelRoom room, Vector2Int tile)
        {
            foreach (LevelDoorway doorway in _layout.Doorways)
            {
                if (doorway.Room != room) continue;
                Vector2Int d = doorway.RoomTile - tile;
                if (Mathf.Abs(d.x) + Mathf.Abs(d.y) <= 2) return true;
            }
            return false;
        }

        private RectInt Inner(LevelRoom room)
        {
            int margin = Mathf.Min(_settings.SpawnMarginTiles, (room.Tiles.width - 1) / 2);
            return new RectInt(room.Tiles.xMin + margin, room.Tiles.yMin + margin,
                room.Tiles.width - margin * 2, room.Tiles.height - margin * 2);
        }

        private Quaternion RandomYaw() => _parent.rotation * Quaternion.Euler(0f, _random.Range(0f, 360f), 0f);

        private static Vector3 OnNavMesh(Vector3 point) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : point;
    }
}
