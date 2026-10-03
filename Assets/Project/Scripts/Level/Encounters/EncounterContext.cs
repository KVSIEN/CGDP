using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.WorldMap;

namespace CGD.Level
{
    // Everything a room's encounter works with, gathered by RoomEncounterBuilder once the
    // level stands.
    public class EncounterContext
    {
        public EncounterContext(LevelRoom room, RoomArea area, RoomArea floor, RoomSeal seal, EncounterSpawner spawner,
            EncounterSettings settings, IReadOnlyList<HealthManager> roomEnemies, Transform player, Transform level,
            float tileSize, Vector3 center, RandomStream random, float lootLuck, WorldMapArea worldMap,
            Queue<Vector3> freeSpots, IReadOnlyList<Pose> forwardDoorways)
        {
            Room            = room;
            Area            = area;
            Floor           = floor;
            Seal            = seal;
            Spawner         = spawner;
            Settings        = settings;
            RoomEnemies     = roomEnemies;
            Player          = player;
            Level           = level;
            TileSize        = tileSize;
            Center          = center;
            Random          = random;
            LootLuck        = lootLuck;
            WorldMap        = worldMap;
            _freeSpots      = freeSpots;
            ForwardDoorways = forwardDoorways;
        }

        private readonly Queue<Vector3> _freeSpots;

        public LevelRoom         Room     { get; }
        // Inside, clear of the doorways: where the player must be for the room to seal.
        public RoomArea          Area     { get; }
        // The whole floor.
        public RoomArea          Floor    { get; }
        public RoomSeal          Seal     { get; }
        public EncounterSpawner  Spawner  { get; }
        public EncounterSettings Settings { get; }
        // Enemies the level placed in the room when it was built.
        public IReadOnlyList<HealthManager> RoomEnemies { get; }
        public Transform    Player   { get; }
        public Transform    Level    { get; }
        public float        TileSize { get; }
        public Vector3      Center   { get; }
        public RandomStream Random   { get; }
        // The run's loot luck (modifiers), added to every reward this encounter drops.
        public float        LootLuck { get; }
        public WorldMapArea WorldMap { get; }
        // Doorways (pose: on the floor, +Z out of the room) leading deeper into the map.
        public IReadOnlyList<Pose> ForwardDoorways { get; }

        public int WaveSize => Settings.WaveSize.Lerp(Room.Node.Intensity);

        // A free floor spot in the room for a terminal or console; the centre once they run out.
        public Vector3 TakeSpot() => _freeSpots.Count > 0 ? _freeSpots.Dequeue() : Center;
    }
}
