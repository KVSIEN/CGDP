using System.Collections.Generic;
using CGD.Level;
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

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static List<RoomIssue> Run(RoomSample sample)
        {
            var issues = new List<RoomIssue>();
            CheckDoorways(sample, issues);
            foreach (string warning in sample.Warnings) issues.Add(new RoomIssue(RoomIssue.Severity.Warning, warning));
            CheckReachable(sample, issues);
            CheckFurnishing(sample, issues);
            CheckPriorities(sample, issues);
            return issues;
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
            var reached = new HashSet<Vector2Int>();
            var open = new Queue<Vector2Int>();
            foreach (Vector2Int entrance in structure.Entrances)
                if (IsWalkable(structure, entrance) && reached.Add(entrance))
                    open.Enqueue(entrance);

            if (open.Count == 0) return;   // no doorway: nothing to walk from
            while (open.Count > 0)
            {
                Vector2Int tile = open.Dequeue();
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int next = tile + side;
                    if (!IsWalkable(structure, next) || structure.IsPartitioned(tile, next) || !reached.Add(next)) continue;
                    open.Enqueue(next);
                }
            }

            int cutOff = 0;
            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (Vector2Int tile in sample.Room.Footprint.Tiles)
            {
                if (!IsWalkable(structure, tile) || reached.Contains(tile)) continue;
                cutOff++;
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            if (cutOff > 0)
                issues.Add(new RoomIssue(RoomIssue.Severity.Error, $"{cutOff} floor tile(s) can't be walked to from any doorway.",
                                         new RectInt(min, max - min + Vector2Int.one)));
        }

        private static bool IsWalkable(RoomStructure structure, Vector2Int tile) =>
            structure.Footprint.Contains(tile) && !structure.Has(tile, Solid);

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

        // A fight room is checked against its faction's priorities; without a faction design,
        // only for the plain hazard of a long open lane with nothing beside it to fight from.
        private static void CheckPriorities(RoomSample sample, List<RoomIssue> issues)
        {
            if (!IsFightRoom(sample)) return;

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
                    $"{faction} wants {Lower(wanted)} {Name(trait)}; this room's is {Lower(got)} ({Describe(trait, sample)}). {Hint(trait, got < wanted)}",
                    trait == RoomTrait.LinesOfFire ? traits.Lane : (RectInt?)null));
            }
        }

        public static readonly RoomTrait[] Traits = { RoomTrait.Concealment, RoomTrait.Walls, RoomTrait.LinesOfFire };

        // Rooms that hold enemies: the ones a faction's priorities are about.
        public static bool IsFightRoom(RoomSample sample)
        {
            RoomContentRule rule = sample.Settings.RuleFor(sample.Room.Node.Type);
            return rule != null && rule.EnemyCount.Max > 0;
        }

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
