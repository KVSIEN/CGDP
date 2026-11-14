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
            CheckExposure(sample, issues);
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

        private static void CheckExposure(RoomSample sample, List<RoomIssue> issues)
        {
            RoomContentRule rule = sample.Settings.RuleFor(sample.Room.Node.Type);
            if (rule == null || rule.EnemyCount.Max <= 0) return;

            RectInt lane = LongestLane(sample, out int length);
            if (length < ExposedLaneTiles) return;
            issues.Add(new RoomIssue(RoomIssue.Severity.Warning,
                $"A {length * sample.Settings.TileSize:0} m open lane with nothing to break it: a fight here favours whoever holds the far end.", lane));
        }

        // The longest unbroken row or column of see-through floor, as a strip of tiles.
        public static RectInt LongestLane(RoomSample sample, out int length)
        {
            length = 0;
            Vector2Int best = default;
            bool alongX = true;
            foreach (Vector2Int tile in sample.Room.Footprint.Tiles)
            {
                (int x, int y) = sample.Sight.Runs(tile);
                if (x > length) { length = x; best = tile; alongX = true; }
                if (y > length) { length = y; best = tile; alongX = false; }
            }
            if (length == 0) return default;

            Vector2Int step = alongX ? Vector2Int.right : Vector2Int.up;
            Vector2Int start = best;
            RoomStructure structure = sample.Room.Structure;
            while (structure.Footprint.Contains(start - step) && !structure.Has(start - step, SightlineMap.BlocksSight)
                   && !structure.IsPartitioned(start - step, start))
                start -= step;
            return alongX ? new RectInt(start, new Vector2Int(length, 1)) : new RectInt(start, new Vector2Int(1, length));
        }
    }
}
