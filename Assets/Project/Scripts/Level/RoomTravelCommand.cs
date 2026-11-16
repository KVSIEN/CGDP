using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CGD.DevTools;
using CGD.Player;

namespace CGD.Level
{
    // The dev console's /room: jump the player between a built level's rooms, for checking
    // rooms without walking the whole level (the Room Gallery has about eighty). Rooms go by
    // node id, which in the gallery is the order of the walk. Registered by LevelBuilder
    // when the player has DevCommands.
    public class RoomTravelCommand
    {
        private const int ListedAtMost = 40;

        private readonly LevelLayout _layout;
        private readonly Transform _level;
        private readonly PlayerMovement _player;
        private readonly Func<LevelRoom, Vector3> _arrival;
        private readonly List<LevelRoom> _rooms;

        // arrival: where in a room the player lands (on the floor; the command lifts it clear).
        public RoomTravelCommand(LevelLayout layout, Transform level, PlayerMovement player, Func<LevelRoom, Vector3> arrival)
        {
            _layout  = layout;
            _level   = level;
            _player  = player;
            _arrival = arrival;
            _rooms   = new List<LevelRoom>(layout.Rooms.Values);
            _rooms.Sort((a, b) => a.Node.Id.CompareTo(b.Node.Id));
        }

        public void Register(DevConsole console) =>
            console.Register("room", "[next|back|<id>|<name>]",
                "Go to a room of the level: the next or previous one, by id, or the next room with that name. No argument lists them",
                Run, example: "/room bar", complete: (_, i) => i == 0 ? Options() : null);

        private string Run(string[] args)
        {
            if (args.Length == 0) return List();

            string query = string.Join(" ", args);
            LevelRoom room = Find(query);
            if (room == null) return $"No room '{query}'. /room lists them.";

            _player.Teleport(_arrival(room) + Vector3.up);
            return $"Room {room.Node.Id}: {RoomLabel.Name(room)}";
        }

        private LevelRoom Find(string query)
        {
            int here = IndexOfCurrent();
            if (query.Equals("next", StringComparison.OrdinalIgnoreCase)) return At(here + 1);
            if (query.Equals("back", StringComparison.OrdinalIgnoreCase)) return At(here - 1);
            if (int.TryParse(query, out int id)) return _layout.Rooms.TryGetValue(id, out LevelRoom byId) ? byId : null;

            // A whole name first, so "bar" means the Bar and not the Barracks; then part of one.
            string wanted = Normalize(query);
            return NextNamed(here, name => name == wanted) ?? NextNamed(here, name => name.Contains(wanted));
        }

        // From the room after this one (the first, from a corridor), so asking again moves on
        // to the next room of that name.
        private LevelRoom NextNamed(int here, Predicate<string> matches)
        {
            for (int step = 1; step <= _rooms.Count; step++)
            {
                LevelRoom room = _rooms[(here + step) % _rooms.Count];
                if (matches(Normalize(RoomLabel.Name(room)))) return room;
            }
            return null;
        }

        private LevelRoom At(int index) => index >= 0 && index < _rooms.Count ? _rooms[index] : null;

        // -1 in a corridor or outside the level.
        private int IndexOfCurrent()
        {
            LevelRoom current = _layout.RoomAtLocal(_level.InverseTransformPoint(_player.transform.position));
            return current != null ? _rooms.IndexOf(current) : -1;
        }

        private string List()
        {
            var text = new StringBuilder();
            int listed = Mathf.Min(_rooms.Count, ListedAtMost);
            for (int i = 0; i < listed; i++)
                text.Append(i > 0 ? ", " : string.Empty).Append(_rooms[i].Node.Id).Append(' ').Append(RoomLabel.Name(_rooms[i]));
            if (_rooms.Count > listed) text.Append($", … {_rooms.Count - listed} more (/room <id>)");
            return text.ToString();
        }

        private IEnumerable<string> Options()
        {
            yield return "next";
            yield return "back";
            var names = new HashSet<string>();
            foreach (LevelRoom room in _rooms)
            {
                string name = Normalize(RoomLabel.Name(room));
                if (names.Add(name)) yield return name;
            }
        }

        private static string Normalize(string text) => text.Replace(" ", string.Empty).ToLowerInvariant();
    }
}
