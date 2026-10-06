using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Map;
using CGD.WorldMap;

namespace CGD.Level
{
    // Turns the level's encounter rooms (Lockdown, Holdout, Ambush, Stealth, Rift, Puzzle,
    // Hazard) into encounters once the level is built, its NavMesh baked and its enemies
    // placed: a shutter in each of the room's doorways, spots set aside for enemies and
    // consoles, and the room type's RoomEncounter on its own object under the level.
    public class RoomEncounterBuilder
    {
        // How far into the room a shutter stands from the doorway's edge, clear of the gate door.
        private const float ShutterInset = 0.4f;
        private const int   ConsoleSpots = 8;

        private readonly EncounterSettings _settings;
        private readonly LevelLayout       _layout;
        private readonly MapGraph          _graph;
        private readonly Transform         _level;
        private readonly RoomPopulator     _populator;
        private readonly Transform         _player;
        private readonly WorldMapArea      _worldMap;
        private readonly RandomStream      _random;
        private readonly float             _lootLuck;

        public RoomEncounterBuilder(EncounterSettings settings, LevelLayout layout, MapGraph graph, Transform level,
            RoomPopulator populator, Transform player, WorldMapArea worldMap, RandomStream random, float lootLuck)
        {
            _settings  = settings;
            _layout    = layout;
            _graph     = graph;
            _level     = level;
            _populator = populator;
            _player    = player;
            _worldMap  = worldMap;
            _random    = random;
            _lootLuck  = lootLuck;
        }

        private static Type EncounterFor(MapNodeType type) => type switch
        {
            MapNodeType.Lockdown => typeof(LockdownEncounter),
            MapNodeType.Holdout  => typeof(HoldoutEncounter),
            MapNodeType.Ambush   => typeof(AmbushEncounter),
            MapNodeType.Stealth  => typeof(StealthEncounter),
            MapNodeType.Rift     => typeof(RiftEncounter),
            MapNodeType.Puzzle   => typeof(PuzzleEncounter),
            MapNodeType.Hazard   => typeof(HazardEncounter),
            _                    => null,
        };

        public void Build()
        {
            if (_settings == null) return;

            var analysis = new MapGraphAnalysis(_graph);
            foreach (LevelRoom room in _layout.Rooms.Values)
            {
                if (!room.Node.Type.IsEncounter()) continue;
                Type encounterType = EncounterFor(room.Node.Type);
                if (encounterType == null) continue;

                var holder = new GameObject($"{room.Node.Type}Encounter_{room.Node.Id}");
                holder.transform.SetParent(_level, false);
                var encounter = (RoomEncounter)holder.AddComponent(encounterType);
                encounter.Begin(CreateContext(room, analysis, holder.transform));
            }
        }

        private EncounterContext CreateContext(LevelRoom room, MapGraphAnalysis analysis, Transform holder)
        {
            var doorways = new List<LevelDoorway>();
            foreach (LevelDoorway doorway in _layout.Doorways)
                if (doorway.Room == room) doorways.Add(doorway);

            var doorTiles = new List<Vector2Int>();
            foreach (LevelDoorway doorway in doorways) doorTiles.Add(doorway.RoomTile);

            Matrix4x4 worldToLevel = _level.worldToLocalMatrix;
            RoomArea area  = RoomArea.Inner(room.Footprint.Tiles, doorTiles, _layout.TileSize, worldToLevel);
            RoomArea floor = new(room.Footprint.Tiles, _layout.TileSize, worldToLevel);

            var spawnSpots = new List<Vector3>();
            for (int i = 0; i < _settings.SpawnSpots && _populator.TryTakeSpawnSpot(room, out Vector3 spot); i++)
                spawnSpots.Add(spot);

            var consoleSpots = new Queue<Vector3>();
            for (int i = 0; i < ConsoleSpots && _populator.TryTakeSpot(room, out Vector3 spot); i++)
                consoleSpots.Enqueue(spot);

            var spawner = new EncounterSpawner(spawnSpots, _populator.RosterFor(room), _populator.SecondRosterFor(room),
                                               _populator.PatrolRouteFor(room), _random);

            return new EncounterContext(room, area, floor, PlaceShutters(doorways, holder), spawner, _settings,
                _populator.EnemiesIn(room), _player, _level, _layout.TileSize, _populator.RoomCenter(room), _random, _lootLuck + _populator.LootLuckFor(room),
                _worldMap, consoleSpots, ForwardDoorways(room, doorways, analysis));
        }

        private RoomSeal PlaceShutters(List<LevelDoorway> doorways, Transform holder)
        {
            var shutters = new List<RoomShutter>();
            if (_settings.ShutterPrefab != null)
                foreach (LevelDoorway doorway in doorways)
                {
                    Pose pose = LevelGeometryBuilder.DoorwayPose(_layout, doorway, _level);
                    Vector3 position = pose.position - pose.forward * ShutterInset;
                    shutters.Add(UnityEngine.Object.Instantiate(_settings.ShutterPrefab, position, pose.rotation, holder));
                }
            return new RoomSeal(shutters);
        }

        // Open passages from this room to rooms further from Start: where a Puzzle room's
        // locked doors go. Gates and one-way doors already have a door of their own.
        private List<Pose> ForwardDoorways(LevelRoom room, List<LevelDoorway> doorways, MapGraphAnalysis analysis)
        {
            var forward = new List<Pose>();
            if (room.Node.Type != MapNodeType.Puzzle) return forward;

            int depth = analysis.Depth(room.Node.Id);
            foreach (LevelDoorway doorway in doorways)
            {
                MapConnection connection = doorway.Connection;
                if (connection.IsGate || connection.OneWay) continue;
                if (analysis.Depth(connection.Other(room.Node.Id)) <= depth) continue;
                forward.Add(LevelGeometryBuilder.DoorwayPose(_layout, doorway, _level));
            }
            return forward;
        }
    }
}
