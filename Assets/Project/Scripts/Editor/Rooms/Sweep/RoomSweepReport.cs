using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CGD.Level;

namespace CGD.Editor
{
    // A sweep's results as Markdown, short enough to paste into a conversation: per faction,
    // how its rooms read on each trait against the bands (spread of levels and of the raw
    // scores behind them), how often they fit its priorities and which furnishing try was
    // kept; the expressive range (how the rooms play from the way in, how widely each faction's
    // rooms spread and how alike the factions are); per place and faction, the median scores;
    // and the worst-fitting rooms by seed.
    public static class RoomSweepReport
    {
        private const int WorstShown = 15;
        private static readonly RoomTrait[] Traits = { RoomTrait.Concealment, RoomTrait.Walls, RoomTrait.LinesOfFire };

        // The two plots the report draws: a faction's trade between hiding and shooting, and
        // between walls and what the doorway gives away.
        public static readonly (RoomMeasure X, RoomMeasure Y)[] Plots =
            { (RoomMeasure.Concealed, RoomMeasure.Lane), (RoomMeasure.Narrow, RoomMeasure.Revealed) };

        // Densest last, for the text histograms.
        private const string Shades = " .:-=+*#";

        public static string Write(RoomSweep sweep)
        {
            var text = new StringBuilder();
            RoomLabRecipe recipe = sweep.Recipe;
            IReadOnlyList<RoomSweepResult> results = sweep.Results;

            text.AppendLine($"# Room sweep: {recipe.Type}, tier {recipe.Tier}, {recipe.DoorCount} doorway(s), seeds from \"{recipe.Seed}\"");
            text.AppendLine($"{results.Count} rooms built, {sweep.Failed} failed, {sweep.Total - sweep.Done} still to build. " +
                            $"Priority Tries {recipe.BuildSettings.PriorityTries}.");
            text.AppendLine($"Bands: concealment > 0 = Medium, ≥ {Percent(RoomTraitMeasure.ConcealmentHighShare)} = High · " +
                            $"walls ≥ {Percent(RoomTraitMeasure.WallsMediumShare)} / ≥ {Percent(RoomTraitMeasure.WallsHighShare)} narrow floor · " +
                            $"lane ≥ {RoomTraitMeasure.LaneMediumTiles} / ≥ {RoomTraitMeasure.LaneHighTiles} tiles (a level lower with no cover beside it)");
            text.AppendLine();

            WriteFactions(text, results);
            WriteReadings(text, results, recipe.BuildSettings.IsovistFor);
            WriteRange(text, results);
            WritePlaces(text, results);
            WriteWorst(text, results);
            return text.ToString();
        }

        private static void WriteFactions(StringBuilder text, IReadOnlyList<RoomSweepResult> results)
        {
            text.AppendLine("## By faction");
            text.AppendLine("Levels are the share of rooms reading Low / Medium / High; raw scores are the 25th / 50th / 75th percentiles.");
            text.AppendLine();
            text.AppendLine("| Faction | Wants C / W / L | Fit | Mean miss | Concealment L/M/H | Concealed % | Walls L/M/H | Narrow % | Lines L/M/H | Lane tiles | Lane covered | Kept try |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<string, RoomSweepResult> faction in results.GroupBy(r => r.Faction))
            {
                List<RoomSweepResult> rooms = faction.ToList();
                List<RoomSweepResult> judged = rooms.Where(r => r.Miss >= 0).ToList();
                FactionDesign design = rooms[0].Design;

                text.Append($"| {faction.Key} | {Wants(design)} | ");
                text.Append(judged.Count > 0 ? $"{Percent(judged.Count(r => r.Miss == 0) / (float)judged.Count)} | {judged.Average(r => r.Miss):0.00} | " : "— | — | ");
                text.Append($"{Levels(rooms, RoomTrait.Concealment)} | {Quartiles(rooms.Select(r => r.Traits.ConcealedShare * 100f))} | ");
                text.Append($"{Levels(rooms, RoomTrait.Walls)} | {Quartiles(rooms.Select(r => r.Traits.NarrowShare * 100f))} | ");
                text.Append($"{Levels(rooms, RoomTrait.LinesOfFire)} | {Quartiles(rooms.Select(r => (float)r.Traits.LongestLane))} | ");
                text.Append($"{Percent(rooms.Count(r => r.Traits.LaneHasCover) / (float)rooms.Count)} | {KeptTries(rooms)} |");
                text.AppendLine();
            }
            text.AppendLine();
        }

        // The same rooms read two ways: along rows and columns, and from what can be seen in every
        // direction (IsovistUse). Which separates the factions better, and which fits their priorities,
        // says which reading to leave on, and where the bands belong for it.
        private static void WriteReadings(StringBuilder text, IReadOnlyList<RoomSweepResult> results, IsovistUse inForce)
        {
            if (results.Count == 0) return;

            text.AppendLine("## Rows and columns against every direction");
            text.AppendLine($"Generation steered by {(((inForce & IsovistUse.Traits) != 0) ? "the every-direction reading" : "rows and columns")} " +
                            "(Isovist For › Traits); both readings below are of the same finished rooms. Narrow % is the floor at most 2 tiles across, " +
                            $"enclosed % the floor that sees {Percent(RoomSightReading.EnclosedShare)} of the room or less; lane is the longest open row or column, " +
                            "sightline the longest line at any angle (both in tiles). Bands are the same for both.");
            text.AppendLine();
            text.AppendLine("| Faction | Reading | Walls L/M/H | Narrow or enclosed % | Lines L/M/H | Lane or sightline | Covered | Fit | Mean miss |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<string, RoomSweepResult> faction in results.GroupBy(r => r.Faction))
            {
                List<RoomSweepResult> rooms = faction.ToList();
                WriteReading(text, faction.Key, "rows", rooms, r => r.Rows, r => r.RowsMiss);
                WriteReading(text, "", "every direction", rooms, r => r.EveryDirection, r => r.EveryDirectionMiss);
            }
            text.AppendLine();

            text.AppendLine("Separation: how far apart the factions' medians are, against how widely each faction's own rooms spread (higher = the factions read more differently, so the reading tells them apart).");
            text.AppendLine();
            text.AppendLine("| Trait | Rows and columns | Every direction |");
            text.AppendLine("|---|---|---|");
            text.AppendLine($"| Walls | {Separated(Separation(results, r => r.Rows.NarrowShare))} | {Separated(Separation(results, r => r.EveryDirection.NarrowShare))} |");
            text.AppendLine($"| Lines of fire | {Separated(Separation(results, r => r.Rows.LongestLane))} | {Separated(Separation(results, r => r.EveryDirection.LongestLane))} |");
            text.AppendLine();
        }

        private static void WriteReading(StringBuilder text, string faction, string reading, List<RoomSweepResult> rooms,
                                         System.Func<RoomSweepResult, RoomTraitReading> read, System.Func<RoomSweepResult, int> missOf)
        {
            List<RoomSweepResult> judged = rooms.Where(r => missOf(r) >= 0).ToList();
            float level(RoomTrait trait, TraitPriority at) => rooms.Count(r => read(r).LevelOf(trait) == at) / (float)rooms.Count;
            string levels(RoomTrait trait) => $"{Whole(level(trait, TraitPriority.Low))}/{Whole(level(trait, TraitPriority.Medium))}/{Whole(level(trait, TraitPriority.High))}";

            text.Append($"| {faction} | {reading} | {levels(RoomTrait.Walls)} | {Quartiles(rooms.Select(r => read(r).NarrowShare * 100f))} | ");
            text.Append($"{levels(RoomTrait.LinesOfFire)} | {Quartiles(rooms.Select(r => (float)read(r).LongestLane))} | ");
            text.Append($"{Percent(rooms.Count(r => read(r).LaneHasCover) / (float)rooms.Count)} | ");
            text.AppendLine(judged.Count > 0 ? $"{Percent(judged.Count(r => missOf(r) == 0) / (float)judged.Count)} | {judged.Average(missOf):0.00} |" : "— | — |");
        }

        // The spread of the factions' median scores over the average of their interquartile ranges:
        // a score that puts the factions far apart while each keeps tight scores separates them well.
        // Negative when there is nothing to compare (fewer than two factions, or no spread within them).
        public static float Separation(IReadOnlyList<RoomSweepResult> results, System.Func<RoomSweepResult, float> score)
        {
            var medians = new List<float>();
            float widths = 0f;
            foreach (IGrouping<string, RoomSweepResult> faction in results.GroupBy(r => r.Faction))
            {
                List<float> sorted = faction.Select(score).OrderBy(v => v).ToList();
                medians.Add(At(sorted, 0.5f));
                widths += At(sorted, 0.75f) - At(sorted, 0.25f);
            }
            if (medians.Count < 2) return -1f;

            float width = widths / medians.Count;
            return width <= 0f ? -1f : (medians.Max() - medians.Min()) / width;
        }

        private static string Separated(float separation) => separation < 0f ? "—" : separation.ToString("0.0", CultureInfo.InvariantCulture);

        private static void WriteRange(StringBuilder text, IReadOnlyList<RoomSweepResult> results)
        {
            List<IGrouping<string, RoomSweepResult>> factions = results.GroupBy(r => r.Faction).ToList();
            if (factions.Count == 0) return;

            text.AppendLine("## Expressive range");
            text.AppendLine("Way round: rooms with a flank past the main lane. Landmark seen: rooms whose Showcase piece is in view from the way in (of those with one). " +
                            $"Spread: the share of the {ExpressiveRange.Bins}×{ExpressiveRange.Bins} plot a faction's rooms reach — low means one room built over and over.");
            text.AppendLine();
            text.Append("| Faction | Way round | Landmark seen | Seen from way in % | Filled % |");
            foreach ((RoomMeasure x, RoomMeasure y) in Plots) text.Append($" Spread {Short(x)}×{Short(y)} |");
            text.AppendLine();
            text.AppendLine("|---|---|---|---|---|" + string.Concat(Plots.Select(_ => "---|")));
            foreach (IGrouping<string, RoomSweepResult> faction in factions)
            {
                List<RoomSweepResult> rooms = faction.ToList();
                List<RoomSweepResult> entered = rooms.Where(r => r.Flow.HasArrival).ToList();
                List<RoomSweepResult> showcased = rooms.Where(r => r.ShowcaseSeen.HasValue).ToList();
                text.Append($"| {faction.Key} | {Percent(rooms.Count(r => r.Flow.Flank) / (float)rooms.Count)} | ");
                text.Append(showcased.Count > 0 ? $"{Percent(showcased.Count(r => r.ShowcaseSeen == true) / (float)showcased.Count)} | " : "— | ");
                text.Append(entered.Count > 0 ? $"{Quartiles(entered.Select(r => r.Flow.Revealed * 100f))} | " : "— | ");
                text.Append($"{Quartiles(rooms.Select(r => r.Flow.Filled * 100f))} |");
                foreach ((RoomMeasure x, RoomMeasure y) in Plots) text.Append($" {Percent(new ExpressiveRange(rooms, x, y).Coverage)} |");
                text.AppendLine();
            }
            text.AppendLine();

            if (factions.Count > 1)
            {
                text.AppendLine("How alike two factions' rooms are on each plot (0% = never the same kind of room, 100% = the same spread):");
                text.AppendLine();
                for (int a = 0; a < factions.Count; a++)
                    for (int b = a + 1; b < factions.Count; b++)
                        text.AppendLine($"- {factions[a].Key} / {factions[b].Key}: " + string.Join(", ", Plots.Select(plot =>
                            $"{Short(plot.X)}×{Short(plot.Y)} {Percent(ExpressiveRange.Overlap(new ExpressiveRange(factions[a], plot.X, plot.Y), new ExpressiveRange(factions[b], plot.X, plot.Y)))}")));
                text.AppendLine();
            }

            foreach ((RoomMeasure x, RoomMeasure y) in Plots)
            {
                text.AppendLine($"{RoomMeasures.Name(y)} (up) against {RoomMeasures.Name(x)} (across), densest `{Shades[Shades.Length - 1]}`:");
                text.AppendLine("```");
                var ranges = factions.Select(f => (f.Key, Range: new ExpressiveRange(f, x, y))).ToList();
                text.AppendLine(string.Join("   ", ranges.Select(r => Pad(r.Key, ExpressiveRange.Bins + 2))));
                for (int row = ExpressiveRange.Bins - 1; row >= 0; row--)
                    text.AppendLine(string.Join("   ", ranges.Select(r => "|" + Row(r.Range, row) + "|")));
                text.AppendLine("```");
            }
            text.AppendLine();
        }

        private static string Row(ExpressiveRange range, int row)
        {
            var line = new StringBuilder();
            for (int column = 0; column < ExpressiveRange.Bins; column++)
            {
                int count = range.Count(column, row);
                int shade = count == 0 || range.Max == 0 ? 0 : 1 + (count - 1) * (Shades.Length - 2) / System.Math.Max(1, range.Max - 1);
                line.Append(Shades[System.Math.Min(shade, Shades.Length - 1)]);
            }
            return line.ToString();
        }

        private static string Pad(string text, int width) => text.Length >= width ? text.Substring(0, width) : text.PadRight(width);

        private static string Short(RoomMeasure measure) => measure.ToString().Substring(0, 1);

        private static void WritePlaces(StringBuilder text, IReadOnlyList<RoomSweepResult> results)
        {
            text.AppendLine("## By place (medians over the seeds)");
            text.AppendLine();
            text.AppendLine("| Place | Faction | Concealed % | Narrow % | Lane | Covered | Exposure % | Enclosed % | Sightline | Levels C/W/L (most common) | Mean miss |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<(string, string), RoomSweepResult> group in results.GroupBy(r => (r.Place, r.Faction)))
            {
                List<RoomSweepResult> rooms = group.ToList();
                List<RoomSweepResult> judged = rooms.Where(r => r.Miss >= 0).ToList();
                text.Append($"| {group.Key.Item1} | {group.Key.Item2} | ");
                text.Append($"{Number(Median(rooms.Select(r => r.Traits.ConcealedShare * 100f)))} | {Number(Median(rooms.Select(r => r.Traits.NarrowShare * 100f)))} | ");
                text.Append($"{Number(Median(rooms.Select(r => (float)r.Traits.LongestLane)))} | {Percent(rooms.Count(r => r.Traits.LaneHasCover) / (float)rooms.Count)} | ");
                text.Append($"{Number(Median(rooms.Select(r => r.Sight.Exposure * 100f)))} | {Number(Median(rooms.Select(r => r.Sight.Enclosed * 100f)))} | {Number(Median(rooms.Select(r => r.Sight.Sightline)))} | ");
                text.Append(string.Join("/", Traits.Select(trait => Initial(MostCommon(rooms, trait)))));
                text.AppendLine(judged.Count > 0 ? $" | {judged.Average(r => r.Miss):0.0} |" : " | — |");
            }
            text.AppendLine();
        }

        private static void WriteWorst(StringBuilder text, IReadOnlyList<RoomSweepResult> results)
        {
            List<RoomSweepResult> worst = results.Where(r => r.Miss > 0).OrderByDescending(r => r.Miss).Take(WorstShown).ToList();
            if (worst.Count == 0) return;

            text.AppendLine("## Worst fits (open in the Room Lab with this place, faction and seed)");
            text.AppendLine();
            foreach (RoomSweepResult room in worst)
                text.AppendLine($"- {room.Place}, {room.Faction}, seed \"{room.Seed}\": miss {room.Miss} — reads {Reads(room.Traits)}, wants {Wants(room.Design)}");
        }

        private static string Wants(FactionDesign design) =>
            design == null ? "—" : string.Join(" / ", Traits.Select(trait => Initial(design.Priorities.Of(trait))));

        private static string Reads(RoomTraitReading traits) =>
            string.Join(" / ", Traits.Select(trait => Initial(traits.LevelOf(trait)))) +
            $" ({Number(traits.ConcealedShare * 100f)}% concealed, {Number(traits.NarrowShare * 100f)}% narrow, lane {traits.LongestLane}{(traits.LaneHasCover ? " covered" : "")})";

        private static string Levels(List<RoomSweepResult> rooms, RoomTrait trait)
        {
            float share(TraitPriority level) => rooms.Count(r => r.Traits.LevelOf(trait) == level) / (float)rooms.Count;
            return $"{Whole(share(TraitPriority.Low))}/{Whole(share(TraitPriority.Medium))}/{Whole(share(TraitPriority.High))}";
        }

        private static TraitPriority MostCommon(List<RoomSweepResult> rooms, RoomTrait trait) =>
            rooms.GroupBy(r => r.Traits.LevelOf(trait)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;

        // How often each furnishing try was the one kept, 0 = the room's own seed.
        private static string KeptTries(List<RoomSweepResult> rooms)
        {
            int tries = rooms.Max(r => r.Tries);
            if (tries <= 1) return "1 try";
            return string.Join("/", Enumerable.Range(0, tries).Select(attempt => Whole(rooms.Count(r => r.Attempt == attempt) / (float)rooms.Count)));
        }

        private static string Quartiles(IEnumerable<float> values)
        {
            List<float> sorted = values.OrderBy(v => v).ToList();
            return $"{Number(At(sorted, 0.25f))} / {Number(At(sorted, 0.5f))} / {Number(At(sorted, 0.75f))}";
        }

        private static float Median(IEnumerable<float> values) => At(values.OrderBy(v => v).ToList(), 0.5f);

        // Nearest-rank percentile of sorted values.
        private static float At(List<float> sorted, float fraction) =>
            sorted.Count == 0 ? 0f : sorted[System.Math.Min(sorted.Count - 1, (int)(fraction * sorted.Count))];

        private static string Initial(TraitPriority level) => level.ToString().Substring(0, 1);
        private static string Percent(float share) => (share * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        private static string Whole(float share) => (share * 100f).ToString("0", CultureInfo.InvariantCulture);
        private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
