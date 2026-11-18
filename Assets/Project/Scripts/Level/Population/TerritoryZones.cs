using System.Collections.Generic;
using UnityEngine;
using CGD.Enemies;
using CGD.Map;

namespace CGD.Level
{
    // The home zone of each room's enemies: every room of the same faction reachable from it
    // through rooms that faction also holds (a territory) and the corridors between them, so a
    // faction's enemies range over their whole territory and stop at its border. A room with no faction is a territory of
    // its own.
    public class TerritoryZones
    {
        private readonly Dictionary<int, HomeZone> _zones = new();

        public TerritoryZones(LevelLayout layout, Transform space)
        {
            Dictionary<int, int> group = GroupRooms(layout, out List<(int Root, MapConnection Connection)> links);
            var rects = new Dictionary<int, List<Rect>>();
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                int root = group[room.Node.Id];
                if (!rects.TryGetValue(root, out List<Rect> list)) rects[root] = list = new List<Rect>();
                list.Add(layout.RoomLocalRect(room));
            }

            // A territory's own corridors are part of it, or its enemies couldn't cross to its next room.
            foreach ((int root, MapConnection connection) in links)
                foreach (Vector2Int tile in layout.CorridorOf(connection))
                    rects[root].Add(layout.TileLocalRect(tile));

            var byRoot = new Dictionary<int, HomeZone>();
            foreach (KeyValuePair<int, List<Rect>> pair in rects) byRoot[pair.Key] = new HomeZone(pair.Value, space);
            foreach (LevelRoom room in layout.Rooms.Values) _zones[room.Node.Id] = byRoot[group[room.Node.Id]];
        }

        public HomeZone ZoneOf(LevelRoom room) => _zones[room.Node.Id];

        // Room id → the id its territory is known by (union-find over the corridors between
        // rooms of one faction).
        private static Dictionary<int, int> GroupRooms(LevelLayout layout, out List<(int Root, MapConnection Connection)> links)
        {
            var joined = new HashSet<MapConnection>();
            var parent = new Dictionary<int, int>();
            foreach (int id in layout.Rooms.Keys) parent[id] = id;

            int Find(int id)
            {
                while (parent[id] != id) id = parent[id] = parent[parent[id]];
                return id;
            }

            foreach (LevelDoorway doorway in layout.Doorways)
            {
                if (!layout.Rooms.TryGetValue(doorway.Connection.A, out LevelRoom a)) continue;
                if (!layout.Rooms.TryGetValue(doorway.Connection.B, out LevelRoom b)) continue;
                if (!a.Node.HasFaction || a.Node.Faction != b.Node.Faction) continue;
                parent[Find(a.Node.Id)] = Find(b.Node.Id);
                joined.Add(doorway.Connection);
            }

            var group = new Dictionary<int, int>();
            foreach (int id in layout.Rooms.Keys) group[id] = Find(id);
            links = new List<(int, MapConnection)>();
            foreach (MapConnection connection in joined) links.Add((group[connection.A], connection));
            return group;
        }
    }
}
