using System.Collections.Generic;
using CGD.Map;

namespace CGD.Level
{
    // Gives every room of a map its beat in the level's pacing (RoomBeat), from the map graph
    // alone, before anything is built, so the rooms can be made for it: their size, how much
    // cover they get, how many enemies, what is put down in them, where reinforcements go.
    //
    // The graph already rises in intensity toward the boss and never strings too many fights
    // together; the director reads that curve the way an encounter designer would pace it
    // (Left 4 Dead's build-up, peak and relax; a tension curve over rooms). Its beats:
    //   - Climax: the Boss room.
    //   - Rest: a room with no fight planned (MapNodeTypeExtensions.IsFight).
    //   - Peak: a fight at least PeakIntensity, more intense than every fight beside it: the big
    //     set-piece fight of its stretch of map. Ties go to the deeper room, then the lower id, so
    //     two peaks never stand side by side.
    //   - Relief: a fight entered from a Peak (a neighbour nearer the Start), the breather after it.
    //   - Approach: the fight right before the Boss on the main path; a calm room there is
    //     already the breather and stays Rest.
    //   - Build: every other fight.
    // Rooms the Start can't reach get None. Pure: the same graph gives the same beats.
    //
    // Then the main path is paced as a sawtooth, rising fights broken by peaks and breathers:
    //   - a run of more than FlatStretch ordinary fights in a row has its strongest one made a
    //     peak (never beside another peak, never the room before the boss), and the fight after
    //     it the relief, until no run is that long;
    //   - a main path with fights and no peak gets its strongest fight made one;
    //   - more than CalmStretch calm rooms in a row, or a strongest peak in the first third of
    //     the way with only weaker ones after it, can't be put right by beats and are reported.
    // Every change and every report is a DirectorFinding on the direction.
    //
    // Each room's size is scaled by its beat and, for a fight, by how far into the map it lies:
    // small chambers early, bigger ones toward the boss, as Hades grows its chambers through a
    // biome, so the space grows with the fights in it. Once the rooms are placed, a peak beside a
    // clearly bigger ordinary fight moves onto it (MovePeaksToBiggerRooms).
    public static class MapDirector
    {
        // Every room's beat and size; nothing without settings, so nothing changes.
        public static MapDirection Plan(MapGraph graph, MapDirectorSettings settings)
        {
            if (settings == null || graph == null || graph.Nodes.Count == 0) return MapDirection.None;

            var findings = new List<DirectorFinding>();
            Dictionary<int, RoomBeat> beats = Plan(graph, settings.PeakIntensity, settings.FlatStretch, settings.CalmStretch, findings);
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

        // How much bigger an ordinary fight beside a peak must come out for the peak to move onto it.
        private const float PeakMoveSize = 1.25f;

        // Once the rooms are placed: the beats were given from the graph alone, before anyone knew
        // which room would be a multi-cell hall, and a peak beside a hall is the smaller room.
        // Each such peak moves onto the biggest ordinary fight beside it that came out at least
        // PeakMoveSize its tiles (not a hand-built landmark, not beside another peak, and on the
        // main path when the peak was), and the reliefs are marked again after the peaks. A peak
        // moves once. Sizes stay as placed. Every move is a fixed PeakSize finding on the layout.
        public static void MovePeaksToBiggerRooms(MapGraph graph, LevelLayout layout, MapDirectorSettings settings)
        {
            if (settings == null || graph == null || layout.Rooms.Count == 0) return;

            var analysis = new MapGraphAnalysis(graph);
            var onPath = new HashSet<int>(analysis.MainPath);
            var beats = new Dictionary<int, RoomBeat>();
            foreach (MapNode node in graph.Nodes)
                beats[node.Id] = layout.Rooms.TryGetValue(node.Id, out LevelRoom room) ? room.Beat : RoomBeat.None;

            var neighbours = new List<int>();
            var moved = new HashSet<int>();
            bool changed = false;
            foreach (MapNode node in graph.Nodes)
            {
                if (moved.Contains(node.Id) || !layout.Rooms.TryGetValue(node.Id, out LevelRoom peak) || beats[node.Id] != RoomBeat.Peak) continue;

                LevelRoom bigger = BiggerFightBeside(graph, layout, beats, peak, onPath.Contains(node.Id) ? onPath : null, neighbours);
                if (bigger == null) continue;

                beats[node.Id] = RoomBeat.Build;
                beats[bigger.Node.Id] = RoomBeat.Peak;
                moved.Add(bigger.Node.Id);
                changed = true;
                layout.DirectorFindings.Add(new DirectorFinding(DirectorCheck.PeakSize, bigger.Node.Id,
                    $"the peak moved from #{node.Id} ({peak.Footprint.Count} tiles) onto #{bigger.Node.Id} beside it ({bigger.Footprint.Count})", true));
            }
            if (!changed) return;

            foreach (int id in new List<int>(beats.Keys))
                if (beats[id] == RoomBeat.Relief) beats[id] = RoomBeat.Build;
            MarkReliefs(graph, analysis, beats, neighbours);
            foreach (LevelRoom room in layout.Rooms.Values)
                if (beats[room.Node.Id] != room.Beat) room.Recast(beats[room.Node.Id]);
        }

        // `path`: when given, only a room on it.
        private static LevelRoom BiggerFightBeside(MapGraph graph, LevelLayout layout, Dictionary<int, RoomBeat> beats, LevelRoom peak,
                                                   HashSet<int> path, List<int> neighbours)
        {
            var candidates = new List<int>();
            graph.GetNeighbors(peak.Node.Id, candidates);
            LevelRoom best = null;
            foreach (int id in candidates)
            {
                if (!layout.Rooms.TryGetValue(id, out LevelRoom other) || other.Landmark != null || !(beats[id] is RoomBeat.Build or RoomBeat.Relief)) continue;
                if (path != null && !path.Contains(id)) continue;
                if (other.Footprint.Count < peak.Footprint.Count * PeakMoveSize || (best != null && other.Footprint.Count <= best.Footprint.Count)) continue;
                if (BesidePeak(graph, beats, id, neighbours, except: peak.Node.Id)) continue;
                best = other;
            }
            return best;
        }

        // `flatStretch`, `calmStretch`: the longest runs of ordinary fights and of calm rooms the main
        // path may have (0 = not checked). `findings`: what was changed and found, when given.
        public static Dictionary<int, RoomBeat> Plan(MapGraph graph, float peakIntensity,
                                                     int flatStretch = 0, int calmStretch = 0, List<DirectorFinding> findings = null)
        {
            var beats = new Dictionary<int, RoomBeat>();
            if (graph == null || graph.Nodes.Count == 0) return beats;

            var analysis = new MapGraphAnalysis(graph);
            bool IsFight(MapNode node) => node.Type.IsFight();

            var neighbours = new List<int>();
            foreach (MapNode node in graph.Nodes)
            {
                if (!analysis.IsReachable(node.Id)) beats[node.Id] = RoomBeat.None;
                else if (node.Type == MapNodeType.Boss) beats[node.Id] = RoomBeat.Climax;
                else if (!IsFight(node)) beats[node.Id] = RoomBeat.Rest;
                else beats[node.Id] = IsPeak(graph, analysis, node, peakIntensity, IsFight, neighbours) ? RoomBeat.Peak : RoomBeat.Build;
            }

            MarkReliefs(graph, analysis, beats, neighbours);
            List<int> path = FightPath(graph, analysis);
            if (flatStretch > 0)
            {
                BreakFlatStretches(graph, analysis, beats, path, flatStretch, findings, neighbours);
                PromoteIfNoPeak(graph, analysis, beats, path, findings, neighbours);
            }

            MarkApproach(graph, analysis, beats, IsFight);
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

        // The main path's rooms between the Start and the Boss (or the Exit), less the room right
        // before the Boss: it is the Approach whatever it was.
        private static List<int> FightPath(MapGraph graph, MapGraphAnalysis analysis)
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

        private static void BreakFlatStretches(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, List<int> path,
                                               int flatStretch, List<DirectorFinding> findings, List<int> neighbours)
        {
            var run = new List<int>();
            var reported = new HashSet<int>();
            for (bool changed = true; changed;)
            {
                changed = false;
                run.Clear();
                for (int i = 0; i <= path.Count && !changed; i++)
                {
                    if (i < path.Count && beats[path[i]] == RoomBeat.Build)
                    {
                        run.Add(path[i]);
                        continue;
                    }
                    if (run.Count > flatStretch)
                    {
                        changed = TryPromote(graph, analysis, beats, run, neighbours, out int peak);
                        string rooms = $"#{run[0]} to #{run[run.Count - 1]}";
                        if (changed)
                        {
                            findings?.Add(new DirectorFinding(DirectorCheck.FlatStretch, peak, $"{run.Count} ordinary fights in a row ({rooms}): #{peak} made their peak", true));
                            MarkReliefs(graph, analysis, beats, neighbours);
                        }
                        else if (reported.Add(run[0]))
                            findings?.Add(new DirectorFinding(DirectorCheck.FlatStretch, run[0], $"{run.Count} ordinary fights in a row ({rooms}), and none can peak beside the peaks there"));
                    }
                    run.Clear();
                }
            }
        }

        private static void PromoteIfNoPeak(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, List<int> path,
                                            List<DirectorFinding> findings, List<int> neighbours)
        {
            var fights = new List<int>();
            foreach (int id in path)
            {
                if (beats[id] == RoomBeat.Peak) return;
                if (beats[id] is RoomBeat.Build or RoomBeat.Relief) fights.Add(id);
            }
            if (fights.Count < 2 || !TryPromote(graph, analysis, beats, fights, neighbours, out int peak)) return;

            findings?.Add(new DirectorFinding(DirectorCheck.NoPeak, peak, $"no peak on the way to the boss: #{peak}, its strongest fight, made one", true));
            MarkReliefs(graph, analysis, beats, neighbours);
        }

        // The strongest of `candidates` that no peak stands beside, made a peak.
        private static bool TryPromote(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, List<int> candidates,
                                       List<int> neighbours, out int peak)
        {
            peak = -1;
            MapNode best = null;
            foreach (int id in candidates)
            {
                if (!graph.TryGetNode(id, out MapNode node) || BesidePeak(graph, beats, id, neighbours)) continue;
                if (best == null || Outranks(analysis, node, best)) best = node;
            }
            if (best == null) return false;
            peak = best.Id;
            beats[peak] = RoomBeat.Peak;
            return true;
        }

        // `except`: a peak not to count (the one about to move away).
        private static bool BesidePeak(MapGraph graph, Dictionary<int, RoomBeat> beats, int id, List<int> neighbours, int except = -1)
        {
            graph.GetNeighbors(id, neighbours);
            foreach (int other in neighbours)
                if (other != except && beats.TryGetValue(other, out RoomBeat beat) && beat == RoomBeat.Peak) return true;
            return false;
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

        private static bool IsPeak(MapGraph graph, MapGraphAnalysis analysis, MapNode node, float peakIntensity,
                                   System.Func<MapNode, bool> isFight, List<int> neighbours)
        {
            if (node.Intensity < peakIntensity) return false;

            graph.GetNeighbors(node.Id, neighbours);
            foreach (int id in neighbours)
                if (graph.TryGetNode(id, out MapNode other) && isFight(other) && analysis.IsReachable(id) && Outranks(analysis, other, node))
                    return false;
            return true;
        }

        // Whether `a` is the stronger candidate for a peak than `b`.
        private static bool Outranks(MapGraphAnalysis analysis, MapNode a, MapNode b)
        {
            if (a.Intensity != b.Intensity) return a.Intensity > b.Intensity;
            int depthA = analysis.Depth(a.Id), depthB = analysis.Depth(b.Id);
            if (depthA != depthB) return depthA > depthB;
            return a.Id < b.Id;
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

        private static void MarkApproach(MapGraph graph, MapGraphAnalysis analysis, Dictionary<int, RoomBeat> beats, System.Func<MapNode, bool> isFight)
        {
            IReadOnlyList<int> path = analysis.MainPath;
            for (int i = 1; i < path.Count; i++)
            {
                if (!graph.TryGetNode(path[i], out MapNode boss) || boss.Type != MapNodeType.Boss) continue;
                if (graph.TryGetNode(path[i - 1], out MapNode before) && isFight(before)) beats[before.Id] = RoomBeat.Approach;
                return;
            }
        }
    }
}
