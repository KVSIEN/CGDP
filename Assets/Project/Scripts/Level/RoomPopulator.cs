using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Enemies;
using CGD.Factions;
using CGD.Interaction;
using CGD.Map;

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

        // What opens each Locked connection, in the rooms the map graph chose: one key for a
        // Keycard lock, and for a Terminal lock a terminal per room, bound to the gate's
        // ConditionLock (`gates`: the door placed on each connection).
        public void PlaceKeys(MapGraph graph, IReadOnlyDictionary<MapConnection, GameObject> gates)
        {
            foreach (MapConnection connection in graph.Connections)
            {
                if (connection.Type != ConnectionType.Locked || !connection.HasKey) continue;

                if (connection.Lock == MapLockKind.Terminals) PlaceTerminals(connection, gates);
                else                                          PlaceKey(connection);
            }
        }

        private void PlaceKey(MapConnection connection)
        {
            if (_settings.KeyPrefab == null) return;
            if (!_layout.Rooms.TryGetValue(connection.KeyNodeIds[0], out LevelRoom room)) return;

            if (TryTakeTile(room, KeepPropsOff, out Vector3 position))
                Object.Instantiate(_settings.KeyPrefab, position, RandomYaw(), _parent);
        }

        private void PlaceTerminals(MapConnection connection, IReadOnlyDictionary<MapConnection, GameObject> gates)
        {
            string gate = $"Terminal lock #{connection.A}–#{connection.B}";
            if (_settings.TerminalPrefab == null)
            {
                _layout.Warnings.Add($"{gate} has no terminals: the build settings have no terminal prefab.");
                return;
            }

            ConditionLock conditionLock = gates.TryGetValue(connection, out GameObject door) && door != null
                ? door.GetComponentInChildren<ConditionLock>()
                : null;
            if (conditionLock == null)
            {
                _layout.Warnings.Add($"{gate} can't be opened by terminals: its door has no ConditionLock.");
                return;
            }

            int placed = 0;
            foreach (int keyRoom in connection.KeyNodeIds)
            {
                if (!_layout.Rooms.TryGetValue(keyRoom, out LevelRoom room)) continue;
                if (!TryTakeTile(room, KeepPropsOff, out Vector3 position)) continue;

                GameObject terminal = Object.Instantiate(_settings.TerminalPrefab, position, RandomYaw(), _parent);
                if (terminal.TryGetComponent(out LockTerminal lockTerminal))
                {
                    lockTerminal.Bind(conditionLock);
                    placed++;
                }
            }

            if (placed < connection.KeyNodeIds.Count)
                _layout.Warnings.Add($"{gate}: only {placed} of {connection.KeyNodeIds.Count} terminals could be placed.");
            // Never ask for more terminals than exist, or the door could never open.
            conditionLock.SetRequired(Mathf.Max(1, placed));
        }

        public void SpawnEnemies()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                if (rule == null) continue;
                GameObject[] roster = EnemiesFor(room.Faction, rule);
                GameObject[] breach = room.BreachFaction != null ? EnemiesFor(room.BreachFaction, rule) : roster;
                if (roster.Length == 0 && breach.Length == 0) continue;

                int count = rule.EnemyCount.Lerp(room.Node.Intensity);
                if (count <= 0) continue;

                Transform[] route = CreatePatrolRoute(room);
                for (int i = 0; i < count && TryTakeTile(room, RoomTileTags.None, out Vector3 position); i++)
                {
                    // A Breach room alternates between its two factions' rosters.
                    bool fromBreach = roster.Length == 0 || (i % 2 == 1 && breach.Length > 0);
                    GameObject enemy = PrefabPool.Spawn(_random.Pick(fromBreach ? breach : roster), OnNavMesh(position), RandomYaw());
                    if (enemy.TryGetComponent(out EnemyAI ai)) ai.SetWaypoints(route);
                }
            }
        }

        // A faction-held room fields that faction's enemies; how many still comes from the room type.
        private static GameObject[] EnemiesFor(FactionDefinition faction, RoomContentRule rule) =>
            faction != null && faction.Enemies.Length > 0 ? faction.Enemies : rule.Enemies;

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
