using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Level
{
    // Furnishes rooms from their function (RoomPropPlanner), then fills them with what their
    // RoomContentRule asks for, on tiles the room's structure leaves free: never on pillars,
    // furniture, in doorways or on the centre spot, and props also keep off the walkways so
    // routes through the room stay clear. Props and centrepieces go in
    // before the NavMesh is built (so it walks around them), enemies after (they need it).
    // Every placement comes from the level's seed, so the same seed gives the same level.
    public class RoomPopulator
    {
        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;
        private readonly Transform _parent;
        private readonly RandomStream _random;

        private const RoomTileTags NeverSpawnOn = RoomTileTags.Structure | RoomTileTags.Prop | RoomTileTags.NearDoor | RoomTileTags.Centre;
        private const RoomTileTags KeepPropsOff = RoomTileTags.Walkway;

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
                foreach (PlannedProp prop in RoomPropPlanner.Plan(room, _random))
                {
                    Vector3 local = new Vector3(prop.Position.x, 0f, prop.Position.y) * _layout.TileSize;
                    Object.Instantiate(prop.Prefab, _parent.TransformPoint(local), _parent.rotation * Quaternion.Euler(0f, prop.Yaw, 0f), _parent);
                }

                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                if (rule == null) continue;

                if (rule.Centerpiece != null)
                    Object.Instantiate(rule.Centerpiece, RoomCenter(room), RandomYaw(), _parent);

                if (rule.Props.Length == 0) continue;
                int count = rule.PropCount.Evaluate(_random);
                for (int i = 0; i < count && TryTakeTile(room, KeepPropsOff, out Vector3 position); i++)
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
                for (int i = 0; i < count && TryTakeTile(room, RoomTileTags.None, out Vector3 position); i++)
                {
                    GameObject enemy = PrefabPool.Spawn(_random.Pick(rule.Enemies), OnNavMesh(position), RandomYaw());
                    if (enemy.TryGetComponent(out EnemyAI ai)) ai.SetWaypoints(route);
                }
            }
        }

        public Vector3 RoomCenter(LevelRoom room) => _parent.TransformPoint(_layout.RoomCenterLocal(room));

        // A loop through the room's outermost reachable spots (north-east, south-east,
        // south-west, north-west), a spawn margin in from the walls. For a cross or T these
        // are the arms, for a ring its corners.
        private Transform[] CreatePatrolRoute(LevelRoom room)
        {
            var holder = new GameObject($"PatrolRoute_{room.Node.Id}").transform;
            holder.SetParent(_parent, false);

            List<Vector2Int> inner = InnerTiles(room);
            Vector2Int[] corners =
            {
                Extreme(inner, 1, 1), Extreme(inner, 1, -1), Extreme(inner, -1, -1), Extreme(inner, -1, 1),
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

        private static Vector2Int Extreme(List<Vector2Int> tiles, int x, int y)
        {
            Vector2Int best = tiles[0];
            foreach (Vector2Int tile in tiles)
                if (tile.x * x + tile.y * y > best.x * x + best.y * y) best = tile;
            return best;
        }

        // Takes the last free tile without any of the `avoid` tags.
        private bool TryTakeTile(LevelRoom room, RoomTileTags avoid, out Vector3 position)
        {
            if (!_freeTiles.TryGetValue(room.Node.Id, out List<Vector2Int> free))
            {
                free = SpawnableTiles(room);
                _random.Shuffle(free);
                _freeTiles[room.Node.Id] = free;
            }

            for (int i = free.Count - 1; i >= 0; i--)
            {
                Vector2Int tile = free[i];
                if (room.Structure != null && room.Structure.Has(tile, avoid)) continue;

                free.RemoveAt(i);
                position = _parent.TransformPoint(_layout.TileToLocal(tile));
                return true;
            }

            position = default;
            return false;
        }

        // Inside the spawn margin, off pillars, doorways and the centre spot.
        private List<Vector2Int> SpawnableTiles(LevelRoom room)
        {
            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in InnerTiles(room))
                if (room.Structure == null || !room.Structure.Has(tile, NeverSpawnOn))
                    tiles.Add(tile);
            return tiles;
        }

        // Floor tiles at least the spawn margin away from every wall — or, in a room too
        // narrow for that, as far from the walls as it allows.
        private List<Vector2Int> InnerTiles(LevelRoom room)
        {
            var tiles = new List<Vector2Int>();
            for (int margin = _settings.SpawnMarginTiles; margin >= 0 && tiles.Count == 0; margin--)
                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (room.Footprint.IsInterior(tile, margin)) tiles.Add(tile);
            return tiles;
        }

        private Quaternion RandomYaw() => _parent.rotation * Quaternion.Euler(0f, _random.Range(0f, 360f), 0f);

        private static Vector3 OnNavMesh(Vector3 point) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : point;
    }
}
