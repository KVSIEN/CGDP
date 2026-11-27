using System;
using System.Collections.Generic;
using CGD.Map;

namespace CGD.Level
{
    // Checks the rooms as they were built against the beats the map director gave them, and puts
    // right what it can while they are built:
    //   - a peak or the boss room (a set piece) should hold more cover for its size than every
    //     fight beside it (structure points per floor tile, StructureBudget): one that doesn't is
    //     built again from the director's own seeds and the try closest to that is kept, one with
    //     a way round before one without (BestSetPiece);
    //   - it should be no smaller than the fights beside it, and a relief smaller than the peak it
    //     follows (sizes are fixed by then, so these are only reported);
    //   - a beat's supplies should have found a place in the room once it is furnished.
    // Every finding goes on the layout (LevelLayout.DirectorFindings) and, short, on its room's
    // overview label, so a room that missed its beat is seen in the Scene view.
    public static class BeatReview
    {
        private static bool IsFight(RoomBeat beat) => beat is RoomBeat.Build or RoomBeat.Peak or RoomBeat.Relief or RoomBeat.Approach;

        // Structure points per floor tile: how much there is to hide behind for the room's size.
        public static float CoverOf(StructureBudget budget, RoomStructure structure) =>
            structure == null || structure.Footprint.Count == 0 ? 0f : budget.Score(structure) / structure.Footprint.Count;

        // The fallback for a set piece planned to its cover bar whose additions ran out before it
        // got there: the best of `first` and up to `retries` more tries (`replan(attempt)`):
        // the first to hold more cover than every built fight beside it, with a way round if any
        // try has one; failing that, the most covered. `first` is kept when it already passes, so
        // a room that met its beat is built exactly as it would be without the check.
        public static RoomStructure BestSetPiece(MapGraph graph, LevelLayout layout, LevelRoom room, RoomStructure first,
                                                 StructureBudget budget, int retries, Func<int, RoomStructure> replan)
        {
            float bar = CoverBar(graph, layout, room, budget);
            if (bar <= 0f || Passes(first, bar, budget)) return first;

            RoomStructure best = first;
            for (int attempt = 0; attempt < retries; attempt++)
            {
                RoomStructure trial = replan(attempt);
                if (Rank(trial, bar, budget) > Rank(best, bar, budget)) best = trial;
                if (Passes(best, bar, budget)) break;
            }
            return best;
        }

        private static bool Passes(RoomStructure structure, float bar, StructureBudget budget) =>
            CoverOf(budget, structure) > bar && RoomFlank.Has(structure);

        // Meeting the bar first, then keeping a way round, then the cover itself.
        private static float Rank(RoomStructure structure, float bar, StructureBudget budget)
        {
            float cover = CoverOf(budget, structure);
            return (cover > bar ? 2f : 0f) + (RoomFlank.Has(structure) ? 1f : 0f) + Math.Min(cover, 0.99f);
        }

        // The cover a set piece should hold more of for its size: the most of any fight beside it
        // already built (0 = none built). It is asked of the room as it is planned
        // (StructureTarget.CoverAbove); BestSetPiece rebuilds only one whose additions ran out.
        public static float CoverBar(MapGraph graph, LevelLayout layout, LevelRoom room, StructureBudget budget)
        {
            float bar = 0f;
            foreach (LevelRoom other in BuiltFightsBeside(graph, layout, room))
                bar = Math.Max(bar, CoverOf(budget, other.Structure));
            return bar;
        }

        private static IEnumerable<LevelRoom> BuiltFightsBeside(MapGraph graph, LevelLayout layout, LevelRoom room)
        {
            var neighbours = new List<int>();
            graph.GetNeighbors(room.Node.Id, neighbours);
            foreach (int id in neighbours)
                if (layout.Rooms.TryGetValue(id, out LevelRoom other) && other.Structure != null && IsFight(other.Beat) && other.Beat != RoomBeat.Peak)
                    yield return other;
        }

        // Once every room is built: the set pieces' cover and size, the reliefs' size, and labels
        // for what the director changed and found before the build (pacing, peaks moved).
        public static void ReviewStructures(MapGraph graph, LevelLayout layout, LevelBuildSettings settings)
        {
            if (settings.Director == null) return;
            foreach (DirectorFinding finding in layout.DirectorFindings) Label(layout, finding);

            StructureBudget budget = settings.StructureBudget;
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Structure == null) continue;
                if (MapDirector.IsSetPiece(room.Beat)) ReviewSetPiece(graph, layout, room, budget);
                else if (room.Beat == RoomBeat.Relief) ReviewRelief(graph, layout, room);
            }
        }

        private static void ReviewSetPiece(MapGraph graph, LevelLayout layout, LevelRoom room, StructureBudget budget)
        {
            float cover = CoverOf(budget, room.Structure);
            foreach (LevelRoom other in BuiltFightsBeside(graph, layout, room))
            {
                if (CoverOf(budget, other.Structure) >= cover)
                {
                    Report(layout, new DirectorFinding(DirectorCheck.PeakCover, room.Node.Id,
                        $"{room.Beat} #{room.Node.Id} holds no more cover for its size ({cover:0.00} a tile) than #{other.Node.Id} beside it ({CoverOf(budget, other.Structure):0.00})"));
                    break;
                }
            }
            foreach (LevelRoom other in BuiltFightsBeside(graph, layout, room))
            {
                if (other.Footprint.Count > room.Footprint.Count)
                {
                    Report(layout, new DirectorFinding(DirectorCheck.PeakSize, room.Node.Id,
                        $"{room.Beat} #{room.Node.Id} ({room.Footprint.Count} tiles) is smaller than #{other.Node.Id} beside it ({other.Footprint.Count})"));
                    break;
                }
            }
        }

        private static void ReviewRelief(MapGraph graph, LevelLayout layout, LevelRoom room)
        {
            var neighbours = new List<int>();
            graph.GetNeighbors(room.Node.Id, neighbours);
            foreach (int id in neighbours)
            {
                if (!layout.Rooms.TryGetValue(id, out LevelRoom peak) || peak.Beat != RoomBeat.Peak || peak.Footprint.Count > room.Footprint.Count) continue;
                Report(layout, new DirectorFinding(DirectorCheck.ReliefSize, room.Node.Id,
                    $"Relief #{room.Node.Id} ({room.Footprint.Count} tiles) is no smaller than the peak #{id} before it ({peak.Footprint.Count})"));
                return;
            }
        }

        // Once the rooms are furnished (`propsOf`): every beat's supplies found a place.
        public static void ReviewSupplies(LevelLayout layout, LevelBuildSettings settings, Func<LevelRoom, IReadOnlyList<PlannedProp>> propsOf)
        {
            if (settings.Director == null) return;
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                BeatTuning tuning = settings.TuningFor(room);
                if (!tuning.HasSupplies || HasAny(propsOf(room), tuning.Supplies)) continue;
                Report(layout, new DirectorFinding(DirectorCheck.SuppliesMissing, room.Node.Id,
                    $"{room.Beat} #{room.Node.Id}: its supplies found no nook to stand in"));
            }
        }

        private static bool HasAny(IReadOnlyList<PlannedProp> props, IReadOnlyList<UnityEngine.GameObject> wanted)
        {
            if (props == null) return false;
            foreach (PlannedProp prop in props)
                foreach (UnityEngine.GameObject prefab in wanted)
                    if (prefab != null && prop.Prefab == prefab) return true;
            return false;
        }

        private static void Report(LevelLayout layout, DirectorFinding finding)
        {
            layout.DirectorFindings.Add(finding);
            Label(layout, finding);
        }

        private static void Label(LevelLayout layout, DirectorFinding finding)
        {
            if (finding.RoomId < 0 || !layout.Rooms.TryGetValue(finding.RoomId, out LevelRoom room)) return;
            room.AddTag(finding.Fixed ? $"Director: {ShortName(finding.Check)}, fixed" : $"! {ShortName(finding.Check)}");
        }

        public static string ShortName(DirectorCheck check) => check switch
        {
            DirectorCheck.FlatStretch     => "flat stretch",
            DirectorCheck.NoPeak          => "no peak",
            DirectorCheck.CalmStretch     => "calm stretch",
            DirectorCheck.EarlyPeak       => "early peak",
            DirectorCheck.PeakCover       => "under-covered",
            DirectorCheck.PeakSize        => "smaller than a neighbour",
            DirectorCheck.ReliefSize      => "relief not smaller",
            DirectorCheck.SuppliesMissing => "supplies missing",
            _                             => check.ToString(),
        };
    }
}
