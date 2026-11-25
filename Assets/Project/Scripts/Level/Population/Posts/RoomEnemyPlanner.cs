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
    // just inside the way in (RoomFlowPlanner) while there is floor beyond it. An overpass's
    // deck is somewhere to start too (high ground and overwatch, over the floor it crosses), and
    // a side's patrol loop crosses each deck whose two ends are on its side. A room with no deck
    // plans exactly as it would without them.
    //
    // Pure planning, like RoomFurnisher: the level (RoomPopulator) spawns from the plan, and
    // the Room Lab draws the same plan from the same seed.
    public sealed class RoomEnemyPlanner
    {
        // Spawns keep at least this many tiles between them (Chebyshev) while the room allows.
        public const int MinSpacing = 2;

        // Where nothing may start: on pillars and props, in doorways, on the centre spot.
        public const RoomTileTags NeverSpawnOn = RoomTileTags.Structure | RoomTileTags.Prop | RoomTileTags.NearDoor | RoomTileTags.Centre;

        // What a spot on a deck offers: it looks down on the floor it crosses, along the lane.
        public const RoomPostKinds DeckKinds = RoomPostKinds.HighGround | RoomPostKinds.Overwatch;

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
            List<Vector2Int> decks = DeckTiles(structure);
            List<Vector2Int> holderDecks = SideOf(PastThreshold(decks, structure, keepAll: false), structure, split, breachSide: false, keepAll: false);
            List<Vector2Int> breachDecks = split != null ? SideOf(PastThreshold(decks, structure, keepAll: false), structure, split, breachSide: true, keepAll: false) : holderDecks;

            var spawns = new List<PlannedSpawn>(count);
            var chosen = new List<Vector2Int>(count);
            for (int i = 0; i < count; i++)
            {
                // A room with two factions alternates between them, as the rosters do.
                bool fromBreach = room.BreachFaction != null && i % 2 == 1;
                PostPreference preference = fromBreach ? breach : holder;
                List<Vector2Int> side = fromBreach ? breachSide : holderSide;
                List<Vector2Int> sideDecks = fromBreach ? breachDecks : holderDecks;

                if (!TryPick(side, sideDecks, posts, preference, chosen, taken, random, out Vector2Int tile, out bool onDeck)
                    && !TryPick(floor, decks, posts, preference, chosen, taken, random, out tile, out onDeck)) break;

                RoomPostKinds kinds = onDeck ? DeckKinds : posts?.KindsAt(tile) ?? RoomPostKinds.None;
                chosen.Add(tile);
                spawns.Add(new PlannedSpawn(tile, fromBreach, kinds, preference != null && preference.Holds(kinds), onDeck));
            }

            List<StandSpot> holderLoop = CrossDecks(Loop(holderSide), structure, split, breachSide: false);
            List<StandSpot> breachLoop = split != null ? CrossDecks(Loop(breachSide), structure, split, breachSide: true) : holderLoop;
            return new RoomEnemyPlan(spawns, holderLoop, breachLoop, split);
        }

        // Every tile an overpass's deck crosses (empty in a room with none).
        private static List<Vector2Int> DeckTiles(RoomStructure structure)
        {
            var tiles = new List<Vector2Int>();
            if (structure == null) return tiles;
            foreach (Overpass overpass in structure.Overpasses) tiles.AddRange(overpass.Deck);
            return tiles;
        }

        // A patrol loop that also walks over each deck with both ends on its side: a stop on the
        // deck's middle, after the stop nearest its first flight, so the patroller climbs on there
        // and comes down wherever its next stop is nearer.
        private static List<StandSpot> CrossDecks(List<StandSpot> loop, RoomStructure structure, BreachSplit split, bool breachSide)
        {
            if (structure == null || loop.Count < 2) return loop;
            foreach (Overpass overpass in structure.Overpasses)
            {
                Vector2Int on = overpass.Stairs[0].Foot, off = overpass.Stairs[1].Foot;
                if (split != null && (split.IsBreachZone(structure.ZoneOf(on)) != breachSide || split.IsBreachZone(structure.ZoneOf(off)) != breachSide)) continue;

                int nearest = 0;
                for (int i = 1; i < loop.Count; i++)
                    if ((loop[i].Tile - on).sqrMagnitude < (loop[nearest].Tile - on).sqrMagnitude) nearest = i;
                loop.Insert(nearest + 1, new StandSpot(overpass.Deck[overpass.Deck.Count / 2], onDeck: true));
            }
            return loop;
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
        // `keepAll`: when every tile is in the threshold, keep them all rather than none.
        private static List<Vector2Int> PastThreshold(List<Vector2Int> floor, RoomStructure structure, bool keepAll = true)
        {
            if (structure == null || !structure.Arrival.HasValue) return floor;
            var inside = new List<Vector2Int>(floor.Count);
            foreach (Vector2Int tile in floor)
                if (!structure.Has(tile, RoomTileTags.Threshold)) inside.Add(tile);
            return inside.Count > 0 || !keepAll ? inside : floor;
        }

        // `keepAll`: when no tile is on the side, keep them all rather than none.
        private static List<Vector2Int> SideOf(List<Vector2Int> floor, RoomStructure structure, BreachSplit split, bool breachSide,
                                               bool keepAll = true)
        {
            if (split == null) return floor;
            var side = new List<Vector2Int>();
            foreach (Vector2Int tile in floor)
                if (split.IsBreachZone(structure.ZoneOf(tile)) == breachSide) side.Add(tile);
            return side.Count > 0 || !keepAll ? side : floor;
        }

        // A weighted draw over the free tiles, spaced from those already chosen; without spacing
        // if the room is too crowded for it.
        // The floor tiles first, then the deck tiles (none in most rooms, so the draw is the same as
        // over the floor alone). `onDeck`: the pick is on a deck, over its tile.
        private static bool TryPick(List<Vector2Int> tiles, List<Vector2Int> decks, RoomPostMap posts, PostPreference preference,
                                    List<Vector2Int> chosen, ICollection<Vector2Int> taken, RandomStream random,
                                    out Vector2Int pick, out bool onDeck)
        {
            for (int spacing = MinSpacing; spacing >= 1; spacing--)
            {
                float total = 0f;
                foreach (Vector2Int tile in tiles)
                    total += Usable(tile, chosen, taken, spacing) ? WeightOf(tile, posts, preference) : 0f;
                foreach (Vector2Int tile in decks)
                    total += Usable(tile, chosen, taken, spacing) ? DeckWeight(posts, preference) : 0f;
                if (total <= 0f) continue;

                float roll = random.Range(0f, total);
                pick = default;
                onDeck = false;
                // The last one that could be picked, should rounding leave the roll unspent.
                for (int list = 0; list < 2 && roll > 0f; list++)
                    foreach (Vector2Int tile in list == 0 ? tiles : decks)
                    {
                        float weight = !Usable(tile, chosen, taken, spacing) ? 0f : list == 0 ? WeightOf(tile, posts, preference) : DeckWeight(posts, preference);
                        if (weight <= 0f) continue;
                        pick = tile;
                        onDeck = list == 1;
                        roll -= weight;
                        if (roll <= 0f) break;
                    }
                return true;
            }
            pick = default;
            onDeck = false;
            return false;
        }

        private static float WeightOf(Vector2Int tile, RoomPostMap posts, PostPreference preference) =>
            preference == null || posts == null ? 1f : preference.WeightOf(posts.KindsAt(tile));

        private static float DeckWeight(RoomPostMap posts, PostPreference preference) =>
            preference == null || posts == null ? 1f : preference.WeightOf(DeckKinds);

        private static bool Usable(Vector2Int tile, List<Vector2Int> chosen, ICollection<Vector2Int> taken, int spacing)
        {
            if (taken != null && taken.Contains(tile)) return false;
            foreach (Vector2Int other in chosen)
                if (Mathf.Max(Mathf.Abs(tile.x - other.x), Mathf.Abs(tile.y - other.y)) < spacing) return false;
            return true;
        }

        // A loop through the side's outermost tiles (north-east, south-east, south-west,
        // north-west): the arms of a cross or T, a ring's corners. Only tiles one can stand on.
        private static List<StandSpot> Loop(List<Vector2Int> tiles)
        {
            var loop = new List<StandSpot>(5);
            if (tiles.Count == 0) return loop;
            foreach (Vector2Int corner in new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1) })
            {
                var spot = new StandSpot(Extreme(tiles, corner));
                if (!loop.Contains(spot)) loop.Add(spot);
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
