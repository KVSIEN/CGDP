using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Editor
{
    // Builds the levels of a map style for many seeds, as LevelBuilder lays them out, and reads
    // them by the map director's beats (MapDirector, BeatReview): what each beat came out as on
    // average, how often each check was fixed or left, and the seeds that left the most, to open
    // and look at. Furnishing is optional: it is the slow part, and only the supplies check needs it.
    public sealed class MapDirectorSweep
    {
        private sealed class BeatSum
        {
            public int Rooms, Tiles, Enemies, FlankRooms, Flanks;
            public float Cover, Run;
        }

        private readonly Dictionary<RoomBeat, BeatSum> _beats = new();
        private readonly Dictionary<DirectorCheck, (int fixedCount, int left)> _checks = new();
        private readonly List<(string seed, int left, string first)> _worst = new();
        private int _maps;
        private long _milliseconds;
        private bool _furnished;

        public bool Cancelled { get; private set; }

        // `progress(done, total)`: return true to stop early.
        public static MapDirectorSweep Run(MapGenerationSettings style, LevelBuildSettings build, int seeds, string firstSeed, bool furnish,
                                           Func<int, int, bool> progress = null)
        {
            var sweep = new MapDirectorSweep { _furnished = furnish };
            var generator = new MapGenerator(style);
            int done = 0;
            foreach (string text in SeedSequence.CountingFrom(firstSeed, seeds))
            {
                if (progress != null && progress(done++, seeds))
                {
                    sweep.Cancelled = true;
                    break;
                }
                Seed seed = Seed.Parse(text);
                var watch = Stopwatch.StartNew();
                MapGraph graph = generator.Generate(seed).Graph;
                LevelLayout layout = new LevelLayoutBuilder(build).Build(graph, style.NodeSpacing, seed, style.Content);
                if (furnish) Furnish(layout, build, seed);
                sweep._milliseconds += watch.ElapsedMilliseconds;
                sweep.Add(text, layout, build);
            }
            return sweep;
        }

        // As LevelBuilder furnishes, then the supplies check.
        private static void Furnish(LevelLayout layout, LevelBuildSettings build, Seed seed)
        {
            var furnisher = new RoomFurnisher(build, LevelSeeds.Contents(seed));
            var props = new Dictionary<int, IReadOnlyList<PlannedProp>>();
            foreach (LevelRoom room in layout.Rooms.Values) props[room.Node.Id] = furnisher.Furnish(room).Props;
            BeatReview.ReviewSupplies(layout, build, room => props.TryGetValue(room.Node.Id, out IReadOnlyList<PlannedProp> p) ? p : null);
        }

        private void Add(string seed, LevelLayout layout, LevelBuildSettings build)
        {
            _maps++;
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (room.Structure == null) continue;
                if (!_beats.TryGetValue(room.Beat, out BeatSum sum)) _beats[room.Beat] = sum = new BeatSum();
                RoomStructure structure = room.Structure;
                sum.Rooms++;
                sum.Tiles += structure.Footprint.Count;
                sum.Cover += BeatReview.CoverOf(build.StructureBudget, structure);
                sum.Run += MeanRun(structure);
                sum.Enemies += RoomEnemyPlanner.CountFor(build, room);
                if (RoomFlank.Applies(structure))
                {
                    sum.FlankRooms++;
                    if (RoomFlank.Has(structure)) sum.Flanks++;
                }
            }

            int left = 0;
            string first = null;
            foreach (DirectorFinding finding in layout.DirectorFindings)
            {
                _checks.TryGetValue(finding.Check, out (int fixedCount, int left) count);
                if (finding.Fixed) count.fixedCount++;
                else
                {
                    count.left++;
                    left++;
                    first ??= finding.Message;
                }
                _checks[finding.Check] = count;
            }
            if (left > 0) _worst.Add((seed, left, first));
        }

        // How far a room's floor sees on average along its rows and columns, standing (tiles).
        private static float MeanRun(RoomStructure structure)
        {
            var sight = new SightlineMap(structure);
            float run = 0f;
            int seeing = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                (int x, int y) = sight.Runs(tile);
                if (x + y == 0) continue;
                run += (x + y) * 0.5f;
                seeing++;
            }
            return seeing > 0 ? run / seeing : 0f;
        }

        public string ToText()
        {
            var text = new StringBuilder();
            text.AppendLine($"BUILT LEVELS: {_maps} maps{(Cancelled ? " (stopped early)" : "")}, {(_maps > 0 ? _milliseconds / _maps : 0)} ms a map{(_furnished ? " with furnishing" : "")}");
            text.AppendLine();
            text.AppendLine("Beat       rooms   tiles   cover/tile   sight run   enemies   way round");
            foreach (RoomBeat beat in Enum.GetValues(typeof(RoomBeat)))
            {
                if (!_beats.TryGetValue(beat, out BeatSum sum) || sum.Rooms == 0) continue;
                float n = sum.Rooms;
                string flank = sum.FlankRooms > 0 ? $"{100f * sum.Flanks / sum.FlankRooms:0}%" : "-";
                text.AppendLine($"{beat,-9}  {sum.Rooms,5}   {sum.Tiles / n,5:0}   {sum.Cover / n,10:0.00}   {sum.Run / n,9:0.0}   {sum.Enemies / n,7:0.0}   {flank,9}");
            }

            text.AppendLine();
            text.AppendLine("Checks     (fixed by the director / left)");
            if (_checks.Count == 0) text.AppendLine("  none found");
            foreach (DirectorCheck check in Enum.GetValues(typeof(DirectorCheck)))
                if (_checks.TryGetValue(check, out (int fixedCount, int left) count))
                    text.AppendLine($"  {BeatReview.ShortName(check),-26} {count.fixedCount,4} / {count.left,-4} ({Per(count.fixedCount + count.left)} a map)");
            if (!_furnished) text.AppendLine("  (supplies not checked: tick Furnish)");

            if (_worst.Count > 0)
            {
                _worst.Sort((a, b) => b.left.CompareTo(a.left));
                text.AppendLine();
                text.AppendLine("Seeds with the most left (open one with this seed on the LevelBuilder):");
                for (int i = 0; i < Math.Min(8, _worst.Count); i++)
                    text.AppendLine($"  {_worst[i].seed,-12} {_worst[i].left} left, e.g. {_worst[i].first}");
            }
            return text.ToString();
        }

        private string Per(int count) => _maps > 0 ? (count / (float)_maps).ToString("0.00") : "0";
    }
}
