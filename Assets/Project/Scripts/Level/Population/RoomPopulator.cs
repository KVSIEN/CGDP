using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;
using CGD.Factions;
using CGD.Interaction;
using CGD.Loot;
using CGD.Map;

namespace CGD.Level
{
    // Furnishes rooms from their function and room type (RoomFurnisher), puts their enemies on
    // the posts their faction favours (RoomEnemyPlanner), and fills them with what their
    // RoomContentRule asks for, on tiles the room's structure leaves free: never on
    // pillars, furniture, in doorways or on the centre spot, and props also keep off the walkways
    // and the floor kept in front of things to use, so routes and approaches stay clear. Props
    // and centrepieces go in before the NavMesh is built (so it walks around them), enemies
    // after (they need it).
    // Every placement comes from the level's seed, so the same seed gives the same level.
    // What gets rolled later — a chest's loot, an enemy's affixes and drops, a shop's shelf —
    // comes from it too: each placed thing and enemy gets its own seed by room and order
    // (ISeedable), never drawn from the placement stream, so seeding them moved nothing.
    public class RoomPopulator
    {
        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;
        private readonly Transform _parent;
        private readonly Seed _seed;
        private readonly RandomStream _random;
        private readonly RoomFurnisher _furnisher;
        private readonly RoomEnemyPlanner _enemyPlanner;
        // Built on first use, once the doorways are known.
        private TerritoryZones _territories;
        private readonly Dictionary<int, HomeZone> _roomZones = new();

        private const RoomTileTags KeepPropsOff = RoomTileTags.Walkway | RoomTileTags.Access;

        // Tiles still free to spawn on, per room, in shuffled order, and those already used.
        private readonly Dictionary<int, List<Vector2Int>> _freeTiles = new();
        private readonly Dictionary<int, HashSet<Vector2Int>> _takenTiles = new();
        // What each room was furnished with, for its enemies' posts.
        private readonly Dictionary<int, IReadOnlyList<PlannedProp>> _props = new();
        private readonly List<GameObject> _furniture = new();
        private readonly Dictionary<int, List<GameObject>> _roomObjects = new();
        // Enemies placed in each room at build time, for the room's encounter.
        private readonly Dictionary<int, List<HealthManager>> _roomEnemies = new();
        // Patrol loops per room and side (a Breach room's second faction walks its own).
        private readonly Dictionary<(int, bool), Transform[]> _routes = new();
        // Things placed in each room so far, numbering their seeds.
        private readonly Dictionary<int, int> _placedCount = new();

        public RoomPopulator(LevelBuildSettings settings, LevelLayout layout, Transform parent, Seed seed)
        {
            _settings = settings;
            _layout   = layout;
            _parent   = parent;
            _seed     = seed;
            _random   = seed.Stream();
            _furnisher = new RoomFurnisher(settings, seed);
            _enemyPlanner = new RoomEnemyPlanner(settings);
        }

        // Every solid piece PlaceProps put down (not decorations), for what reads the furnished
        // level afterwards (VaultLinkBuilder).
        public IReadOnlyList<GameObject> Furniture => _furniture;

        // What was placed in each room (its furniture, centrepiece, containers and nodes), by
        // room id, for what draws rooms on and off (RoomCuller).
        public IReadOnlyDictionary<int, List<GameObject>> RoomObjects => _roomObjects;

        public void PlaceProps()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                IReadOnlyList<PlannedProp> props = _furnisher.Furnish(room).Props;
                _props[room.Node.Id] = props;
                foreach (PlannedProp prop in props)
                {
                    if (prop.Prefab == null) continue;

                    Vector3 local = new Vector3(prop.Position.x, 0f, prop.Position.y) * _layout.TileSize;
                    GameObject placed = Place(room, prop.Prefab, _parent.TransformPoint(local), _parent.rotation * Quaternion.Euler(0f, prop.Yaw, 0f));
                    if (prop.Stretch != Vector2.one) placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(prop.Stretch.x, 1f, prop.Stretch.y));
                    // After any stretch, so its places to stand stay where they were put.
                    PropActivity.Attach(placed, prop, _layout.TileSize);
                    if (!prop.Decor) _furniture.Add(placed);
                }

                if (rule != null && rule.Centerpiece != null)
                    Place(room, rule.Centerpiece, RoomCenter(room), RandomYaw());
            }
        }

        // A room's things share its tier: containers roll with its loot luck and resource
        // nodes yield by it.
        private GameObject Place(LevelRoom room, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject placed = Object.Instantiate(prefab, position, rotation, _parent);
            int tier = room.Node.EffectiveTier;
            if (!_roomObjects.TryGetValue(room.Node.Id, out List<GameObject> objects)) _roomObjects[room.Node.Id] = objects = new List<GameObject>();
            objects.Add(placed);

            _placedCount.TryGetValue(room.Node.Id, out int index);
            _placedCount[room.Node.Id] = index + 1;
            ISeedable.SeedAll(placed, _seed.Derive("placed").Derive(room.Node.Id).Derive(index));

            float luck = _settings.LootLuckFor(tier);
            if (luck > 0f)
                foreach (LootDropper dropper in placed.GetComponentsInChildren<LootDropper>(true))
                    dropper.AddLuck(luck);

            foreach (ResourceNode node in placed.GetComponentsInChildren<ResourceNode>(true))
                node.SetTier(tier);
            return placed;
        }

        public float LootLuckFor(LevelRoom room) => _settings.LootLuckFor(room.Node.EffectiveTier);

        // Whether SpawnEnemies will put enemies in this room (before it has run).
        public bool WillHaveEnemies(LevelRoom room)
        {
            RoomContentRule rule = _settings.RuleFor(room.Node.Type);
            return RoomEnemyPlanner.CountFor(rule, room) > 0
                   && !(RosterFor(room).IsEmpty && SecondRosterFor(room).IsEmpty);
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

        // Each enemy starts on the post RoomEnemyPlanner gave it (its faction's favoured kind of
        // tile, on its own side of a Breach room) and either holds it or patrols its side's loop.
        public void SpawnEnemies()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                RoomContentRule rule = _settings.RuleFor(room.Node.Type);
                if (rule == null) continue;
                RoomRoster roster = RosterFor(room);
                RoomRoster breach = SecondRosterFor(room);
                if (roster.IsEmpty && breach.IsEmpty) continue;

                int count = RoomEnemyPlanner.CountFor(rule, room);
                if (count <= 0) continue;

                _props.TryGetValue(room.Node.Id, out IReadOnlyList<PlannedProp> props);
                RoomEnemyPlan plan = _enemyPlanner.Plan(room, props ?? System.Array.Empty<PlannedProp>(), count,
                                                        RoomEnemyPlanner.SeedFor(_seed, room).Stream(), TakenTiles(room));
                var placed = new List<HealthManager>();
                HomeZone home = HomeOf(room);
                for (int i = 0; i < plan.Spawns.Count; i++)
                {
                    PlannedSpawn spawn = plan.Spawns[i];
                    Claim(room, spawn.Tile);
                    // A side whose roster is empty borrows the other's.
                    bool fromBreach = roster.IsEmpty || (spawn.Breach && !breach.IsEmpty);
                    GameObject prefab = (fromBreach ? breach : roster).Pick(_random);
                    if (prefab == null) continue;

                    Vector3 position = OnNavMesh(_parent.TransformPoint(_layout.TileToLocal(spawn.Tile)));
                    GameObject enemy = PrefabPool.Spawn(prefab, position, RandomYaw());
                    ISeedable.SeedAll(enemy, _seed.Derive("enemies").Derive(room.Node.Id).Derive(i));
                    // A pooled enemy keeps its last route unless told otherwise: guards get none.
                    if (enemy.TryGetComponent(out EnemyAI ai))
                    {
                        ai.SetWaypoints(spawn.Holds ? System.Array.Empty<Transform>() : RouteOf(room, plan, spawn.Breach));
                        ai.SetHome(home);
                    }
                    if (enemy.TryGetComponent(out HealthManager health)) placed.Add(health);
                }
                _roomEnemies[room.Node.Id] = placed;
            }
        }

        // The faction's fauna (FactionDefinition › Fauna) settle in its calm rooms: a group of
        // prey that wanders its room's territory. They are no enemies of the room: not counted
        // in its encounter, not in its enemy count. Drawn from their own stream, so adding or
        // changing them moves nothing else the level places.
        public void SpawnFauna()
        {
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                FactionDefinition faction = room.Faction;
                if (faction == null || !faction.HoldsFauna(room.Node.Type)) continue;

                RandomStream random = _seed.Derive("fauna").Derive(room.Node.Id).Stream();
                if (!random.Chance(faction.FaunaChance)) continue;

                GameObject prefab = random.Pick(faction.Fauna);
                int count = faction.FaunaGroup.Evaluate(random);
                HomeZone home = HomeOf(room);
                for (int i = 0; i < count; i++)
                {
                    if (!TryTakeSpawnSpot(room, out Vector3 position)) break;

                    GameObject animal = PrefabPool.Spawn(prefab, position, _parent.rotation * Quaternion.Euler(0f, random.Range(0f, 360f), 0f));
                    ISeedable.SeedAll(animal, _seed.Derive("fauna").Derive(room.Node.Id).Derive(i));
                    if (!animal.TryGetComponent(out EnemyAI ai)) continue;

                    ai.SetWaypoints(System.Array.Empty<Transform>());
                    ai.SetHome(home);
                }
            }
        }

        // A faction-held room fields that faction's enemies (by the room's tier); how many still
        // comes from the room type.
        public RoomRoster RosterFor(LevelRoom room) => RosterOf(room.Faction, room);

        // The second reality's roster in a Breach or Rift room; otherwise the room's own.
        public RoomRoster SecondRosterFor(LevelRoom room) => room.BreachFaction == null ? RosterFor(room) : RosterOf(room.BreachFaction, room);

        private RoomRoster RosterOf(FactionDefinition faction, LevelRoom room)
        {
            RoomContentRule rule = _settings.RuleFor(room.Node.Type);
            GameObject[] fallback = rule != null ? rule.Enemies : System.Array.Empty<GameObject>();
            return new RoomRoster(faction, fallback, _settings.EnemyTierOddsFor(room.Node.EffectiveTier));
        }

        // The territory a room's enemies (build-time and encounter ones alike) stay within.
        public HomeZone HomeOf(LevelRoom room) => (_territories ??= new TerritoryZones(_layout, _parent)).ZoneOf(room);

        // The room alone, for enemies an encounter brings in: they fight where the encounter is
        // (its shutters seal the doorways) instead of spilling into the rest of the territory.
        public HomeZone EncounterHomeOf(LevelRoom room)
        {
            if (_roomZones.TryGetValue(room.Node.Id, out HomeZone zone)) return zone;
            return _roomZones[room.Node.Id] = new HomeZone(new[] { _layout.RoomLocalRect(room) }, _parent);
        }

        public IReadOnlyList<HealthManager> EnemiesIn(LevelRoom room) =>
            _roomEnemies.TryGetValue(room.Node.Id, out List<HealthManager> enemies) ? enemies : System.Array.Empty<HealthManager>();

        // The holding faction's patrol loop, shared by everything patrolling the room (an
        // encounter's reinforcements too), built once.
        public Transform[] PatrolRouteFor(LevelRoom room)
        {
            if (_routes.TryGetValue((room.Node.Id, false), out Transform[] route)) return route;
            RoomEnemyPlan plan = _enemyPlanner.Plan(room, System.Array.Empty<PlannedProp>(), 0, RoomEnemyPlanner.SeedFor(_seed, room).Stream());
            return RouteOf(room, plan, breach: false);
        }

        private Transform[] RouteOf(LevelRoom room, RoomEnemyPlan plan, bool breach)
        {
            // An unsplit room has one loop, whichever faction walks it.
            breach &= plan.Split != null;
            if (_routes.TryGetValue((room.Node.Id, breach), out Transform[] route)) return route;
            _routes[(room.Node.Id, breach)] = route = CreatePatrolRoute(room, plan.PatrolOf(breach), breach);
            return route;
        }

        // A free floor spot for a console or cache: off walkways, columns and doorways.
        public bool TryTakeSpot(LevelRoom room, out Vector3 position) => TryTakeTile(room, KeepPropsOff, out position);

        // A free floor spot for an enemy to arrive on.
        public bool TryTakeSpawnSpot(LevelRoom room, out Vector3 position)
        {
            if (!TryTakeTile(room, RoomTileTags.None, out position)) return false;
            position = OnNavMesh(position);
            return true;
        }

        public Vector3 RoomCenter(LevelRoom room) => _parent.TransformPoint(_layout.RoomCenterLocal(room));

        private Transform[] CreatePatrolRoute(LevelRoom room, IReadOnlyList<Vector2Int> loop, bool breach)
        {
            var holder = new GameObject(breach ? $"PatrolRoute_{room.Node.Id}_Breach" : $"PatrolRoute_{room.Node.Id}").transform;
            holder.SetParent(_parent, false);

            var route = new Transform[loop.Count];
            for (int i = 0; i < loop.Count; i++)
            {
                route[i] = new GameObject($"Waypoint{i}").transform;
                route[i].SetParent(holder, false);
                route[i].localPosition = _layout.TileToLocal(loop[i]);
            }
            return route;
        }

        // Takes the last free tile without any of the `avoid` tags.
        private bool TryTakeTile(LevelRoom room, RoomTileTags avoid, out Vector3 position)
        {
            List<Vector2Int> free = FreeTiles(room);
            for (int i = free.Count - 1; i >= 0; i--)
            {
                Vector2Int tile = free[i];
                if (room.Structure != null && room.Structure.Has(tile, avoid)) continue;

                free.RemoveAt(i);
                TakenTiles(room).Add(tile);
                position = _parent.TransformPoint(_layout.TileToLocal(tile));
                return true;
            }

            position = default;
            return false;
        }

        private List<Vector2Int> FreeTiles(LevelRoom room)
        {
            if (_freeTiles.TryGetValue(room.Node.Id, out List<Vector2Int> free)) return free;
            free = RoomEnemyPlanner.SpawnableTiles(room, _settings.SpawnMarginTiles);
            _random.Shuffle(free);
            _freeTiles[room.Node.Id] = free;
            return free;
        }

        private HashSet<Vector2Int> TakenTiles(LevelRoom room)
        {
            if (!_takenTiles.TryGetValue(room.Node.Id, out HashSet<Vector2Int> taken))
                _takenTiles[room.Node.Id] = taken = new HashSet<Vector2Int>();
            return taken;
        }

        // A tile an enemy starts on: nothing else is put there afterwards.
        private void Claim(LevelRoom room, Vector2Int tile)
        {
            FreeTiles(room).Remove(tile);
            TakenTiles(room).Add(tile);
        }

        private Quaternion RandomYaw() => _parent.rotation * Quaternion.Euler(0f, _random.Range(0f, 360f), 0f);

        private static Vector3 OnNavMesh(Vector3 point) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : point;
    }
}
