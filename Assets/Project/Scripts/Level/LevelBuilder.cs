using UnityEngine;
using Unity.AI.Navigation;
using CGD.Core;
using CGD.Map;
using CGD.WorldMap;

namespace CGD.Level
{
    // Builds a playable level from a map graph when the scene loads: rooms and corridors,
    // doors on connections, room contents, a NavMesh for enemies, the player in the Start
    // room and a way out in the Exit room.
    //
    // The graph is either a MapGraphAsset (a hand-checked map, same every time) or generated
    // on the spot from MapGenerationSettings and a seed. Seed 0 rolls a new one every play;
    // the one used is shown in Seed so a good level can be kept.
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
        [Tooltip("0 = a new random seed every play. Also drives room contents")]
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

        public Seed        Seed   { get; private set; }
        public MapGraph    Graph  { get; private set; }
        public LevelLayout Layout { get; private set; }

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"{name}: LevelBuilder needs a LevelBuildSettings asset.", this);
                return;
            }

            Seed  = _seed != 0 ? Seed.From(_seed) : Seed.Random();
            Graph = ResolveGraph(out Vector2 nodeSpacing);
            if (Graph == null)
            {
                Debug.LogError($"{name}: LevelBuilder needs a map graph asset or map generation settings.", this);
                return;
            }

            Layout = new LevelLayoutBuilder(_settings).Build(Graph, nodeSpacing);
            foreach (string warning in Layout.Warnings)
                Debug.LogWarning($"{name}: {warning}", this);

            new LevelGeometryBuilder(_settings).Build(Layout, transform);

            var populator = new RoomPopulator(_settings, Layout, transform, Seed.Derive("contents").Stream());
            populator.PlaceProps();
            PlaceExit(populator);

            if (_navMesh != null) _navMesh.BuildNavMesh();
            populator.SpawnEnemies();

            PlacePlayer(populator);
            FitWorldMap();
        }

        private MapGraph ResolveGraph(out Vector2 nodeSpacing)
        {
            if (_graphAsset != null)
            {
                nodeSpacing = _graphAsset.Settings != null ? _graphAsset.Settings.NodeSpacing : Vector2.one;
                return _graphAsset.Graph;
            }

            nodeSpacing = _generation != null ? _generation.NodeSpacing : Vector2.one;
            return _generation != null ? new MapGenerator(_generation).Generate(Seed.Derive("map")).Graph : null;
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

            Rect bounds = Layout.LocalBounds();
            const float Margin = 10f;
            _worldMap.SetBounds(
                transform.TransformPoint(new Vector3(bounds.center.x, 0f, bounds.center.y)),
                bounds.size + Vector2.one * Margin * 2f);
        }

        private LevelRoom FindRoom(MapNodeType type)
        {
            foreach (LevelRoom room in Layout.Rooms.Values)
                if (room.Node.Type == type) return room;
            return null;
        }
    }
}
