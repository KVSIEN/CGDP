using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Turns some entrances to optional areas into Locked or Secret connections. An
    // entrance is a connection that is the only way into part of the map holding
    // neither the Boss nor the Exit, so a gate on it can't be walked around. Runs after
    // every other link, so nothing added later can open a second way in.
    internal class MapGatePlacer
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapGatePlacer(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        private MapGraph        Graph => _context.Graph;
        private MapGateSettings Gates => _context.Settings.Gates;

        // Shallowest entrances first, and none inside an area already gated, so a gate
        // sits at the mouth of an area rather than nested deep inside another.
        public void Place()
        {
            if (Gates.MaxGates <= 0 || (Gates.LockedChance <= 0f && Gates.SecretChance <= 0f)) return;

            List<Entrance> entrances = FindEntrances();
            entrances.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            var gated  = new HashSet<int>();
            int placed = 0;
            foreach (Entrance entrance in entrances)
            {
                if (placed == Gates.MaxGates) return;
                if (gated.Contains(entrance.Outside)) continue;

                ConnectionType type = RollGate();
                if (type == ConnectionType.Normal) continue;

                entrance.Connection.Type = type;
                gated.UnionWith(entrance.Area);
                placed++;
            }
        }

        private ConnectionType RollGate()
        {
            if (_random.Chance(Gates.LockedChance)) return ConnectionType.Locked;
            if (_random.Chance(Gates.SecretChance)) return ConnectionType.Secret;
            return ConnectionType.Normal;
        }

        // The room on Start's side must be Start or a junction, so the gate sits where the
        // area branches off rather than partway down a chain of rooms. Graphs are small,
        // so one search per connection is simpler than a bridge-finding algorithm.
        private List<Entrance> FindEntrances()
        {
            var entrances = new List<Entrance>();
            int startId = Graph.FindFirst(MapNodeType.Start).Id;
            int exitId  = Graph.FindFirst(MapNodeType.Exit).Id;
            Dictionary<int, int> depths = MapGraphSearch.Distances(Graph, startId, includeShortcuts: true);

            foreach (MapConnection connection in Graph.Connections)
            {
                if (connection.Type != ConnectionType.Normal) continue;

                Dictionary<int, int> reach = MapGraphSearch.Distances(Graph, startId, includeShortcuts: true, ignored: connection);
                if (!reach.ContainsKey(exitId)) continue;

                int inside = !reach.ContainsKey(connection.A) ? connection.A
                           : !reach.ContainsKey(connection.B) ? connection.B
                           : -1;
                if (inside < 0) continue;

                int outside = connection.Other(inside);
                if (outside != startId && Graph.Degree(outside) < 3) continue;

                var area = new List<int>();
                foreach (MapNode node in Graph.Nodes)
                    if (!reach.ContainsKey(node.Id))
                        area.Add(node.Id);

                entrances.Add(new Entrance(connection, outside, depths[outside], area));
            }
            return entrances;
        }

        private class Entrance
        {
            public Entrance(MapConnection connection, int outside, int depth, List<int> area)
            {
                Connection = connection;
                Outside    = outside;
                Depth      = depth;
                Area       = area;
            }

            public MapConnection Connection { get; }
            // The room on Start's side of the gate.
            public int           Outside    { get; }
            public int           Depth      { get; }
            // The rooms behind the gate.
            public List<int>     Area       { get; }
        }
    }
}
