using System.Collections.Generic;
using CGD.Map;

namespace CGD.Level
{
    // Gives every room of a map its beat in the level's pacing (RoomBeat), from the map graph
    // alone, before anything is built, so the rooms can be made for it: their size, how much
    // cover they get, how many enemies, what is put down in them, where reinforcements go.
    //
    // The graph rises in intensity toward the boss, never strings too many fights together and
    // comes out of the generator with its peaks raised (MapPeaks); the director reads that
    // curve the way an encounter designer would pace it (Left 4 Dead's build-up, peak and
    // relax; a tension curve over rooms). Its beats:
    //   - Climax: the Boss room.
    //   - Rest: a room with no fight planned (MapNodeTypeExtensions.IsFight).
    //   - Peak: a fight MapPeaks.IsPeak calls the top of its stretch (MapPacingSettings.PeakIntensity).
    //   - Relief: a fight entered from a Peak (a neighbour nearer the Start), the breather after it.
    //   - Approach: the fight right before the Boss on the main path; a calm room there is
    //     already the breather and stays Rest.
    //   - Build: every other fight.
    // Rooms the Start can't reach get None. Pure: the same graph gives the same beats.
    //
    // It changes no intensity: what the pacing asks of the map is the generator's to make. A
    // graph that misses it (a hand-made one, or a pinned room standing in the way) is reported,
    // as the validator reports it in the map editor: a run of more than MaxFightsWithoutPeak
    // fights with no peak, a way to the boss with no peak, more than CalmStretch calm rooms in a
    // row, or a strongest peak in the first third of the way with only weaker ones after it.
    // Every report is a DirectorFinding on the direction.
    //
    // Each room's size is scaled by its beat and, for a fight, by how far into the map it lies:
    // small chambers early, bigger ones toward the boss, as Hades grows its chambers through a
    // biome, so the space grows with the fights in it. The set pieces are placed first and no
    // ordinary fight beside one is given more floor than it (PlacementOrder, AreaLimit), so the
    // big fight of a stretch is its biggest room from the start.
    public static class MapDirector
    {
        // Every room's beat and size; nothing without settings, so nothing changes. `pacing`: the
        // map content's (null = MapPacingSettings.Default).
        public static MapDirection Plan(MapGraph graph, MapPacingSettings pacing, MapDirectorSettings settings)
        {
            if (settings == null || graph == null || graph.Nodes.Count == 0) return MapDirection.None;

            var findings = new List<DirectorFinding>();
            Dictionary<int, RoomBeat> beats = Plan(graph, pacing ?? MapPacingSettings.Default, settings.CalmStretch, findings);
            var analysis = new MapGraphAnalysis(graph);
            var sizes = new Dictionary<int, float>();
            foreach (MapNode node in graph.Nodes)
            {
                RoomBeat beat = beats[node.Id];
                float size = settings.TuningFor(beat).SizeScale;
                if (IsFightBeat(beat)) size *= settings.SizeAt(analysis.Progress(node.Id));
                sizes[node.Id] = size;
            }
            return new MapDirection(beats, sizes, findings);
        }

        private static bool IsFightBeat(RoomBeat beat) => beat is RoomBeat.Build or RoomBeat.Peak or RoomBeat.Relief or RoomBeat.Approach;

        public static bool IsSetPiece(RoomBeat beat) => beat is RoomBeat.Peak or RoomBeat.Climax;

        // The order the rooms are placed in: the set pieces (peaks and the boss room) first, so
        // they take their cells before the fights beside them; otherwise the graph's own order.
        public static List<MapNode> PlacementOrder(MapGraph graph, MapDirection direction)
        {
            var order = new List<MapNode>(graph.Nodes);
            var rank = new Dictionary<MapNode, int>();
            for (int i = 0; i < order.Count; i++) rank[order[i]] = (IsSetPiece(direction.BeatOf(order[i].Id)) ? 0 : order.Count) + i;
            order.Sort((a, b) => rank[a].CompareTo(rank[b]));
            return order;
        }

        // The most floor tiles an ordinary fight may be given beside the set pieces already placed
        // (`placed`: their floor tiles): the smallest of theirs, so the set-piece fight is the
        // biggest room around it. int.MaxValue for any other room, or with none placed beside it.
        public static int AreaLimit(MapGraph graph, int id, MapDirection direction, IReadOnlyDictionary<int, int> placed, List<int> neighbours)
        {
            if (!(direction.BeatOf(id) is RoomBeat.Build or RoomBeat.Relief or RoomBeat.Approach)) return int.MaxValue;

            int limit = int.MaxValue;
            graph.GetNeighbors(id, neighbours);
            foreach (int other in neighbours)
                if (IsSetPiece(direction.BeatOf(other)) && placed.TryGetValue(other, out int tiles))
                    limit = System.Math.Min(limit, tiles);
            return limit;
        }

        // `calmStretch`: the longest run of calm rooms the main path may have before it is reported
        // (0 = not checked). `findings`: what was found, when given.
        public static Dictionary<int, RoomBeat> Plan(MapGraph graph, MapPacingSettings pacing, int calmStretch = 0, List<DirectorFinding> findings = null)
        {
            var beats = new Dictionary<int, RoomBeat>();
            if (graph == null || graph.Nodes.Count == 0) return beats;

            var analysis = new MapGraphAnalysis(graph);
            var neighbours = new List<int>();
            foreach (MapNode node in graph.Nodes)
            {
                if (!analysis.IsReachable(node.Id)) beats[node.Id] = RoomBeat.None;
                else if (node.Type == MapNodeType.Boss) beats[node.Id] = RoomBeat.Climax;
                else if (!node.Type.IsFight()) beats[node.Id] = RoomBeat.Rest;
                else beats[node.Id] = MapPeaks.IsPeak(graph, analysis, node, pacing.PeakIntensity, neighbours) ? RoomBeat.Peak : RoomBeat.Build;
            }

            MarkReliefs(graph, analysis, beats, neighbours);
            MarkApproach(graph, analysis, beats);
            if (findings == null) return beats;

            ReportPeaklessPacing(graph, analysis, pacing, findings);
            if (calmStretch > 0) ReportCalmStretches(analysis, beats, calmStretch, findings);
            ReportEarlyPeak(graph, analysis, beats, findings);
            return beats;
        }

        private static void MarkReliefs(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, List<int> neighbours)
        {
            foreach (MapNode node in graph.Nodes)
                if (beats[node.Id] == RoomBeat.Build && FollowsPeak(graph, analysis, node, beats, neighbours))
                    beats[node.Id] = RoomBeat.Relief;
        }

        private static void ReportPeaklessPacing(MapGraph graph, MapGraphAnalysis analysis, MapPacingSettings pacing, List<DirectorFinding> findings)
        {
            if (pacing.MaxFightsWithoutPeak <= 0) return;
            List<int> path = MapPeaks.PacedPath(graph, analysis);
            foreach (List<int> run in MapPeaks.PeaklessRuns(graph, analysis, path, pacing.PeakIntensity, pacing.MaxFightsWithoutPeak))
                findings.Add(new DirectorFinding(DirectorCheck.FlatStretch, run[0],
                    $"{run.Count} fights in a row (#{run[0]} to #{run[run.Count - 1]}) with no peak among them"));

            List<int> fights = MapPeaks.PeaklessFights(graph, analysis, path, pacing.PeakIntensity);
            if (fights.Count > 0)
                findings.Add(new DirectorFinding(DirectorCheck.NoPeak, fights[0], $"no peak among the {fights.Count} fights on the way to the boss"));
        }

        // Calm rooms in a row between the Start and the Exit (neither counts).
        private static void ReportCalmStretches(MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, int calmStretch, List<DirectorFinding> findings)
        {
            if (findings == null) return;
            IReadOnlyList<int> main = analysis.MainPath;
            int first = -1, count = 0;
            for (int i = 1; i < main.Count; i++)
            {
                if (i < main.Count - 1 && beats[main[i]] == RoomBeat.Rest)
                {
                    if (count++ == 0) first = main[i];
                    continue;
                }
                if (count > calmStretch)
                    findings.Add(new DirectorFinding(DirectorCheck.CalmStretch, first, $"{count} calm rooms in a row on the way to the boss, from #{first}"));
                count = 0;
            }
        }

        // A sawtooth should climb: the strongest peak early, with only weaker ones after it, falls away.
        private static void ReportEarlyPeak(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, List<DirectorFinding> findings)
        {
            if (findings == null) return;
            MapNode strongest = null;
            foreach (int id in analysis.MainPath)
                if (beats[id] == RoomBeat.Peak && graph.TryGetNode(id, out MapNode node) && (strongest == null || node.Intensity > strongest.Intensity))
                    strongest = node;
            if (strongest == null || analysis.Progress(strongest.Id) >= 1f / 3f) return;

            findings.Add(new DirectorFinding(DirectorCheck.EarlyPeak, strongest.Id,
                $"the strongest peak, #{strongest.Id}, comes in the first third of the way and every later one is weaker"));
        }

        private static bool FollowsPeak(MapGraph graph, MapGraphAnalysis analysis, MapNode node, Dictionary<int, RoomBeat> beats, List<int> neighbours)
        {
            int depth = analysis.Depth(node.Id);
            graph.GetNeighbors(node.Id, neighbours);
            foreach (int id in neighbours)
                if (beats.TryGetValue(id, out RoomBeat beat) && beat == RoomBeat.Peak && analysis.Depth(id) < depth)
                    return true;
            return false;
        }

        private static void MarkApproach(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats)
        {
            IReadOnlyList<int> path = analysis.MainPath;
            for (int i = 1; i < path.Count; i++)
            {
                if (!graph.TryGetNode(path[i], out MapNode boss) || boss.Type != MapNodeType.Boss) continue;
                if (graph.TryGetNode(path[i - 1], out MapNode before) && before.Type.IsFight()) beats[before.Id] = RoomBeat.Approach;
                return;
            }
        }
    }
}
