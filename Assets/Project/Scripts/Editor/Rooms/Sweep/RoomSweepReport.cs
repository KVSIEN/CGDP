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
    // kept; per place and faction, the median scores; and the worst-fitting rooms by seed.
    public static class RoomSweepReport
    {
        private const int WorstShown = 15;
        private static readonly RoomTrait[] Traits = { RoomTrait.Concealment, RoomTrait.Walls, RoomTrait.LinesOfFire };

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

        private static void WritePlaces(StringBuilder text, IReadOnlyList<RoomSweepResult> results)
        {
            text.AppendLine("## By place (medians over the seeds)");
            text.AppendLine();
            text.AppendLine("| Place | Faction | Concealed % | Narrow % | Lane | Covered | Levels C/W/L (most common) | Mean miss |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (IGrouping<(string, string), RoomSweepResult> group in results.GroupBy(r => (r.Place, r.Faction)))
            {
                List<RoomSweepResult> rooms = group.ToList();
                List<RoomSweepResult> judged = rooms.Where(r => r.Miss >= 0).ToList();
                text.Append($"| {group.Key.Item1} | {group.Key.Item2} | ");
                text.Append($"{Number(Median(rooms.Select(r => r.Traits.ConcealedShare * 100f)))} | {Number(Median(rooms.Select(r => r.Traits.NarrowShare * 100f)))} | ");
                text.Append($"{Number(Median(rooms.Select(r => (float)r.Traits.LongestLane)))} | {Percent(rooms.Count(r => r.Traits.LaneHasCover) / (float)rooms.Count)} | ");
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
