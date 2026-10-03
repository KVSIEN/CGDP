using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using CGD.Core;
using CGD.Feedback;
using CGD.Loot;
using CGD.Map;
using CGD.WorldMap;

namespace CGD.Level
{
    // Builds a playable level from a map graph when the scene loads: rooms and corridors,
    // doors on connections (and keys for the locked ones), room contents, a NavMesh for
    // enemies, the player in the Start room and a way out in the Exit room.
    //
    // The graph is either a MapGraphAsset (a hand-checked map, same every time) or generated
    // on the spot from a MapGenerationSettings style and a seed. Seed 0 rolls a new one every play
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
        [Tooltip("0 = a new random seed every play (with a graph asset: the asset's seed). Also drives room shapes and contents")]
        [SerializeField] private int _seed;
        [SerializeField] private LevelBuildSettings _settings;

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

        public Seed        Seed   { get; private set; }
        public MapGraph    Graph  { get; private set; }
        public LevelLayout Layout { get; private set; }

        private Texture2D _mapTexture;
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
            Seed  = _seed != 0 ? Seed.From(_seed)
                  : _graphAsset != null ? Seed.From(_graphAsset.Seed)
                  : Seed.Random();
            Graph = ResolveGraph(out Vector2 nodeSpacing, out MapContentSettings content);
            if (Graph == null)
            {
                Debug.LogError($"{name}: LevelBuilder needs a map graph asset, or map generation settings with content and a layout.", this);
                return;
            }

            Layout = new LevelLayoutBuilder(_settings).Build(Graph, nodeSpacing, Seed, content);
            new LevelGeometryBuilder(_settings).Build(Layout, transform);
            new DoorwaySignBuilder(_settings).Build(Layout, transform);
            foreach (string warning in Layout.Warnings)
                Debug.LogWarning($"{name}: {warning}", this);

            var populator = new RoomPopulator(_settings, Layout, transform, Seed.Derive("contents").Stream());
            populator.PlaceProps();
            populator.PlaceKeys(Graph);
            PlaceExit(populator);
            ApplyLootLuck();

            if (_navMesh != null) _navMesh.BuildNavMesh();
            populator.SpawnEnemies();

            PlacePlayer(populator);
            FitWorldMap();
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
            if (_graphAsset != null)
            {
                nodeSpacing = _graphAsset.Settings != null ? _graphAsset.Settings.NodeSpacing : Vector2.one;
                content     = _graphAsset.Content;
                Modifiers   = _graphAsset.Tuning.Modifiers;
                return _graphAsset.Graph;
            }

            nodeSpacing = _generation != null ? _generation.NodeSpacing : Vector2.one;
            content     = _generation != null ? _generation.Content : null;
            if (_generation == null || !_generation.CanGenerate) return null;

            MapGenerationResult result = new MapGenerator(_generation).Generate(Seed.Derive("map"));
            Modifiers = result.Modifiers;
            return result.Graph;
        }

        private void PlacePlayer(RoomPopulator populator)
        {
            LevelRoom start = FindRoom(MapNodeType.Start);
            if (start == null) return;

            // Slightly above the floor so the player's capsule doesn't start inside it.
            Vector3 position = populator.RoomCenter(start) + Vector3.up;
            if (_spawnPoint != null) _spawnPoint.position = position;
            if (_player     != null) _player.position     = position;
        }

        // The Exit room's content may bring its own exit; otherwise a plain pad is added.
        private void PlaceExit(RoomPopulator populator)
        {
            LevelRoom exit = FindRoom(MapNodeType.Exit);
            if (exit == null || GetComponentInChildren<LevelExit>() != null) return;

            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "LevelExit";
            pad.transform.SetParent(transform, true);
            pad.transform.SetPositionAndRotation(populator.RoomCenter(exit) + Vector3.up * 0.1f, transform.rotation);
            pad.transform.localScale = new Vector3(2f, 0.1f, 2f);
            pad.AddComponent<LevelExit>();
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

        private void OnDestroy()
        {
            if (_mapTexture != null) Destroy(_mapTexture);
        }

        // The map covers exactly what the blueprint image does, so the image needs no scaling.
        private void FitBlueprintMap()
        {
            Color32[] pixels = LevelBlueprint.Render(Layout, _settings, LevelBlueprint.Fill.RoomType,
                                                     out int width, out int height, out Vector2Int tileOrigin);
            var texture = _mapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name       = "LevelBlueprint",
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
            };
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
