using System.Collections.Generic;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Level
{
    // The home zone of each room's enemies: every room of the same faction reachable from it
    // through rooms that faction also holds (a territory), so a faction's enemies range over
    // their whole territory and stop at its border. A room with no faction is a territory of
    // its own.
    public class TerritoryZones
    {
        private readonly Dictionary<int, HomeZone> _zones = new();

        public TerritoryZones(LevelLayout layout, Transform space)
        {
            Dictionary<int, int> group = GroupRooms(layout);
            var rects = new Dictionary<int, List<Rect>>();
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                int root = group[room.Node.Id];
                if (!rects.TryGetValue(root, out List<Rect> list)) rects[root] = list = new List<Rect>();
                list.Add(layout.RoomLocalRect(room));
            }

            var byRoot = new Dictionary<int, HomeZone>();
            foreach (KeyValuePair<int, List<Rect>> pair in rects) byRoot[pair.Key] = new HomeZone(pair.Value, space);
            foreach (LevelRoom room in layout.Rooms.Values) _zones[room.Node.Id] = byRoot[group[room.Node.Id]];
        }

        public HomeZone ZoneOf(LevelRoom room) => _zones[room.Node.Id];

        // Room id → the id its territory is known by (union-find over the corridors between
        // rooms of one faction).
        private static Dictionary<int, int> GroupRooms(LevelLayout layout)
        {
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
            }

            var group = new Dictionary<int, int>();
            foreach (int id in layout.Rooms.Keys) group[id] = Find(id);
            return group;
        }
    }
}
