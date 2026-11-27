using System.Collections.Generic;
using CGD.Algorithms;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // What a designer wants to know about a generated room before playing it: doorways that
    // didn't fit, floor that can't be walked to, furniture that came up short (and why), and
    // fight rooms with long open lanes and nothing to hide behind.
    public static class RoomSampleChecks
    {
        // An unbroken line of sight this many tiles long in a fight room is worth a look.
        public const int ExposedLaneTiles = 8;

        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        public static List<RoomIssue> Run(RoomSample sample)
        {
            var issues = new List<RoomIssue>();
            CheckDoorways(sample, issues);
            CheckBuilding(sample, issues);
            foreach (string warning in sample.Warnings) issues.Add(new RoomIssue(RoomIssue.Severity.Warning, warning));
            CheckReachable(sample, issues);
            CheckElevation(sample, issues);
            CheckFurnishing(sample, issues);
            CheckPropsClear(sample, issues);
            CheckPriorities(sample, issues);
            return issues;
        }

        // A building or town that was asked for but not built, and rooms a building had no space for.
        private static void CheckBuilding(RoomSample sample, List<RoomIssue> issues)
        {
            LevelRoom room = sample.Room;
            RoomStructure structure = room.Structure;
            bool wanted = (room.RoomLayout != null && room.RoomLayout.IsBuilding) || (room.Function != null && room.Function.IsTown);
            if (wanted && structure.Spaces.Count == 0)
                issues.Add(new RoomIssue(RoomIssue.Severity.Warning,
                    "No building was built: the room is too small for the plan, or its walls would have cut floor off from a doorway."));
            if (structure.LeftOut.Count > 0)
                issues.Add(new RoomIssue(RoomIssue.Severity.Info, $"The building had no space for: {string.Join(", ", structure.LeftOut)}."));
        }

        // What the room's floor levels came to: how many raised and sunken areas, their stairs,
        // where enemies can drop off a ledge, and the overpasses over them.
        private static void CheckElevation(RoomSample sample, List<RoomIssue> issues)
        {
            RoomStructure structure = sample.Room.Structure;
            if (!structure.HasElevation) return;

            int raised = 0, sunken = 0, flights = 0, railed = 0, walled = 0;
            foreach (ElevatedArea area in structure.Elevations)
            {
                if (area.Level > 0) raised++;
                else sunken++;
                flights += area.Stairs.Count;
                if (Has(area, LedgeGuard.Railing)) railed++;
                if (Has(area, LedgeGuard.HalfWall)) walled++;
            }
            string overpasses = structure.Overpasses.Count > 0 ? $", {structure.Overpasses.Count} overpass(es) across" : "";
            issues.Add(new RoomIssue(RoomIssue.Severity.Info,
                $"{raised} raised and {sunken} sunken area(s), {flights} flight(s) of stairs, {railed} of the areas railed and {walled} half-walled on some side (the rest can be dropped off){overpasses}."));
        }

        // Whether any side of the area carries `guard`.
        private static bool Has(ElevatedArea area, LedgeGuard guard)
        {
            foreach (Vector2Int side in GridNeighbours.Four)
                if (area.Guards.On(side) == guard) return true;
            return false;
        }

        private static void CheckDoorways(RoomSample sample, List<RoomIssue> issues)
        {
            int count = sample.DoorwayCount;
            if (sample.DoorsWanted >= 0 && count < sample.DoorsWanted)
                issues.Add(new RoomIssue(RoomIssue.Severity.Warning,
                    $"Only {count} of {sample.DoorsWanted} doorways fit: the floor plan has no free door socket on a wall facing the others."));
            if (sample.Room.Landmark != null)
                issues.Add(new RoomIssue(RoomIssue.Severity.Info, $"Hand-built landmark '{sample.Room.Landmark.name}': it keeps its own interior, so nothing is furnished."));
        }

        // Every floor tile nothing stands on should be reachable from the doorways — what the
        // planners promise (RoomStructure.StaysConnected): walls and inner walls block, pillars
        // and furniture take their tiles, kept-clear and decorated floor is still floor.
        private static void CheckReachable(RoomSample sample, List<RoomIssue> issues)
        {
            RoomStructure structure = sample.Room.Structure;
            var doorways = new List<Vector2Int>();
            foreach (Vector2Int entrance in structure.Entrances)
                if (IsWalkable(structure, entrance)) doorways.Add(entrance);
            if (doorways.Count == 0) return;   // no doorway: nothing to walk from
            var reached = new FloorSteps(structure, doorways, Solid);

            int cutOff = 0;
            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (Vector2Int tile in sample.Room.Footprint.Tiles)
            {
                if (!IsWalkable(structure, tile) || reached.Reaches(tile)) continue;
                cutOff++;
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            if (cutOff > 0)
                issues.Add(new RoomIssue(RoomIssue.Severity.Error, $"{cutOff} floor tile(s) can't be walked to from any doorway.",
                                         new RectInt(min, max - min + Vector2Int.one)));
        }

        private static bool IsWalkable(RoomStructure structure, Vector2Int tile) =>
            structure.Contains(tile) && !structure.Has(tile, Solid);

        // Furniture that reaches into a wall, pillar, ledge guard, stairs or another floor
        // (PropObstruction): planning should never leave one, so each is named.
        private static void CheckPropsClear(RoomSample sample, List<RoomIssue> issues)
        {
            var boxes = new List<Vector2[]>();
            foreach (PlannedProp prop in sample.Furnishing.Props)
            {
                string hit = PropObstruction.Find(sample.Room.Structure, prop, boxes);
                if (hit != null)
                    issues.Add(new RoomIssue(RoomIssue.Severity.Error, $"{prop.Prefab.name} at {prop.Position} reaches into {hit}."));
            }
        }

        private static void CheckFurnishing(RoomSample sample, List<RoomIssue> issues)
        {
            foreach (PropPlacementOutcome outcome in sample.Furnishing.Outcomes)
            {
                if (outcome.Relaxed)
                    issues.Add(new RoomIssue(RoomIssue.Severity.Info, $"{outcome.Name}: {outcome.Explanation}."));
                else if (outcome.IsShort)
                    issues.Add(new RoomIssue(outcome.Placed == 0 && outcome.Placement.Required ? RoomIssue.Severity.Error
                                             : outcome.Placed == 0 ? RoomIssue.Severity.Warning : RoomIssue.Severity.Info,
                        $"{outcome.Name}: {outcome.Placed} of {outcome.Wanted} placed — {outcome.Explanation}."));
            }
        }

        // A fight room is checked for a flank (RoomFlank) and against its faction's priorities;
        // without a faction design, only for the plain hazard of a long open lane with nothing
        // beside it to fight from.
        private static void CheckPriorities(RoomSample sample, List<RoomIssue> issues)
        {
            if (!IsFightRoom(sample)) return;

            if (!RoomFlank.Has(sample.Room.Structure))
                issues.Add(new RoomIssue(RoomIssue.Severity.Info,
                    "No flank: the only way from the threshold to the far end is one lane through the middle (Flow layer). " +
                    "Structure with two openings, or rows running toward the far end, gives a way round."));

            RoomTraitReading traits = sample.Traits;
            if (sample.Design == null)
            {
                if (traits.LongestLane >= ExposedLaneTiles && !traits.LaneHasCover)
                    issues.Add(new RoomIssue(RoomIssue.Severity.Warning,
                        $"A {traits.LongestLane * sample.Settings.TileSize:0} m open lane with nothing beside it: a fight here favours whoever holds the far end.",
                        traits.Lane));
                return;
            }

            foreach (RoomTrait trait in Traits)
            {
                TraitPriority wanted = sample.Design.Priorities.Of(trait), got = traits.LevelOf(trait);
                int off = Mathf.Abs((int)got - (int)wanted);
                if (off == 0) continue;

                string faction = sample.Room.Faction != null ? sample.Room.Faction.DisplayName : sample.Design.name;
                issues.Add(new RoomIssue(off >= 2 ? RoomIssue.Severity.Warning : RoomIssue.Severity.Info,
                    $"{faction} wants {Lower(wanted)} {Name(trait)}; this room's is {Lower(got)} ({Describe(trait, sample)}){(sample.Furnishing.Tries > 1 ? $", the closest of {sample.Furnishing.Tries} tries" : "")}. {Hint(trait, got < wanted)}",
                    trait == RoomTrait.LinesOfFire ? traits.Lane : (RectInt?)null));
            }
        }

        public static readonly RoomTrait[] Traits = { RoomTrait.Concealment, RoomTrait.Walls, RoomTrait.LinesOfFire };

        // Rooms that hold enemies: the ones a faction's priorities are about.
        public static bool IsFightRoom(RoomSample sample) => RoomTraitFit.IsFightRoom(sample.Settings.RuleFor(sample.Room.Node.Type));

        public static string Name(RoomTrait trait) => trait switch
        {
            RoomTrait.Concealment => "concealment",
            RoomTrait.Walls       => "walls",
            _                     => "lines of fire",
        };

        // The measurement behind a trait's level, in a designer's words.
        public static string Describe(RoomTrait trait, RoomSample sample)
        {
            RoomTraitReading traits = sample.Traits;
            float tile = sample.Settings.TileSize;
            return trait switch
            {
                RoomTrait.Concealment => $"{traits.ConcealedShare:0%} of the floor under concealment",
                RoomTrait.Walls       => $"{traits.NarrowShare:0%} of the floor at most {RoomTraitMeasure.NarrowTiles * tile:0} m across",
                _                     => $"longest lane {traits.LongestLane * tile:0} m, {(traits.LaneHasCover ? "with" : "no")} cover beside it",
            };
        }

        public static string SideName(ShipSide side) => side switch
        {
            ShipSide.GuestFirstClass => "guest, first class",
            ShipSide.GuestSteerage   => "guest, steerage",
            ShipSide.Crew            => "crew",
            _                        => "no side",
        };

        private static string Lower(TraitPriority priority) => priority.ToString().ToLowerInvariant();

        // Which knobs move a trait, so the check says what to change.
        private static string Hint(RoomTrait trait, bool tooLittle) => (trait, tooLittle) switch
        {
            (RoomTrait.Concealment, true)  => "Add concealment patches to the faction's or room type's dressing.",
            (RoomTrait.Concealment, false) => "Fewer concealment patches in its dressing.",
            (RoomTrait.Walls, true)        => "More dividers or pillars, a higher fill share or smaller rooms in the faction design.",
            (RoomTrait.Walls, false)       => "Fewer dividers, a lower fill share or larger rooms.",
            (RoomTrait.LinesOfFire, true)  => "Longer rooms (Rect, Cross), cover set along the lanes rather than across them, a lower fill share.",
            _                              => "Break the long lane with cover across it, or a divider.",
        };
    }
}
