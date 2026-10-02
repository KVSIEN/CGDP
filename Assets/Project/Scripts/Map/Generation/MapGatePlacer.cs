using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Turns some entrances to optional areas into Locked or Secret connections, then
    // gives every Locked one a key room. An entrance is a connection that is the only way
    // into part of the map holding neither the Boss nor the Exit, so a gate on it can't
    // be walked around. Runs after every other link, so nothing added later can open a
    // second way in.
    internal class MapGatePlacer
    {
        private const int NoRoom = -1;

        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;
        private readonly List<int>            _candidates = new();

        public MapGatePlacer(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        private MapGraph        Graph => _context.Graph;
        private MapGateSettings Gates => _context.Layout.Gates;

        public void Place()
        {
            if (!Gates.Enabled) return;

            PlaceGates();
            PlaceKeys();
        }

        // Shallowest entrances first, and none inside an area already gated, so a gate
        // sits at the mouth of an area rather than nested deep inside another.
        private void PlaceGates()
        {
            List<Entrance> entrances = FindEntrances();
            entrances.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            var gated = new HashSet<int>();
            foreach (Entrance entrance in entrances)
            {
                if (_context.GatedAreas.Count >= Gates.MaxGates + _context.Tuning.ExtraGates) return;
                if (entrance.Depth < Gates.MinDepth || gated.Contains(entrance.Outside)) continue;

                ConnectionType type = RollGate();
                if (type == ConnectionType.Normal) continue;

                entrance.Connection.Type = type;
                gated.UnionWith(entrance.Area);
                _context.GatedAreas.Add(new MapGatedArea(entrance.Connection, entrance.Outside, entrance.Area));
            }
        }

        private ConnectionType RollGate()
        {
            if (_random.Chance(Gates.LockedChance * _context.Tuning.LockedMultiplier)) return ConnectionType.Locked;
            if (_random.Chance(Gates.SecretChance * _context.Tuning.SecretMultiplier)) return ConnectionType.Secret;
            return ConnectionType.Normal;
        }

        // Keys only go where the player can walk without opening any gate, so no key is
        // ever shut away behind its own door or another one. Preferences relax one at a
        // time until a room qualifies; a gate with no room at all goes back to Normal.
        private void PlaceKeys()
        {
            int startId = Graph.FindFirst(MapNodeType.Start).Id;
            Dictionary<int, int> open = MapGraphSearch.Distances(Graph, startId, c => !c.IsGate);
            var analysis = new MapGraphAnalysis(Graph);
            var keyRooms = new HashSet<int>();

            for (int i = _context.GatedAreas.Count - 1; i >= 0; i--)
            {
                MapGatedArea area = _context.GatedAreas[i];
                if (area.Gate.Type != ConnectionType.Locked) continue;

                int key = PickKeyRoom(open, analysis, analysis.Depth(area.OutsideId), keyRooms);
                if (key == NoRoom)
                {
                    area.Gate.Type = ConnectionType.Normal;
                    _context.GatedAreas.RemoveAt(i);
                    _context.Warnings.Add($"No room can hold the key for the gate at #{area.OutsideId} — it was left open.");
                    continue;
                }

                area.Gate.KeyNodeId = key;
                keyRooms.Add(key);
            }
        }

        private int PickKeyRoom(Dictionary<int, int> open, MapGraphAnalysis analysis, int gateDepth, HashSet<int> keyRooms)
        {
            // Least important preference first to drop: off the main path, then before the
            // gate, then one key per room.
            for (int relaxed = 0; relaxed <= 3; relaxed++)
            {
                bool offPath    = Gates.KeyOffMainPath && relaxed < 1;
                bool beforeGate = Gates.KeyBeforeGate  && relaxed < 2;
                bool unshared   = relaxed < 3;

                _candidates.Clear();
                foreach (int id in open.Keys)
                {
                    if (!Graph.TryGetNode(id, out MapNode node) || node.Type.IsStructural()) continue;
                    if (offPath && analysis.IsOnMainPath(id)) continue;
                    if (beforeGate && analysis.Depth(id) > gateDepth) continue;
                    if (unshared && keyRooms.Contains(id)) continue;
                    _candidates.Add(id);
                }

                if (_candidates.Count > 0)
                {
                    // Dictionary order isn't part of the seed's contract; sorting keeps it deterministic.
                    _candidates.Sort();
                    return _random.Pick(_candidates);
                }
            }
            return NoRoom;
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
