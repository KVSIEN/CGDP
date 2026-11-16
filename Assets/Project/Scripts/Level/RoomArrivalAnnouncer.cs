using System.Collections.Generic;
using UnityEngine;
using CGD.Feedback;

namespace CGD.Level
{
    // The first time the player steps into a room, the HUD feed names it: what the place is,
    // who holds it and its tier (as its door signs do). Corridors and rooms already visited
    // stay quiet. In the Room Gallery every room is an exhibit: its fuller placard
    // (RoomLabel.Exhibit) shows each time the player walks in, so a room can be re-read on
    // the way back. Added and started by LevelBuilder once the level exists.
    public class RoomArrivalAnnouncer : MonoBehaviour
    {
        private const float CheckEvery = 0.25f;

        private LevelLayout _layout;
        private Transform _level;
        private Transform _player;
        // Gallery only: names each room's depth in its faction's territory. Null = a level.
        private TerritoryDepth _exhibitDepth;
        private readonly HashSet<int> _visited = new();
        private int _currentRoom = -1;
        private float _nextCheck;

        public void Begin(LevelLayout layout, Transform level, Transform player)
        {
            _layout = layout;
            _level  = level;
            _player = player;
        }

        public void BeginGallery(LevelLayout layout, Transform level, Transform player, TerritoryDepth depth)
        {
            Begin(layout, level, player);
            _exhibitDepth = depth;
        }

        private void Update()
        {
            if (_player == null || Time.time < _nextCheck) return;
            _nextCheck = Time.time + CheckEvery;

            LevelRoom room = _layout.RoomAtLocal(_level.InverseTransformPoint(_player.position));
            if (room == null) return;

            int previous = _currentRoom;
            _currentRoom = room.Node.Id;
            if (_exhibitDepth != null)
            {
                if (room.Node.Id != previous) FeedbackBus.Notify($"{RoomLabel.Name(room)} — {RoomLabel.Exhibit(room, _exhibitDepth)}");
                return;
            }

            if (_visited.Add(room.Node.Id)) FeedbackBus.Notify($"{RoomLabel.Name(room)} — {RoomLabel.Details(room)}");
        }
    }
}
