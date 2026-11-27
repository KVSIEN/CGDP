using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // How a room is composed round what it is arranged to face: pairs framing a doorway, a flight
    // of stairs or a keyed piece, and a stage across from the way in with symmetric stairs.
    public class RoomCompositionTests
    {
        private static RoomPropPlacement Flank(FlankAnchor anchor, int pairs, PropFacing facing = PropFacing.Square)
        {
            RoomPropPlacement placement = Placement(pairs * 2, pairs * 2, facing);
            placement.Selection = PropSelection.Flank;
            placement.Flanks    = anchor;
            return placement;
        }

        // Whether two props stand mirrored about the line through `middle` across `axis` (1 = x, the mirror runs along y).
        private static void AssertMirrored(PlannedProp a, PlannedProp b, Vector2 middle, bool acrossX, string at)
        {
            Vector2 pa = a.Position, pb = b.Position;
            if (acrossX)
            {
                Assert.That(pa.y, Is.EqualTo(pb.y).Within(0.01f), $"{at}: in line");
                Assert.That(pa.x + pb.x, Is.EqualTo(2f * middle.x).Within(0.01f), $"{at}: as far either side");
            }
            else
            {
                Assert.That(pa.x, Is.EqualTo(pb.x).Within(0.01f), $"{at}: in line");
                Assert.That(pa.y + pb.y, Is.EqualTo(2f * middle.y).Within(0.01f), $"{at}: as far either side");
            }
            Assert.That(Forward(a), Is.EqualTo(Forward(b)), $"{at}: facing the same way");
        }

        [Test]
        public void APairFramesTheDoorwayAlongItsWall()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var door = new Vector2Int(0, 5);
                RoomStructure room = Room(12, 10, door);
                room.SetArrival(door);
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(Flank(FlankAnchor.Doorways, 1, PropFacing.AgainstWall));

                Assert.That(planner.Planned.Count, Is.EqualTo(2), $"seed {seed}");
                PlannedProp a = planner.Planned[0], b = planner.Planned[1];
                Assert.That(TileOf(a).x, Is.Zero, $"seed {seed}: against the doorway's wall");
                AssertMirrored(a, b, door + Vector2.one * 0.5f, acrossX: false, $"seed {seed}");
                Assert.That(Forward(a), Is.EqualTo(Vector2Int.right), "facing into the room");
                Assert.That(room.Has(TileOf(a), RoomTileTags.NearDoor) || room.Has(TileOf(b), RoomTileTags.NearDoor), Is.False,
                            "the doorway's front stays clear");
            }
        }

        [Test]
        public void APairFramesWhatItStandsNear()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.SetArrival(new Vector2Int(11, 5));
            RoomPropPlanner planner = Planner(room, 3);
            RoomPropPlacement altar = Placement(1, 1, PropFacing.AgainstWall);
            altar.On  = RoomTileTags.Edge;
            altar.Key = "Altar";
            planner.Add(altar);
            Assert.That(planner.Planned.Count, Is.EqualTo(1));
            Vector2 middle = planner.Planned[0].Position;

            RoomPropPlacement candles = Flank(FlankAnchor.Near, 1);
            candles.Near = "Altar";
            candles.NearDistance = 2;
            planner.Add(candles);

            Assert.That(planner.Planned.Count, Is.EqualTo(3));
            Vector2Int facing = Forward(planner.Planned[0]);
            AssertMirrored(planner.Planned[1], planner.Planned[2], middle, acrossX: facing.y != 0, "the altar's pair");
            Assert.That(Forward(planner.Planned[1]), Is.EqualTo(facing), "facing the way the altar does");
        }

        [Test]
        public void APairFramesAFlightAtItsFootOrItsTop()
        {
            foreach (PropFloor floor in new[] { PropFloor.Any, PropFloor.Raised })
            {
                RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
                room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                var tiles = new List<Vector2Int>();
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 2; x++)
                        tiles.Add(new Vector2Int(x, y));
                var flight = new StairFlight(new Vector2Int(2, 3), Vector2Int.left);
                Assert.That(room.TryAddElevation(new ElevatedArea(ElevationKind.Platform, 1, tiles, new[] { flight }, LedgeGuards.None)), Is.True);

                RoomPropPlanner planner = Planner(room, 1);
                RoomPropPlacement planters = Flank(FlankAnchor.Stairs, 1);
                planters.Floor = floor;
                planner.Add(planters);

                Assert.That(planner.Planned.Count, Is.EqualTo(2), floor.ToString());
                Vector2Int end = floor == PropFloor.Raised ? flight.Head : flight.Tile;
                AssertMirrored(planner.Planned[0], planner.Planned[1], end + Vector2.one * 0.5f, acrossX: false, floor.ToString());
                Assert.That(TileOf(planner.Planned[0]).x, Is.EqualTo(end.x), $"{floor}: beside that end");
                Assert.That(Forward(planner.Planned[0]), Is.EqualTo(Vector2Int.right), $"{floor}: facing whoever walks up");
            }
        }

        [Test]
        public void APairGoesDownWholeOrNotAtAll()
        {
            // A doorway in a corner: one side of it is the room's other wall, so no pair fits there.
            var door = new Vector2Int(0, 0);
            RoomStructure room = Room(6, 6, door);
            RoomPropPlanner planner = Planner(room, 1);
            planner.Add(Flank(FlankAnchor.Doorways, 1, PropFacing.AgainstWall));
            Assert.That(planner.Planned.Count % 2, Is.Zero);
        }

        // --- Stages ---------------------------------------------------------------------

        [Test]
        public void AStageIsClimbedUpTheMiddleOrAtBothEnds()
        {
            var front = new List<StairFlight>();
            for (int x = 0; x <= 6; x++) front.Add(new StairFlight(new Vector2Int(x, 3), Vector2Int.up));

            List<StairFlight> wide = ElevationAccess.Stage(front, 0, 6, wide: true);
            Assert.That(wide, Is.EqualTo(new[] { front[3] }), "an odd front: one flight up its middle");
            List<StairFlight> evenWide = ElevationAccess.Stage(front.GetRange(0, 6), 0, 5, wide: true);
            Assert.That(evenWide, Is.EqualTo(new[] { front[2], front[3] }), "an even front: two side by side");

            List<StairFlight> pair = ElevationAccess.Stage(front, 0, 6, wide: false);
            Assert.That(pair, Is.EqualTo(new[] { front[0], front[6] }), "a pair at the ends");

            List<StairFlight> blockedEnd = ElevationAccess.Stage(front.GetRange(1, 6), 0, 6, wide: false);
            Assert.That(blockedEnd, Is.EqualTo(new[] { front[1], front[5] }), "kept symmetric when an end is taken");

            List<StairFlight> noMiddle = ElevationAccess.Stage(new[] { front[0], front[6] }, 0, 6, wide: true);
            Assert.That(noMiddle, Is.EqualTo(new[] { front[0], front[6] }), "the other kind when the middle is taken");
        }

        private static ElevationStructureRule StageRule()
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)ElevationKind.Platform;
            serialized.FindProperty("_role").enumValueIndex = (int)ElevationRole.Stage;
            serialized.FindProperty("_length").FindPropertyRelative("Min").intValue = 4;
            serialized.FindProperty("_length").FindPropertyRelative("Max").intValue = 6;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        [Test]
        public void AStageFacesTheWayInWithSymmetricStairs()
        {
            ElevationStructureRule rule = StageRule();
            int built = 0, facing = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var door = new Vector2Int(6, 0);
                RoomStructure room = Room(13, 11, door);
                room.SetArrival(door);
                room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"seed {seed}");
                if (room.Elevations.Count == 0) continue;

                built++;
                ElevatedArea stage = room.Elevations[0];
                bool farWall = true;
                foreach (Vector2Int tile in stage.Tiles) farWall &= tile.y >= 7;
                if (!farWall) continue;

                facing++;
                float middle = 0f;
                foreach (Vector2Int tile in stage.Tiles) middle += tile.x + 0.5f;
                middle /= stage.Tiles.Count;
                Assert.That(middle, Is.EqualTo(6.5f).Within(0.51f), $"seed {seed}: centred on the room");

                float stairs = 0f;
                foreach (StairFlight flight in stage.Stairs)
                {
                    Assert.That(flight.Up, Is.EqualTo(Vector2Int.up), $"seed {seed}: up its front");
                    stairs += flight.Tile.x + 0.5f;
                }
                Assert.That(stairs / stage.Stairs.Count, Is.EqualTo(middle).Within(0.01f), $"seed {seed}: stairs symmetric about its middle");
            }
            Assert.That(built, Is.GreaterThan(Seeds / 2));
            Assert.That(facing, Is.GreaterThan(built / 2), "most face the way in");
        }
        // --- Islands --------------------------------------------------------------------

        private static ElevationStructureRule IslandRule(ElevationRole role)
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)ElevationKind.Island;
            serialized.FindProperty("_role").enumValueIndex = (int)role;
            serialized.FindProperty("_depth").FindPropertyRelative("Min").intValue = 3;
            serialized.FindProperty("_depth").FindPropertyRelative("Max").intValue = 4;
            serialized.FindProperty("_length").FindPropertyRelative("Min").intValue = 4;
            serialized.FindProperty("_length").FindPropertyRelative("Max").intValue = 5;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        [Test]
        public void AnIslandStandsRaisedOutInTheRoomNearItsMiddle()
        {
            ElevationStructureRule rule = IslandRule(ElevationRole.Bastion);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(14, 12, new Vector2Int(13, 6), new Vector2Int(7, 0));
                room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"seed {seed}");
                if (room.Elevations.Count == 0) continue;

                built++;
                ElevatedArea island = room.Elevations[0];
                Assert.That(island.Level, Is.EqualTo(1), $"seed {seed}: raised");
                Assert.That(island.Stairs.Count, Is.GreaterThanOrEqualTo(2), $"seed {seed}: a bastion is climbed two ways");
                Vector2 middle = Vector2.zero;
                foreach (Vector2Int tile in island.Tiles)
                {
                    Assert.That(room.Footprint.IsInterior(tile, 2), Is.True, $"seed {seed}: floor all round it");
                    middle += tile + Vector2.one * 0.5f;
                }
                middle /= island.Tiles.Count;
                Assert.That((middle - room.Footprint.Anchor).magnitude, Is.LessThan(4.5f), $"seed {seed}: near the room's middle");
                foreach (StairFlight stairs in island.Stairs)
                    Assert.That(room.LevelOf(stairs.Tile), Is.Zero, $"seed {seed}: climbed from the floor round it");
            }
            Assert.That(built, Is.GreaterThan(Seeds / 2));
        }

        // --- Activity areas -------------------------------------------------------------

        private static RoomPropPlacement AreaMarker(string key, Vector2Int size, bool rug)
        {
            RoomPropPlacement area = Placement(1, 1);
            if (!rug) area.Prefabs = System.Array.Empty<GameObject>();
            area.Area      = true;
            area.Decor     = rug;
            area.Footprint = size;
            area.Key       = key;
            area.On        = RoomTileTags.Corner;
            area.Selection = PropSelection.Spread;
            return area;
        }

        private static RoomPropPlacement Inside(string key, int count)
        {
            RoomPropPlacement piece = Placement(count, count);
            piece.Within = key;
            return piece;
        }

        [Test]
        public void AnAreasPiecesStayOnItAndAreasKeepApart()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(AreaMarker("Lounge", new Vector2Int(3, 3), rug: true));
                planner.Add(Inside("Lounge", 3));
                planner.Add(AreaMarker("Work", new Vector2Int(3, 3), rug: false));
                planner.Add(Inside("Work", 2));

                Assert.That(planner.Planned.Count, Is.EqualTo(1 + 3 + 2), $"seed {seed}: the rug and five pieces, the unmarked area nothing");
                RectInt lounge = planner.Planned[0].Tiles;
                Assert.That(lounge.size, Is.EqualTo(new Vector2Int(3, 3)), $"seed {seed}");
                Assert.That(planner.Planned[0].Decor, Is.True, "the rug is a decoration");
                for (int i = 1; i <= 3; i++)
                    Assert.That(lounge.Contains(TileOf(planner.Planned[i])), Is.True, $"seed {seed}: on the lounge's rug");

                // The work corner's two pieces share a 3 by 3 patch a tile clear of the rug.
                Vector2Int first = TileOf(planner.Planned[4]), second = TileOf(planner.Planned[5]);
                Assert.That(Mathf.Abs(first.x - second.x) <= 2 && Mathf.Abs(first.y - second.y) <= 2, Is.True, $"seed {seed}: in one corner");
                foreach (Vector2Int piece in new[] { first, second })
                {
                    int dx = Mathf.Max(0, Mathf.Max(lounge.xMin - piece.x, piece.x - (lounge.xMax - 1)));
                    int dy = Mathf.Max(0, Mathf.Max(lounge.yMin - piece.y, piece.y - (lounge.yMax - 1)));
                    Assert.That(Mathf.Max(dx, dy), Is.GreaterThanOrEqualTo(2), $"seed {seed}: the areas keep apart");
                }
            }
        }

        [Test]
        public void NothingStandsWithinAnAreaNobodyMarked()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            RoomPropPlanner planner = Planner(room, 1);
            Assert.That(planner.Add(Inside("Lounge", 3)), Is.Zero);
        }

        [Test]
        public void AnAreaTakesNoFloorOfItsOwn()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            int free = 0;
            foreach (Vector2Int tile in room.Footprint.Tiles) if (room.IsFree(tile)) free++;
            RoomPropPlanner planner = Planner(room, 1);
            planner.Add(AreaMarker("Lounge", new Vector2Int(3, 3), rug: true));
            int after = 0;
            foreach (Vector2Int tile in room.Footprint.Tiles) if (room.IsFree(tile)) after++;
            Assert.That(after, Is.EqualTo(free));
        }

        // --- A beat down the route ------------------------------------------------------

        [Test]
        public void PairsLineTheRouteAtAnEvenBeat()
        {
            var way = new Vector2Int(0, 5);
            RoomStructure room = Room(20, 11, way, new Vector2Int(19, 5));
            room.SetArrival(way);
            RoomPropPlanner planner = Planner(room, 1);
            RoomPropPlacement columns = Flank(FlankAnchor.Route, 10);
            columns.Rhythm   = new Vector2Int(4, 3);
            columns.FlankGap = 1;
            planner.Add(columns);

            Assert.That(planner.Planned.Count, Is.GreaterThanOrEqualTo(4), "two pairs at least down a 20-tile hall");
            Assert.That(planner.Planned.Count % 2, Is.Zero);
            for (int i = 0; i < planner.Planned.Count; i += 2)
            {
                Vector2Int a = TileOf(planner.Planned[i]), b = TileOf(planner.Planned[i + 1]);
                Vector2Int beat = (a + b) / 2;
                Assert.That(room.Has(beat, RoomTileTags.Walkway), Is.True, $"pair {a} {b}: either side of the walkway");
                Assert.That(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y), Is.GreaterThanOrEqualTo(4), $"pair {a} {b}: an aisle between them");
            }
            // Along the straight run from the doorway, the beats are four tiles apart.
            var beatsX = new List<int>();
            for (int i = 0; i < planner.Planned.Count; i += 2)
            {
                Vector2Int a = TileOf(planner.Planned[i]), b = TileOf(planner.Planned[i + 1]);
                if (a.x == b.x && a.y + b.y == 10) beatsX.Add(a.x);
            }
            beatsX.Sort();
            Assert.That(beatsX.Count, Is.GreaterThanOrEqualTo(2));
            for (int i = 1; i < beatsX.Count; i++) Assert.That((beatsX[i] - beatsX[0]) % 4, Is.Zero, $"beats at {string.Join(",", beatsX)}");
        }
    }
}
