using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using CGD.Core;
using CGD.DevTools;
using CGD.Director;
using CGD.Enemies;
using CGD.Feedback;
using CGD.Interaction;
using CGD.Loot;
using CGD.Map;
using CGD.Player;
using CGD.Quests;
using CGD.WorldMap;

namespace CGD.Level
{
    // Builds a playable level from a map graph when the scene loads: rooms and corridors,
    // doors on connections (and keys for the locked ones), room contents, a NavMesh for
    // enemies, the player in the Start room and a way out in the Exit room.
    //
    // The graph is either a MapGraphAsset (a hand-checked map, same every time), generated
    // on the spot from a MapGenerationSettings style and a seed, or a RoomGallerySettings
    // walk-through of every place (the RoomGallery scene), or a single made-up room from a
    // RoomGenerator (the RoomGenerator scene). Seed 0 rolls a new one every play
    // (with a graph asset, 0 uses the asset's seed, matching its blueprint); the one used is
    // shown in Seed so a good level can be kept.
    //
    // Runs before everything else so the level exists by the time other components wake up
    // (the world map fits itself to it, enemies find their NavMesh).
    [DefaultExecutionOrder(-500)]
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Map")]
        [Tooltip("Build this graph. Leave empty to generate one from the settings below")]
        [SerializeField] private MapGraphAsset _graphAsset;
        [SerializeField] private MapGenerationSettings _generation;
        [Tooltip("Build a gallery of every place, faction and side instead of a map (takes the place of the graph asset and the generation settings)")]
        [SerializeField] private RoomGallerySettings _gallery;
        [Tooltip("Build the one room this makes, with the player (or the spectator) in it, instead of a map (takes the place of everything above, and of the seed)")]
        [SerializeField] private RoomGenerator _roomGenerator;
        [Tooltip("A number or any text. Empty or 0 = a new random seed every play (with a graph asset: the asset's seed). Also drives room shapes and contents")]
        [SerializeField] private string _seed;
        [Tooltip("For testing one room: start (and respawn) in the room of this node id instead of the Start room. -1 = the Start room")]
        [SerializeField] private int _startRoom = -1;
        [SerializeField] private LevelBuildSettings _settings;
        [Tooltip("Optional — how Lockdown, Holdout, Ambush, Stealth, Rift, Puzzle and Hazard rooms play. Empty = they play as plain rooms")]
        [SerializeField] private EncounterSettings _encounters;
        [Tooltip("Optional — main objectives that open the Boss room and side objectives for rewards. Needs a QuestTracker on the player. Empty = no objectives")]
        [SerializeField] private MapObjectiveSettings _objectives;

        [Header("Scene")]
        [Tooltip("Rebuilt after the geometry is placed. Set it to collect this object's children")]
        [SerializeField] private NavMeshSurface _navMesh;
        [Tooltip("Moved into the Start room")]
        [SerializeField] private Transform _player;
        [Tooltip("The respawn point PlayerLifecycle uses, moved into the Start room")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("Optional — resized to cover the level")]
        [SerializeField] private WorldMapArea _worldMap;
        [Tooltip("Draw the world map and minimap from the level's floor plan (rooms coloured by type, walls, gates) instead of the area's own background")]
        [SerializeField] private bool _blueprintMap = true;
        [Tooltip("Optional — the scene's cover spots, found as soon as the NavMesh is built instead of when the first enemy looks for cover mid-fight")]
        [SerializeField] private CoverMap _coverMap;
        [Tooltip("Once the NavMesh is built, let enemies hop the low cover (low inner walls, barriers, counters) where it saves them a walk round")]
        [SerializeField] private bool _vaultLowCover = true;
        [Tooltip("Optional — the camera the player sees through. With it, the furniture of rooms it can't see into through a doorway stops being drawn (RoomCuller), while Cull Hidden Rooms is on; /cull in the dev console switches it, to compare while profiling")]
        [SerializeField] private Camera _cullingCamera;
        [SerializeField] private bool _cullHiddenRooms;
        [Tooltip("Optional — the director's reinforcements, given the level's route so groups arrive in rooms ahead of the player in the build-up and behind them at the peak")]
        [SerializeField] private ReinforcementSpawner _reinforcements;
        [Tooltip("Name each room in the HUD feed (place, faction, tier) the first time the player walks in; in a gallery, every time, with its side, depth and fight type too. Needs a FeedbackPlayer in the scene")]
        [SerializeField] private bool _announceRooms = true;
        [Tooltip("Outline every room along its wall tops in its room type's colour and label it from above (id, place, room type, layout, faction, tier), for reading a generated map in the Scene view. /overview in the dev console hides or shows it")]
        [SerializeField] private bool _roomOverview = true;

        public LevelBuildSettings Settings => _settings;
        public Seed        Seed   { get; private set; }
        public MapGraph    Graph  { get; private set; }
        public LevelLayout Layout { get; private set; }

        // Meshes, materials and the map texture made for this level, freed with it.
        private readonly GeneratedAssets _assets = new();
        // The run modifiers the map was generated with (a MapGraphAsset's are recovered from its seed).
        public IReadOnlyList<MapRunModifier> Modifiers { get; private set; } = Array.Empty<MapRunModifier>();

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{name}: LevelBuilder needs a LevelBuildSettings asset.", this);
                return;
            }

            // A graph asset's own seed lays its rooms out the way the Map Graph window's
            // blueprint shows them; only generated maps roll a fresh seed for 0.
            Seed  = _roomGenerator != null ? _roomGenerator.Seed
                  : !RollsFreshSeed(_seed) ? Seed.Parse(_seed)
                  : _graphAsset != null && _gallery == null ? _graphAsset.Seed
                  : Seed.Random();
            Graph = ResolveGraph(out Vector2 nodeSpacing, out MapContentSettings content);
            if (Graph == null)
            {
                Debug.LogError($"{name}: LevelBuilder needs a map graph asset, or map generation settings with content and a layout.", this);
                return;
            }

            LevelLayoutBuilder layoutBuilder = _roomGenerator != null ? _roomGenerator.LayoutBuilder(_settings) : new LevelLayoutBuilder(_settings);
            Layout = layoutBuilder.Build(Graph, nodeSpacing, Seed, content);
            var populator = new RoomPopulator(_settings, Layout, transform, Seed.Derive("contents"));
            List<PlannedObjective> objectives = PlanObjectives(populator);

            // Furnished first, so the overgrowth leaves the furniture's floor bare; both before the
            // geometry, which builds the overgrowth with the floor.
            populator.FurnishRooms();
            OvergrowthSource overgrowth = _roomGenerator != null ? _roomGenerator.Overgrowth(_settings) : OvergrowthSource.FromFactions(_settings);
            new OvergrowthPlanner(Layout.TileSize).PlanLevel(Layout, overgrowth, Seed.Derive("overgrowth"));

            var geometry = new LevelGeometryBuilder(_settings, _assets);
            LockBossRoom(geometry, objectives);
            geometry.Build(Layout, transform);
            new DoorwaySignBuilder(_settings, _assets).Build(Layout, transform);

            populator.PlaceProps();
            populator.PlaceKeys(Graph, geometry.Gates);
            PlaceExit(populator);
            foreach (string warning in Layout.Warnings)
                Debug.LogWarning($"{name}: {warning}", this);
            ApplyLootLuck();

            if (_navMesh != null) _navMesh.BuildNavMesh();
            if (_navMesh != null && _vaultLowCover) new VaultLinkBuilder().Build(Layout, transform, populator.Furniture, _navMesh.agentTypeID);
            // Hundreds of probes on a big level: done here, during the load, not on the frame an
            // enemy first wants cover.
            if (_coverMap != null) _coverMap.Rebuild();
            if (_reinforcements != null) _reinforcements.FollowRoute(new LevelRoute(Layout, Graph, transform));
            populator.SpawnEnemies();
            populator.SpawnFauna();
            new RoomEncounterBuilder(_encounters, Layout, Graph, transform, populator, _player, _worldMap,
                                     Seed.Derive("encounters"), new MapRunTuning(Modifiers).LootLuck).Build();

            StartObjectives(objectives, populator, geometry);
            PlacePlayer(populator);
            FitWorldMap();
            AnnounceRooms();
            AddRoomTravel(populator);
            AddRoomCulling(populator);
            // Last, so neither the NavMesh nor the cover map ever sees it.
            if (_roomOverview) AddOverviewCommand(new RoomOverviewBuilder(_settings, _assets).Build(Layout, transform));
        }

        // The dev console's /overview, wherever the player carries one: hides or shows the room labels and outlines.
        private void AddOverviewCommand(GameObject overview)
        {
            if (overview == null || _player == null || !_player.TryGetComponent(out DevCommands commands)) return;
            commands.Console.Register("overview", "", "Hide or show the room type outlines and labels above every room",
                _ =>
                {
                    overview.SetActive(!overview.activeSelf);
                    return overview.activeSelf ? "Room overview shown." : "Room overview hidden.";
                });
        }

        private void AnnounceRooms()
        {
            if (!_announceRooms || _player == null) return;

            var announcer = gameObject.AddComponent<RoomArrivalAnnouncer>();
            if (_gallery != null) announcer.BeginGallery(Layout, transform, _player, _settings.TerritoryDepth);
            else                  announcer.Begin(Layout, transform, _player);
        }

        // Not on by default: worth it only where profiling shows the rooms' props cost (RoomCuller).
        private void AddRoomCulling(RoomPopulator populator)
        {
            if (_cullingCamera == null) return;

            var culler = gameObject.AddComponent<RoomCuller>();
            culler.Begin(Layout, transform, _cullingCamera, _player, populator.RoomObjects);
            culler.enabled = _cullHiddenRooms;
            if (_player == null || !_player.TryGetComponent(out DevCommands commands)) return;
            commands.Console.Register("cull", "", "Switch drawing only the rooms the camera can see into on or off",
                _ =>
                {
                    culler.enabled = !culler.enabled;
                    return culler.enabled ? "Hidden rooms' furniture is no longer drawn." : "Every room is drawn.";
                });
        }

        // The dev console's /room, wherever the player carries one.
        private void AddRoomTravel(RoomPopulator populator)
        {
            if (_player == null || !_player.TryGetComponent(out DevCommands commands) || !_player.TryGetComponent(out PlayerMovement movement)) return;
            new RoomTravelCommand(Layout, transform, movement, populator.RoomCenter).Register(commands.Console);
        }

        // "0" too: the field used to be a number where 0 meant a fresh seed, and scenes saved then still hold it.
        private static bool RollsFreshSeed(string text) => string.IsNullOrWhiteSpace(text) || text.Trim() == "0";

        // Only when the player can track quests; the same seed always plans the same objectives.
        private List<PlannedObjective> PlanObjectives(RoomPopulator populator)
        {
            var none = new List<PlannedObjective>();
            if (_objectives == null || _player == null || !_player.TryGetComponent(out QuestTracker _)) return none;

            RandomStream random = Seed.Derive("objectives").Stream();
            var planner = new MapObjectivePlanner(Graph, random,
                node => Layout.Rooms.TryGetValue(node.Id, out LevelRoom room) && populator.WillHaveEnemies(room));
            return planner.Plan(_objectives.Templates, _objectives.MainCount.Evaluate(random), _objectives.SideCount.Evaluate(random));
        }

        // With main objectives, every way into the Boss room gets a door that waits on them.
        private void LockBossRoom(LevelGeometryBuilder geometry, List<PlannedObjective> objectives)
        {
            MapNode boss = Graph.FindFirst(MapNodeType.Boss);
            if (boss == null || !objectives.Exists(o => o.IsMain)) return;

            geometry.ObjectiveDoorPrefab = _objectives.BossDoorPrefab != null ? _objectives.BossDoorPrefab : _settings.TerminalDoorPrefab;
            foreach (MapConnection connection in Graph.Connections)
                if (connection.Connects(boss.Id)) geometry.ObjectiveGated.Add(connection);
        }

        private void StartObjectives(List<PlannedObjective> objectives, RoomPopulator populator, LevelGeometryBuilder geometry)
        {
            if (objectives.Count == 0) return;

            int mainCount = objectives.FindAll(o => o.IsMain).Count;
            var bossLocks = new List<ConditionLock>();
            foreach (GameObject door in geometry.ObjectiveDoors)
            {
                ConditionLock bossLock = door.GetComponentInChildren<ConditionLock>();
                if (bossLock == null)
                {
                    Debug.LogWarning($"{name}: the Boss room's objective door has no ConditionLock, so it can't wait on the main objectives.", door);
                    continue;
                }
                bossLock.SetLabel("Objectives");
                bossLock.SetRequired(mainCount);
                bossLocks.Add(bossLock);
            }

            var runner = gameObject.AddComponent<MapObjectiveRunner>();
            runner.Begin(objectives, Layout.Rooms, _objectives, populator, _player.GetComponent<QuestTracker>(), bossLocks,
                         new MapRunTuning(Modifiers).LootLuck, Seed.Derive("objective rewards"));
        }

        // In Start, not Awake: the HUD subscribes to FeedbackBus in its OnEnable, after this
        // builder (which runs first) has woken.
        private void Start()
        {
            foreach (MapRunModifier modifier in Modifiers)
            {
                string message = string.IsNullOrEmpty(modifier.Description)
                    ? modifier.DisplayName
                    : $"{modifier.DisplayName}: {modifier.Description}";
                FeedbackBus.Notify(message, modifier.Kind == MapRunModifierKind.Warning ? NotificationStyle.Warning : NotificationStyle.Info);
            }
        }

        // Warnings pay out through the level's loot containers (chests, caches): props and
        // centrepieces placed under this builder.
        private void ApplyLootLuck()
        {
            float luck = new MapRunTuning(Modifiers).LootLuck;
            if (luck <= 0f) return;

            foreach (LootDropper dropper in GetComponentsInChildren<LootDropper>(true))
                dropper.AddLuck(luck);
        }

        // content: where the graph's faction indices point (null = rooms have no faction).
        private MapGraph ResolveGraph(out Vector2 nodeSpacing, out MapContentSettings content)
        {
            if (_roomGenerator != null)
            {
                // Positions are in cells, as in the gallery.
                nodeSpacing = Vector2.one;
                content     = _roomGenerator.Content;
                return _roomGenerator.BuildGraph(_settings);
            }
            if (_gallery != null)
            {
                // Gallery positions are in cells.
                nodeSpacing = Vector2.one;
                content     = _gallery.Content;
                return RoomGalleryGraph.Build(_gallery, _settings);
            }
            if (_graphAsset != null)
            {
                nodeSpacing = _graphAsset.NodeSpacing;
                content     = _graphAsset.Content;
                Modifiers   = _graphAsset.Tuning.Modifiers;
                return _graphAsset.Graph;
            }

            nodeSpacing = _generation != null ? _generation.NodeSpacing : MapGenerationSettings.DefaultNodeSpacing;
            content     = _generation != null ? _generation.Content : null;
            if (_generation == null || !_generation.CanGenerate) return null;

            MapGenerationResult result = new MapGenerator(_generation).Generate(Seed.Derive("map"));
            Modifiers = result.Modifiers;
            return result.Graph;
        }

        // Metres the spectator may pan from the room's centre: 1.5 x the way to the far corner of everything built round it, plus a margin.
        private float ReachFrom(LevelRoom room, RoomPopulator populator)
        {
            Vector3 centre = populator.RoomCenter(room);
            float reach = 0f;
            foreach (LevelRoom other in Layout.Rooms.Values)
            {
                Rect rect = Layout.RoomLocalRect(other);
                foreach (Vector2 corner in new[] { rect.min, rect.max, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMin) })
                    reach = Mathf.Max(reach, Vector3.Distance(centre, transform.TransformPoint(new Vector3(corner.x, 0f, corner.y))));
            }
            return reach * 1.5f + 40f;
        }

        private void PlacePlayer(RoomPopulator populator)
        {
            int startRoom = _roomGenerator != null ? RoomRecipeGraph.RoomId : _startRoom;
            LevelRoom start = startRoom >= 0 && Layout.Rooms.TryGetValue(startRoom, out LevelRoom chosen) ? chosen : FindRoom(MapNodeType.Start);
            if (start == null) return;

            // Slightly above the floor so the player's capsule doesn't start inside it.
            Vector3 position = populator.RoomCenter(start) + Vector3.up;
            if (_spawnPoint != null) _spawnPoint.position = position;
            if (_roomGenerator != null) _roomGenerator.FrameRoom(populator.RoomCenter(start), ReachFrom(start, populator));
            if (_player == null) return;

            _player.position = position;
            // The body keeps its own pose and, interpolated, writes it back over the transform:
            // left at the scene's spot, it would pull the player back out of the level.
            if (_player.TryGetComponent(out Rigidbody body)) body.position = position;
        }

        // The Exit room's content may bring its own exit; otherwise a plain pad is added.
        private void PlaceExit(RoomPopulator populator)
        {
            LevelRoom exit = FindRoom(MapNodeType.Exit);
            if (exit == null || HasMainExit()) return;

            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "LevelExit";
            pad.transform.SetParent(transform, true);
            pad.transform.SetPositionAndRotation(populator.RoomCenter(exit) + Vector3.up * 0.1f, transform.rotation);
            pad.transform.localScale = new Vector3(2f, 0.1f, 2f);
            pad.AddComponent<LevelExit>();
        }

        // An Emergency Exit room's escape pod is an exit too, but not the way out.
        private bool HasMainExit()
        {
            foreach (LevelExit levelExit in GetComponentsInChildren<LevelExit>())
                if (!levelExit.IsEmergency) return true;
            return false;
        }

        private void FitWorldMap()
        {
            if (_worldMap == null) return;
            if (_blueprintMap)
            {
                FitBlueprintMap();
                return;
            }

            Rect bounds = Layout.LocalBounds();
            const float Margin = 10f;
            _worldMap.SetBounds(
                transform.TransformPoint(new Vector3(bounds.center.x, 0f, bounds.center.y)),
                bounds.size + Vector2.one * Margin * 2f);
        }

        private void OnDestroy() => _assets.DestroyAll();

        // The map covers exactly what the blueprint image does, so the image needs no scaling.
        private void FitBlueprintMap()
        {
            Color32[] pixels = LevelBlueprint.Render(Layout, _settings, LevelBlueprint.Fill.RoomType,
                                                     out int width, out int height, out Vector2Int tileOrigin);
            var texture = _assets.Track(new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name       = "LevelBlueprint",
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
            });
            texture.SetPixels32(pixels);
            texture.Apply(false);

            float tile = Layout.TileSize;
            Vector2 size = new Vector2(width, height) / LevelBlueprint.PixelsPerTile * tile;
            Vector2 min  = (Vector2)tileOrigin * tile;
            Vector2 centre = min + size * 0.5f;
            _worldMap.SetBounds(transform.TransformPoint(new Vector3(centre.x, 0f, centre.y)), size);
            _worldMap.SetBackground(texture);
        }

        private LevelRoom FindRoom(MapNodeType type)
        {
            foreach (LevelRoom room in Layout.Rooms.Values)
                if (room.Node.Type == type) return room;
            return null;
        }
    }
}
