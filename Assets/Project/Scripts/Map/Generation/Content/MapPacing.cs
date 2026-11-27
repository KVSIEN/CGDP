using System.Collections.Generic;

namespace CGD.Map
{
    // The rhythm the main path should have, read off any graph (MapPacingSettings holds the
    // numbers). Its peaks: the fight that tops its stretch of map, read from the rooms' intensity
    // (PeakIntensity), and the sawtooth they make: no more than MaxFightsWithoutPeak fights in a
    // row with no peak among them, a peak somewhere before the boss once there are two fights or
    // more, and a climb: the strongest peak not in the first third with only weaker ones after it.
    // Its rests: no more than MaxCalmInARow calm rooms in a row. The generator makes a map that
    // follows it (the type assigner caps the calm rooms, the intensity painter raises the peaks,
    // Raise); the validator and the map director read the same rules off any graph, hand-made
    // ones included, and report where it doesn't.
    public static class MapPacing
    {
        // How far a raised peak is put above the strongest fight beside it: clear of the
        // painter's jitter, so it reads as the top of its stretch.
        public const float Margin = 0.05f;

        // The share of the way to the boss a strongest peak may not stand in alone.
        public const float EarlyShare = 1f / 3f;

        // A reachable fight at least `peakIntensity`, outranked by no fight beside it: ties go to
        // the deeper room, then the lower id, so two peaks never stand side by side.
        public static bool IsPeak(MapGraph graph, MapGraphAnalysis analysis, MapNode node, float peakIntensity, List<int> neighbours)
        {
            if (!node.Type.IsFight() || !analysis.IsReachable(node.Id) || node.Intensity < peakIntensity) return false;

            graph.GetNeighbors(node.Id, neighbours);
            foreach (int id in neighbours)
                if (graph.TryGetNode(id, out MapNode other) && other.Type.IsFight() && analysis.IsReachable(id) && Outranks(analysis, other, node))
                    return false;
            return true;
        }

        // Whether `a` is the stronger candidate for a peak than `b`.
        public static bool Outranks(MapGraphAnalysis analysis, MapNode a, MapNode b)
        {
            if (a.Intensity != b.Intensity) return a.Intensity > b.Intensity;
            int depthA = analysis.Depth(a.Id), depthB = analysis.Depth(b.Id);
            if (depthA != depthB) return depthA > depthB;
            return a.Id < b.Id;
        }

        // The main path's rooms between the Start and the Boss (or the Exit), less the room right
        // before the Boss: that one leads into the boss fight and is never its stretch's peak.
        public static List<int> PacedPath(MapGraph graph, MapGraphAnalysis analysis)
        {
            var path = new List<int>();
            IReadOnlyList<int> main = analysis.MainPath;
            for (int i = 1; i < main.Count - 1; i++)
            {
                if (graph.TryGetNode(main[i], out MapNode node) && node.Type == MapNodeType.Boss)
                {
                    if (path.Count > 0) path.RemoveAt(path.Count - 1);
                    break;
                }
                path.Add(main[i]);
            }
            return path;
        }

        // Runs of fights in a row along `path` with no peak among them, longer than `maxRun`.
        public static List<List<int>> PeaklessRuns(MapGraph graph, MapGraphAnalysis analysis, IReadOnlyList<int> path, float peakIntensity, int maxRun)
        {
            var runs = new List<List<int>>();
            if (maxRun <= 0) return runs;

            var neighbours = new List<int>();
            var run = new List<int>();
            for (int i = 0; i <= path.Count; i++)
            {
                MapNode node = null;
                bool fight = i < path.Count && graph.TryGetNode(path[i], out node) && node.Type.IsFight();
                if (fight && !IsPeak(graph, analysis, node, peakIntensity, neighbours))
                {
                    run.Add(node.Id);
                    continue;
                }
                if (run.Count > maxRun) runs.Add(run);
                run = new List<int>();
            }
            return runs;
        }

        // The fights along `path` when there are two or more and none is a peak; else none.
        public static List<int> PeaklessFights(MapGraph graph, MapGraphAnalysis analysis, IReadOnlyList<int> path, float peakIntensity)
        {
            var fights = new List<int>();
            var neighbours = new List<int>();
            foreach (int id in path)
            {
                if (!graph.TryGetNode(id, out MapNode node) || !node.Type.IsFight()) continue;
                if (IsPeak(graph, analysis, node, peakIntensity, neighbours)) return new List<int>();
                fights.Add(id);
            }
            return fights.Count >= 2 ? fights : new List<int>();
        }

        // The peak along `path` with the highest intensity (the first of equals) when it stands in
        // the first EarlyShare of the way, so every later peak is weaker; else -1.
        public static int EarlyStrongestPeak(MapGraph graph, MapGraphAnalysis analysis, IReadOnlyList<int> path, float peakIntensity)
        {
            var neighbours = new List<int>();
            MapNode strongest = null;
            foreach (int id in path)
                if (graph.TryGetNode(id, out MapNode node) && IsPeak(graph, analysis, node, peakIntensity, neighbours)
                    && (strongest == null || node.Intensity > strongest.Intensity))
                    strongest = node;
            return strongest != null && analysis.Progress(strongest.Id) < EarlyShare ? strongest.Id : -1;
        }

        // Runs of calm rooms in a row along the main path, longer than `maxRun`; the Start, Boss
        // and Exit end a run and are never in one.
        public static List<List<int>> CalmRuns(MapGraph graph, MapGraphAnalysis analysis, int maxRun)
        {
            var runs = new List<List<int>>();
            if (maxRun <= 0) return runs;

            IReadOnlyList<int> main = analysis.MainPath;
            var run = new List<int>();
            for (int i = 0; i <= main.Count; i++)
            {
                if (i < main.Count && graph.TryGetNode(main[i], out MapNode node) && !node.Type.IsStructural() && !node.Type.IsFight())
                {
                    run.Add(node.Id);
                    continue;
                }
                if (run.Count > maxRun) runs.Add(run);
                run = new List<int>();
            }
            return runs;
        }

        // Raises peaks into `graph` until its main path follows the sawtooth: each peakless run
        // and a peakless way to the boss gets its strongest fight that can peak (beside no peak,
        // not in `keep`) lifted to the peak intensity, and above every fight beside it by Margin;
        // then a strongest peak standing early gets a later fight lifted above it. Only
        // intensities change, and none drawn at random, so later layers roll as before.
        public static void Raise(MapGraph graph, MapPacingSettings pacing, ICollection<int> keep)
        {
            if (pacing.MaxFightsWithoutPeak <= 0) return;
            var analysis = new MapGraphAnalysis(graph);
            List<int> path = PacedPath(graph, analysis);
            float peak = pacing.PeakIntensity;

            for (int guard = path.Count; guard > 0; guard--)
            {
                List<List<int>> runs = PeaklessRuns(graph, analysis, path, peak, pacing.MaxFightsWithoutPeak);
                List<int> stretch = runs.Count > 0 ? runs[0] : PeaklessFights(graph, analysis, path, peak);
                if (stretch.Count == 0 || !TryRaise(graph, analysis, stretch, peak, keep)) break;
            }

            int early = EarlyStrongestPeak(graph, analysis, path, peak);
            if (early < 0 || !graph.TryGetNode(early, out MapNode top)) return;
            var later = new List<int>();
            foreach (int id in path)
                if (analysis.Progress(id) >= EarlyShare) later.Add(id);
            TryRaise(graph, analysis, later, System.Math.Min(1f, top.Intensity + Margin), keep, peak);
        }

        // The strongest of `candidates` that can be lifted into a peak, lifted to at least `lift`
        // (a peak at `peak`, the map's peak intensity; `lift` defaults to it).
        private static bool TryRaise(MapGraph graph, MapGraphAnalysis analysis, List<int> candidates, float lift, ICollection<int> keep, float peak = -1f)
        {
            if (peak < 0f) peak = lift;
            var neighbours = new List<int>();
            var order = new List<MapNode>();
            foreach (int id in candidates)
                if (graph.TryGetNode(id, out MapNode node) && node.Type.IsFight() && (keep == null || !keep.Contains(id))
                    && (IsPeak(graph, analysis, node, peak, neighbours) || !BesidePeak(graph, analysis, id, peak, neighbours)))
                    order.Add(node);
            order.Sort((a, b) => Outranks(analysis, a, b) ? -1 : Outranks(analysis, b, a) ? 1 : 0);

            foreach (MapNode node in order)
            {
                float was = node.Intensity;
                node.Intensity = System.Math.Max(node.Intensity, System.Math.Min(1f, System.Math.Max(lift, StrongestFightBeside(graph, analysis, node, neighbours) + Margin)));
                if (IsPeak(graph, analysis, node, peak, neighbours) && node.Intensity >= lift) return true;
                node.Intensity = was;
            }
            return false;
        }

        private static bool BesidePeak(MapGraph graph, MapGraphAnalysis analysis, int id, float peak, List<int> neighbours)
        {
            var around = new List<int>();
            graph.GetNeighbors(id, around);
            foreach (int other in around)
                if (graph.TryGetNode(other, out MapNode node) && IsPeak(graph, analysis, node, peak, neighbours)) return true;
            return false;
        }

        private static float StrongestFightBeside(MapGraph graph, MapGraphAnalysis analysis, MapNode node, List<int> neighbours)
        {
            float strongest = 0f;
            graph.GetNeighbors(node.Id, neighbours);
            foreach (int id in neighbours)
                if (graph.TryGetNode(id, out MapNode other) && other.Type.IsFight() && analysis.IsReachable(id))
                    strongest = System.Math.Max(strongest, other.Intensity);
            return strongest;
        }
    }
}
