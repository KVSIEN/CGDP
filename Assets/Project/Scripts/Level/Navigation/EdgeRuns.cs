using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Tile edges gathered into straight runs: edges facing the same way on one line, side by
    // side, cut at most `maxRun` tiles long, so a long wall or ledge gets several links across it
    // (VaultLinkPlanner, DropLinkPlanner). Runs come out in the order their lines were first met,
    // each from its lowest tile up.
    public static class EdgeRuns
    {
        public readonly struct Run
        {
            public Run(Vector2Int side, int across, int first, int last)
            {
                Side   = side;
                Across = across;
                First  = first;
                Last   = last;
            }

            public Vector2Int Side   { get; }   // the step across the edge, from the run's tiles to the others
            public int        Across { get; }   // the run's tiles' coordinate across the line (x for a line running along y)
            public int        First  { get; }   // its first and last tiles' coordinate along the line
            public int        Last   { get; }
        }

        // `edges`: each tile with the side its edge is on, and a group that keeps runs apart even
        // on one line (a ledge's level, say); edges of a group may come in any order.
        public static List<Run> Of(IEnumerable<(Vector2Int tile, Vector2Int side, int group)> edges, int maxRun)
        {
            var lines = new Dictionary<(Vector2Int Side, int Across, int Group), List<int>>();
            foreach ((Vector2Int tile, Vector2Int side, int group) in edges)
            {
                bool vertical = side.x != 0;   // an edge between x and x + 1, on a line running along y
                var key = (side, vertical ? tile.x : tile.y, group);
                if (!lines.TryGetValue(key, out List<int> along)) lines[key] = along = new List<int>();
                along.Add(vertical ? tile.y : tile.x);
            }

            var runs = new List<Run>();
            foreach (KeyValuePair<(Vector2Int Side, int Across, int Group), List<int>> line in lines)
            {
                List<int> along = line.Value;
                along.Sort();
                int start = 0;
                for (int i = 1; i <= along.Count; i++)
                {
                    bool ends = i == along.Count || along[i] != along[i - 1] + 1 || i - start == maxRun;
                    if (!ends) continue;
                    runs.Add(new Run(line.Key.Side, line.Key.Across, along[start], along[i - 1]));
                    start = i;
                }
            }
            return runs;
        }

        // The middle of the run's tiles and of the tiles across from them, in tile units.
        public static (Vector2 from, Vector2 to) Middles(Run run)
        {
            float middle = (run.First + run.Last + 1) * 0.5f;
            bool vertical = run.Side.x != 0;
            Vector2 from = vertical ? new Vector2(run.Across + 0.5f, middle) : new Vector2(middle, run.Across + 0.5f);
            return (from, from + (Vector2)run.Side);
        }

        public static int Length(Run run) => run.Last - run.First + 1;
    }
}
