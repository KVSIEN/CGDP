using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // Locks and secrets, in two kinds. Progression locks sit on the main path itself, so
    // reaching the Boss means finding their keys first; the layout's own links around each
    // one are cut, and they are placed before any loop or shortcut, which then stay on one
    // side of each. Optional gates turn some
    // entrances to optional areas into Locked or Secret connections — an entrance is a
    // connection that is the only way into part of the map holding neither the Boss nor
    // the Exit, so a gate on it can't be walked around; they come after every other link,
    // so nothing added later can open a second way in. Every Locked connection then gets
    // its lock kind and key rooms (one keycard, or a terminal in each of several rooms).
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

        // Run before MapLinkBuilder, so no loop or shortcut it adds can lead around a lock.
        public void PlaceProgressionLocks()
        {
            if (Gates.ProgressionLocks.Max <= 0) return;
            int count = Gates.ProgressionLocks.Evaluate(_random);
            if (count <= 0) return;

            List<MapConnection> candidates = ProgressionCandidates();

            // Spread out: split the candidates, in path order, into one run per lock, with at
            // least one room between two locks so every section has somewhere to hide a key.
            int locks = Mathf.Min(count, candidates.Count);
            int previous = -2;
            for (int i = 0; i < locks; i++)
            {
                int from = Mathf.Max(i * candidates.Count / locks, previous + 2);
                int to   = (i + 1) * candidates.Count / locks;
                if (from >= to) continue;

                previous = _random.Range(from, to);
                MapConnection connection = candidates[previous];
                connection.Type = ConnectionType.Locked;
                _context.ProgressionLocks.Add(connection);
            }

            if (_context.ProgressionLocks.Count < count)
                _context.Warnings.Add($"Only {_context.ProgressionLocks.Count} of {count} progression locks fit — the main path is too short past Min Depth.");

            int startId = Graph.FindFirst(MapNodeType.Start).Id;
            foreach (MapConnection progressionLock in _context.ProgressionLocks)
                Isolate(progressionLock, startId);
            AssignZones();
        }

        // Run after every other link.
        public void Place()
        {
            if (Gates.Enabled) PlaceGates();
            PlaceKeys();
        }

        // Main path connections from MinDepth on, in path order. The one into the Boss room
        // is skipped: the main objectives already lock it.
        private List<MapConnection> ProgressionCandidates()
        {
            var candidates = new List<MapConnection>();
            var analysis = new MapGraphAnalysis(Graph);
            IReadOnlyList<int> path = analysis.MainPath;

            for (int i = Mathf.Max(1, Gates.MinDepth); i < path.Count - 1; i++)
            {
                if (_context.IsBossOrExit(path[i + 1])) break;

                MapConnection connection = Graph.GetConnection(path[i], path[i + 1]);
                if (connection != null && connection.Type == ConnectionType.Normal && !connection.OneWay)
                    candidates.Add(connection);
            }
            return candidates;
        }

        // Makes `gate` the only way between the rooms before it and those past it, by
        // removing every other link between the two sides. The far side is what the gate
        // leads to without coming back to the gate's depth or shallower: one connected
        // group holding the rest of the main path, Boss and Exit. Every other room keeps
        // its route back to a shallower room, so both sides stay whole; and since links
        // only ever join rooms one step apart in depth, just the links beside the gate go.
        private void Isolate(MapConnection gate, int startId)
        {
            Dictionary<int, int> depth = MapGraphSearch.Distances(Graph, startId, includeShortcuts: true);
            int near = depth[gate.A] <= depth[gate.B] ? gate.A : gate.B;
            int gateDepth = depth[near];

            var farSide = new HashSet<int>(MapGraphSearch.Distances(Graph, gate.Other(near),
                c => depth[c.A] > gateDepth && depth[c.B] > gateDepth).Keys);

            var bypasses = new List<MapConnection>();
            foreach (MapConnection connection in Graph.Connections)
                if (connection != gate && farSide.Contains(connection.A) != farSide.Contains(connection.B))
                    bypasses.Add(connection);

            foreach (MapConnection bypass in bypasses)
                Graph.Disconnect(bypass.A, bypass.B);
        }

        // A room's zone is how many progression locks cut it off from Start.
        private void AssignZones()
        {
            int startId = Graph.FindFirst(MapNodeType.Start).Id;
            var zones = new Dictionary<int, int>();
            foreach (MapConnection progressionLock in _context.ProgressionLocks)
            {
                Dictionary<int, int> around = MapGraphSearch.Distances(Graph, startId, includeShortcuts: true, ignored: progressionLock);
                foreach (MapNode node in Graph.Nodes)
                    if (!around.ContainsKey(node.Id))
                        zones[node.Id] = zones.TryGetValue(node.Id, out int zone) ? zone + 1 : 1;
            }

            foreach (KeyValuePair<int, int> entry in zones)
                _context.SetZone(entry.Key, entry.Value);
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

        // Progression locks first, from the shallowest. Keys and terminals only go where the
        // player can walk by then: through no gate except progression locks nearer Start
        // than this one, which are opened first. So none is ever shut away behind its own
        // door or another one. A gate with no room for its key goes back to Normal.
        private void PlaceKeys()
        {
            var analysis = new MapGraphAnalysis(Graph);
            var keyRooms = new HashSet<int>();

            for (int i = 0; i < _context.ProgressionLocks.Count; i++)
            {
                MapConnection gate = _context.ProgressionLocks[i];
                if (GiveKeys(gate, ShallowEnd(gate, analysis), analysis, keyRooms)) continue;

                gate.Type = ConnectionType.Normal;
                _context.ProgressionLocks.RemoveAt(i--);
                _context.Warnings.Add($"No room can hold the key for the progression lock #{gate.A}–#{gate.B} — it was left open.");
            }

            for (int i = _context.GatedAreas.Count - 1; i >= 0; i--)
            {
                MapGatedArea area = _context.GatedAreas[i];
                if (area.Gate.Type != ConnectionType.Locked) continue;
                if (GiveKeys(area.Gate, area.OutsideId, analysis, keyRooms)) continue;

                area.Gate.Type = ConnectionType.Normal;
                _context.GatedAreas.RemoveAt(i);
                _context.Warnings.Add($"No room can hold the key for the gate at #{area.OutsideId} — it was left open.");
            }
        }

        // Rolls the gate's lock kind and fills its key rooms; false when not even one fits.
        private bool GiveKeys(MapConnection gate, int outsideId, MapGraphAnalysis analysis, HashSet<int> keyRooms)
        {
            int depth = analysis.Depth(outsideId);
            Dictionary<int, int> open = MapGraphSearch.Distances(Graph, analysis.StartId,
                c => !c.IsGate || OpensBefore(c, depth, analysis));
            int zoneStart = ZoneStartDepth(depth, analysis);

            int parts = RollLock(gate);
            gate.ClearKeyRooms();
            for (int p = 0; p < parts; p++)
            {
                int key = PickKeyRoom(open, analysis, depth, zoneStart, keyRooms);
                if (key == NoRoom) break;
                gate.AddKeyRoom(key);
                keyRooms.Add(key);
            }

            if (!gate.HasKey) return false;

            // A terminal lock that found room for only one terminal is just a keycard door.
            if (gate.KeyNodeIds.Count == 1) gate.Lock = MapLockKind.Keycard;
            return true;
        }

        // A progression lock nearer Start than `depth` is opened before anything at `depth`.
        private bool OpensBefore(MapConnection connection, int depth, MapGraphAnalysis analysis) =>
            _context.ProgressionLocks.Contains(connection) && analysis.Depth(ShallowEnd(connection, analysis)) < depth;

        // First depth past the last progression lock before `depth`, so keys can be kept
        // past the door the player just opened rather than back near Start.
        private int ZoneStartDepth(int depth, MapGraphAnalysis analysis)
        {
            int zoneStart = 0;
            foreach (MapConnection progressionLock in _context.ProgressionLocks)
            {
                int lockDepth = analysis.Depth(ShallowEnd(progressionLock, analysis));
                if (lockDepth < depth) zoneStart = Mathf.Max(zoneStart, lockDepth + 1);
            }
            return zoneStart;
        }

        private static int ShallowEnd(MapConnection connection, MapGraphAnalysis analysis) =>
            analysis.Depth(connection.A) <= analysis.Depth(connection.B) ? connection.A : connection.B;

        // Picks the gate's lock kind; returns how many key rooms it needs.
        private int RollLock(MapConnection gate)
        {
            bool terminals = _random.Chance(Gates.TerminalLockChance);
            gate.Lock = terminals ? MapLockKind.Terminals : MapLockKind.Keycard;
            return terminals ? Mathf.Max(2, Gates.TerminalCount.Evaluate(_random)) : 1;
        }

        private int PickKeyRoom(Dictionary<int, int> open, MapGraphAnalysis analysis, int gateDepth, int zoneStart, HashSet<int> keyRooms)
        {
            // Least important preference first to drop: off the main path, then before the
            // gate, then past the last progression lock, then one key per room.
            for (int relaxed = 0; relaxed <= 4; relaxed++)
            {
                bool offPath    = Gates.KeyOffMainPath && relaxed < 1;
                bool beforeGate = Gates.KeyBeforeGate  && relaxed < 2;
                bool inZone     = relaxed < 3;
                bool unshared   = relaxed < 4;

                _candidates.Clear();
                foreach (int id in open.Keys)
                {
                    if (!Graph.TryGetNode(id, out MapNode node) || node.Type.IsStructural()) continue;
                    if (offPath && analysis.IsOnMainPath(id)) continue;
                    if (beforeGate && analysis.Depth(id) > gateDepth) continue;
                    if (inZone && analysis.Depth(id) < zoneStart) continue;
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
