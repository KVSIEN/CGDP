using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Plans where a room's enemies start and how they move before a fight: each on a tile its
    // faction favours (FactionDesign › Posts, read from the furnished room by RoomPostMap), a
    // few tiles apart, holding the post or patrolling its side's loop. In a Breach room split
    // by zone, each faction starts and patrols on its own side. Plain floor where a room has
    // no design or no tile of the favoured kinds. Nobody starts or patrols in the threshold
    // just inside the way in (RoomFlowPlanner) while there is floor beyond it.
    //
    // Pure planning, like RoomFurnisher: the level (RoomPopulator) spawns from the plan, and
    // the Room Lab draws the same plan from the same seed.
    public sealed class RoomEnemyPlanner
    {
        // Spawns keep at least this many tiles between them (Chebyshev) while the room allows.
        public const int MinSpacing = 2;

        // Where nothing may start: on pillars and props, in doorways, on the centre spot.
        public const RoomTileTags NeverSpawnOn = RoomTileTags.Structure | RoomTileTags.Prop | RoomTileTags.NearDoor | RoomTileTags.Centre;

        private readonly LevelBuildSettings _settings;

        public RoomEnemyPlanner(LevelBuildSettings settings) => _settings = settings;

        // How many enemies a room's type sends, at its intensity (before rosters are checked).
        public static int CountFor(RoomContentRule rule, LevelRoom room) => rule != null ? rule.EnemyCount.Lerp(room.Node.Intensity) : 0;

        // The seed a room's enemy plan is drawn from, under the level's contents seed.
        public static Seed SeedFor(Seed contents, LevelRoom room) => contents.Derive("posts").Derive(room.Node.Id);

        // `props`: the room's furnishing. `taken`: tiles already used by something else (a key,
        // a terminal), never started on.
        public RoomEnemyPlan Plan(LevelRoom room, IReadOnlyList<PlannedProp> props, int count, RandomStream random,
                                  ICollection<Vector2Int> taken = null)
        {
            // The holder's preference at the room's strength (a frontier room's enemies are less
            // particular); the breaching faction's at full.
            PostPreference holder = _settings.StyleFor(room).Posts;
            PostPreference breach = room.BreachFaction != null ? _settings.DesignFor(room.BreachFaction)?.Posts : holder;
            return Plan(room, props, count, random, taken, holder, breach, _settings.SpawnMarginTiles, _settings.UsesIsovist(IsovistUse.Posts));
        }

        // The plan with the two factions' preferences given (null = plain floor, patrolling).
        internal static RoomEnemyPlan Plan(LevelRoom room, IReadOnlyList<PlannedProp> props, int count, RandomStream random,
                                           ICollection<Vector2Int> taken, PostPreference holder, PostPreference breach, int margin,
                                           bool isovist = false)
        {
            List<Vector2Int> floor = SpawnableTiles(room, margin);
            if (floor.Count == 0) return RoomEnemyPlan.Empty;

            RoomStructure structure = room.Structure;
            RoomPostMap posts = structure != null ? new RoomPostMap(structure, props, isovist) : null;
            BreachSplit split = room.BreachFaction != null ? BreachSplit.Of(structure, room.BreachEntrances) : null;

            // The way in is the player's foothold: enemies start and patrol past it while the room allows.
            List<Vector2Int> inside = PastThreshold(floor, structure);
            List<Vector2Int> holderSide = SideOf(inside, structure, split, breachSide: false);
            List<Vector2Int> breachSide = split != null ? SideOf(inside, structure, split, breachSide: true) : holderSide;

            var spawns = new List<PlannedSpawn>(count);
            var chosen = new List<Vector2Int>(count);
            for (int i = 0; i < count; i++)
            {
                // A room with two factions alternates between them, as the rosters do.
                bool fromBreach = room.BreachFaction != null && i % 2 == 1;
                PostPreference preference = fromBreach ? breach : holder;
                List<Vector2Int> side = fromBreach ? breachSide : holderSide;

                if (!TryPick(side, posts, preference, chosen, taken, random, out Vector2Int tile)
                    && !TryPick(floor, posts, preference, chosen, taken, random, out tile)) break;

                RoomPostKinds kinds = posts?.KindsAt(tile) ?? RoomPostKinds.None;
                chosen.Add(tile);
                spawns.Add(new PlannedSpawn(tile, fromBreach, kinds, preference != null && preference.Holds(kinds)));
            }

            return new RoomEnemyPlan(spawns, Loop(holderSide), split != null ? Loop(breachSide) : Loop(holderSide), split);
        }

        // Floor tiles inside the spawn margin (or as far from the walls as a narrow room allows),
        // off pillars, props, doorways and the centre spot.
        public static List<Vector2Int> SpawnableTiles(LevelRoom room, int margin)
        {
            var tiles = new List<Vector2Int>();
            for (int m = margin; m >= 0 && tiles.Count == 0; m--)
                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (room.Footprint.IsInterior(tile, m) && (room.Structure == null || !room.Structure.Has(tile, NeverSpawnOn)))
                        tiles.Add(tile);
            return tiles;
        }

        // The tiles beyond the room's threshold (RoomFlowPlanner); all of them when none are.
        private static List<Vector2Int> PastThreshold(List<Vector2Int> floor, RoomStructure structure)
        {
            if (structure == null || !structure.Arrival.HasValue) return floor;
            var inside = new List<Vector2Int>(floor.Count);
            foreach (Vector2Int tile in floor)
                if (!structure.Has(tile, RoomTileTags.Threshold)) inside.Add(tile);
            return inside.Count > 0 ? inside : floor;
        }

        private static List<Vector2Int> SideOf(List<Vector2Int> floor, RoomStructure structure, BreachSplit split, bool breachSide)
        {
            if (split == null) return floor;
            var side = new List<Vector2Int>();
            foreach (Vector2Int tile in floor)
                if (split.IsBreachZone(structure.ZoneOf(tile)) == breachSide) side.Add(tile);
            return side.Count > 0 ? side : floor;
        }

        // A weighted draw over the free tiles, spaced from those already chosen; without spacing
        // if the room is too crowded for it.
        private static bool TryPick(List<Vector2Int> tiles, RoomPostMap posts, PostPreference preference, List<Vector2Int> chosen,
                                    ICollection<Vector2Int> taken, RandomStream random, out Vector2Int pick)
        {
            for (int spacing = MinSpacing; spacing >= 1; spacing--)
            {
                float total = 0f;
                foreach (Vector2Int tile in tiles)
                    total += Usable(tile, chosen, taken, spacing) ? WeightOf(tile, posts, preference) : 0f;
                if (total <= 0f) continue;

                float roll = random.Range(0f, total);
                pick = default;
                foreach (Vector2Int tile in tiles)
                {
                    float weight = Usable(tile, chosen, taken, spacing) ? WeightOf(tile, posts, preference) : 0f;
                    if (weight <= 0f) continue;
                    pick = tile;   // the last one that could be picked, should rounding leave the roll unspent
                    roll -= weight;
                    if (roll <= 0f) break;
                }
                return true;
            }
            pick = default;
            return false;
        }

        private static float WeightOf(Vector2Int tile, RoomPostMap posts, PostPreference preference) =>
            preference == null || posts == null ? 1f : preference.WeightOf(posts.KindsAt(tile));

        private static bool Usable(Vector2Int tile, List<Vector2Int> chosen, ICollection<Vector2Int> taken, int spacing)
        {
            if (taken != null && taken.Contains(tile)) return false;
            foreach (Vector2Int other in chosen)
                if (Mathf.Max(Mathf.Abs(tile.x - other.x), Mathf.Abs(tile.y - other.y)) < spacing) return false;
            return true;
        }

        // A loop through the side's outermost tiles (north-east, south-east, south-west,
        // north-west): the arms of a cross or T, a ring's corners. Only tiles one can stand on.
        private static List<Vector2Int> Loop(List<Vector2Int> tiles)
        {
            var loop = new List<Vector2Int>(4);
            if (tiles.Count == 0) return loop;
            foreach (Vector2Int corner in new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1) })
            {
                Vector2Int tile = Extreme(tiles, corner);
                if (!loop.Contains(tile)) loop.Add(tile);
            }
            return loop;
        }

        private static Vector2Int Extreme(List<Vector2Int> tiles, Vector2Int toward)
        {
            Vector2Int best = tiles[0];
            foreach (Vector2Int tile in tiles)
                if (tile.x * toward.x + tile.y * toward.y > best.x * toward.x + best.y * toward.y) best = tile;
            return best;
        }
    }
}
