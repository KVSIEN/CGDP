using System.Collections.Generic;
using UnityEngine;
using CGD.Feedback;

namespace CGD.Level
{
    // The first time the player steps into a room, the HUD feed names it: what the place is,
    // who holds it and its tier (as its door signs do). Corridors and rooms already visited
    // stay quiet. Added and started by LevelBuilder once the level exists.
    public class RoomArrivalAnnouncer : MonoBehaviour
    {
        private const float CheckEvery = 0.25f;

        private LevelLayout _layout;
        private Transform _level;
        private Transform _player;
        private readonly HashSet<int> _visited = new();
        private float _nextCheck;

        public void Begin(LevelLayout layout, Transform level, Transform player)
        {
            _layout = layout;
            _level  = level;
            _player = player;
        }

        private void Update()
        {
            if (_player == null || Time.time < _nextCheck) return;
            _nextCheck = Time.time + CheckEvery;

            Vector3 local = _level.InverseTransformPoint(_player.position) / _layout.TileSize;
            LevelRoom room = _layout.RoomAt(new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.z)));
            if (room == null || !_visited.Add(room.Node.Id)) return;

            FeedbackBus.Notify($"{RoomLabel.Name(room)} — {RoomLabel.Details(room)}");
        }
    }
}
