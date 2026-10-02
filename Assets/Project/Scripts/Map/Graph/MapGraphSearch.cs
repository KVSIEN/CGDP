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
                                                     MapConnection ignored = null)
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
                    if (connection == ignored || !connection.Connects(current)) continue;
                    if (!includeShortcuts && connection.Type == ConnectionType.Shortcut) continue;

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
    }
}
