using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Lays out one room's structure once its doorways are known: tags the edge, corner,
    // centre and doorway tiles, notes where the entrances are, reserves a walkway from every
    // doorway to the centre, runs the room function's structure rules (its layout's, where the
    // layout brings some; the faction's where neither has any) and then the faction's
    // additions, then works out the zones their
    // inner walls make. A Breach room left in one piece gets the breach faction's divider.
    // Last, the room's flow from the doorway a player arrives by (RoomFlowPlanner).
    // A landmark room brings its own interior: its furniture tiles are marked instead.
    public class RoomStructurePlanner
    {
        // `factionRules`: the building style of the room's faction, applied only where the room's
        // function brings no structure of its own, so a restaurant keeps its kitchen wall.
        // `factionAdditions`: applied to every room the faction holds, after either.
        // `breachDivider`: a Breach room's dividing wall, built only when nothing else has
        // already split the room into zones, so the two factions each get a side.
        // `additionChance`: how likely each addition is to be built (a frontier room's thinner
        // style, TerritoryDepth); at 1 every one is, with no random draw.
        public RoomStructure Plan(LevelRoom room, IReadOnlyList<LevelDoorway> doorways, RandomStream random,
                                  IReadOnlyList<RoomStructureRule> factionRules = null,
                                  IReadOnlyList<RoomStructureRule> factionAdditions = null,
                                  RoomStructureRule breachDivider = null, float additionChance = 1f)
        {
            var entrances = new List<Vector2Int>();
            foreach (LevelDoorway doorway in doorways)
                if (doorway.Room == room) entrances.Add(doorway.RoomTile);

            RoomStructure structure = Prepare(room.Footprint, room.WallHeight, entrances);
            if (room.Landmark != null)
                TagLandmark(structure, room);
            else
            {
                IReadOnlyList<RoomStructureRule> rules = HasRules(room.StructureRules) ? room.StructureRules : factionRules;
                Apply(rules, structure, random);
                Apply(factionAdditions, structure, random, additionChance);
            }

            structure.AssignZones();
            if (room.Landmark == null && breachDivider != null && structure.ZoneCount < 2)
            {
                breachDivider.Apply(structure, random);
                structure.AssignZones();
            }

            Vector2Int? arrival = RoomFlowPlanner.ArrivalOf(room, doorways);
            if (arrival.HasValue) RoomFlowPlanner.Mark(structure, arrival.Value);
            return structure;
        }

        private static void Apply(IReadOnlyList<RoomStructureRule> rules, RoomStructure structure, RandomStream random, float chance = 1f)
        {
            if (rules == null) return;
            foreach (RoomStructureRule rule in rules)
                if (rule != null && (chance >= 1f || random.Chance(chance))) rule.Apply(structure, random);
        }

        private static bool HasRules(IReadOnlyList<RoomStructureRule> rules)
        {
            foreach (RoomStructureRule rule in rules)
                if (rule != null) return true;
            return false;
        }

        // A bare room: its walls, centre, entrances and the walkways between them tagged, before
        // any structure rule has run. `entrances` are the floor tiles inside the doorways.
        public RoomStructure Prepare(RoomFootprint footprint, float wallHeight, IReadOnlyList<Vector2Int> entrances)
        {
            var structure = new RoomStructure(footprint, wallHeight);
            Vector2Int centre = new(Mathf.FloorToInt(footprint.Anchor.x), Mathf.FloorToInt(footprint.Anchor.y));

            TagWalls(structure);
            TagCentre(structure, footprint.Anchor);
            foreach (Vector2Int entrance in entrances)
            {
                structure.AddEntrance(entrance);
                TagNearDoor(structure, entrance);
                TagWalkway(structure, entrance, centre);
            }
            return structure;
        }

        private static void TagLandmark(RoomStructure structure, LevelRoom room)
        {
            Vector2Int origin = room.Footprint.Bounds.min;
            foreach (Vector2Int local in room.Landmark.OccupiedTiles)
            {
                Vector2Int tile = origin + LandmarkTurn.Tile(local, room.Landmark.SizeTiles, room.LandmarkTurns);
                if (room.Footprint.Contains(tile)) structure.Tag(tile, RoomTileTags.Structure);
            }
        }

        private static void TagWalls(RoomStructure structure)
        {
            RoomFootprint footprint = structure.Footprint;
            foreach (Vector2Int tile in footprint.Tiles)
            {
                bool horizontalWall = !footprint.Contains(tile + Vector2Int.left) || !footprint.Contains(tile + Vector2Int.right);
                bool verticalWall   = !footprint.Contains(tile + Vector2Int.up)   || !footprint.Contains(tile + Vector2Int.down);
                if (!footprint.IsInterior(tile, 1)) structure.Tag(tile, RoomTileTags.Edge);
                if (horizontalWall && verticalWall) structure.Tag(tile, RoomTileTags.Corner);
            }
        }

        // Same reach as the populator always kept clear around the centrepiece.
        private static void TagCentre(RoomStructure structure, Vector2 anchor)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (Vector2.Distance(tile + Vector2.one * 0.5f, anchor) < 1.5f)
                    structure.Tag(tile, RoomTileTags.Centre);
        }

        private static void TagNearDoor(RoomStructure structure, Vector2Int doorTile)
        {
            foreach (Vector2Int tile in structure.Footprint.Tiles)
                if (Mathf.Abs(tile.x - doorTile.x) + Mathf.Abs(tile.y - doorTile.y) <= 2)
                    structure.Tag(tile, RoomTileTags.NearDoor);
        }

        // Shortest route over the floor (breadth-first), preferring to keep going straight.
        private static void TagWalkway(RoomStructure structure, Vector2Int from, Vector2Int to)
        {
            RoomFootprint footprint = structure.Footprint;
            if (!footprint.Contains(to)) return;

            var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var open = new Queue<Vector2Int>();
            open.Enqueue(from);
            while (open.Count > 0 && !cameFrom.ContainsKey(to))
            {
                Vector2Int tile = open.Dequeue();
                Vector2Int heading = tile - cameFrom[tile];
                if (heading != Vector2Int.zero) TryStep(tile, heading);
                foreach (Vector2Int side in GridSides.All) TryStep(tile, side);

                void TryStep(Vector2Int current, Vector2Int dir)
                {
                    Vector2Int next = current + dir;
                    if (!footprint.Contains(next) || cameFrom.ContainsKey(next)) return;
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(to)) return;
            for (Vector2Int tile = to; ; tile = cameFrom[tile])
            {
                structure.Tag(tile, RoomTileTags.Walkway);
                if (tile == from) break;
            }
        }
    }
}
