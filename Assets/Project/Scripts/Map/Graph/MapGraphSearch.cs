using System;
using System.Collections.Generic;

namespace CGD.Map
{
    // Breadth-first distances over a MapGraph, shared by the analysis, the generator and
    // the validator.
    public static class MapGraphSearch
    {
        // Connections from `from` to every node it can reach. `ignored` is treated as
        // missing, which tells whether one connection is the only way into part of the map.
        public static Dictionary<int, int> Distances(MapGraph graph, int from, bool includeShortcuts,
                                                     MapConnection ignored = null) =>
            Distances(graph, from, c => c != ignored && (includeShortcuts || c.Type != ConnectionType.Shortcut));

        // Same, following only the connections `canPass` accepts.
        public static Dictionary<int, int> Distances(MapGraph graph, int from, Predicate<MapConnection> canPass)
        {
            var distances = new Dictionary<int, int>();
            if (!graph.TryGetNode(from, out _)) return distances;

            var frontier = new Queue<int>();
            distances[from] = 0;
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();

                foreach (MapConnection connection in graph.Connections)
                {
                    if (!connection.Connects(current) || !canPass(connection)) continue;

                    int next = connection.Other(current);
                    if (distances.ContainsKey(next)) continue;

                    distances[next] = distances[current] + 1;
                    frontier.Enqueue(next);
                }
            }
            return distances;
        }

        // Fewest connections from Start to Boss, shortcuts included, or int.MaxValue
        // when either is missing or the Boss can't be reached.
        public static int BossDistance(MapGraph graph)
        {
            MapNode start = graph.FindFirst(MapNodeType.Start);
            MapNode boss  = graph.FindFirst(MapNodeType.Boss);
            if (start == null || boss == null) return int.MaxValue;

            return Distances(graph, start.Id, includeShortcuts: true).TryGetValue(boss.Id, out int distance)
                ? distance
                : int.MaxValue;
        }

        // Plays the map from `from`: walks everywhere not behind a Locked connection,
        // opens each Locked connection whose door and key room are both reached, and
        // repeats. Returns the Locked connections that never open — a missing key, a key
        // behind its own door, or keys locked behind each other.
        public static List<MapConnection> UnopenableLocks(MapGraph graph, int from)
        {
            var opened = new HashSet<MapConnection>();

            while (true)
            {
                Dictionary<int, int> reached = Distances(graph, from, c => c.Type != ConnectionType.Locked || opened.Contains(c));
                bool openedAny = false;

                foreach (MapConnection connection in graph.Connections)
                {
                    if (connection.Type != ConnectionType.Locked || opened.Contains(connection)) continue;
                    if (!connection.HasKey || !reached.ContainsKey(connection.KeyNodeId)) continue;
                    if (!reached.ContainsKey(connection.A) && !reached.ContainsKey(connection.B)) continue;

                    opened.Add(connection);
                    openedAny = true;
                }

                if (openedAny) continue;

                var closed = new List<MapConnection>();
                foreach (MapConnection connection in graph.Connections)
                    if (connection.Type == ConnectionType.Locked && !opened.Contains(connection))
                        closed.Add(connection);
                return closed;
            }
        }
    }
}
