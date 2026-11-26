using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Editor;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The placement options that keep a place recognisable at any size and layout: groups set
    // their members round them and turn with them, a placement can stand near (and face) what
    // an earlier one put down, a feature grows with the room and stretches to fit, and a piece
    // for big rooms is left out of small ones.
    public class PropGroupTests
    {
        private const float Offset = 0.35f;

        private static PropMember Member(Vector2 offset, float chance = 1f) => new()
        {
            Prefabs = new GameObject[1],
            Offset  = offset,
            Chance  = chance,
        };

        // --- Groups ---------------------------------------------------------------------

        [Test]
        public void AGroupSetsItsMembersInFrontOfItWhicheverWayItFaces()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(9, 9, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                RoomPropPlacement bar = Placement(3, 3, PropFacing.AgainstWall);
                bar.On      = RoomTileTags.Edge;
                bar.Members = new[] { Member(new Vector2(0f, Offset)) };

                int groups = planner.Add(bar);

                Assert.AreEqual(groups * 2, planner.Planned.Count, $"seed {seed}: a piece and its member per group");
                for (int i = 0; i < planner.Planned.Count; i += 2)
                {
                    PlannedProp piece = planner.Planned[i], member = planner.Planned[i + 1];
                    Vector2 expected = piece.Position + (Vector2)Forward(piece) * Offset;
                    Assert.Less(Vector2.Distance(expected, member.Position), 0.001f, $"seed {seed}: the member stands in front");
                    Assert.AreEqual(piece.Yaw, member.Yaw, 0.001f, $"seed {seed}: the member turns with the group");
                    Assert.AreEqual(piece.Tiles, member.Tiles, $"seed {seed}: the group shares its floor");
                }
            }
        }

        [Test]
        public void AMemberComesInByItsChance()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(0, 4));
            RoomPropPlanner planner = Planner(room, seed: 4);
            RoomPropPlacement table = Placement(4, 4);
            table.Members = new[] { Member(new Vector2(Offset, 0f), chance: 0f), Member(new Vector2(-Offset, 0f)) };

            int groups = planner.Add(table);

            Assert.Greater(groups, 0);
            Assert.AreEqual(groups * 2, planner.Planned.Count, "the never-chance member stays out, the other always comes");
        }

        [Test]
        public void AGroupWithoutAPieceOfItsOwnIsJustItsMembers()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(0, 4));
            RoomPropPlanner planner = Planner(room, seed: 2);
            RoomPropPlacement lounge = Placement(2, 2);
            lounge.Prefabs = System.Array.Empty<GameObject>();
            lounge.Members = new[] { Member(new Vector2(Offset, 0f)), Member(new Vector2(-Offset, 0f)) };

            int groups = planner.Add(lounge);

            Assert.AreEqual(2, groups);
            Assert.AreEqual(4, planner.Planned.Count);
        }

        // --- Near -----------------------------------------------------------------------

        [Test]
        public void APlacementStandsNearWhatItsKeyPutDown()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                RoomPropPlacement pool = Placement(1, 1);
                pool.Key       = "Pool";
                pool.Footprint = new Vector2Int(3, 2);
                RoomPropPlacement loungers = Placement(5, 5);
                loungers.Near         = "Pool";
                loungers.NearDistance = 1;

                Assert.AreEqual(1, planner.Add(pool), $"seed {seed}");
                RectInt basin = planner.Planned[0].Tiles;
                planner.Add(loungers);

                for (int i = 1; i < planner.Planned.Count; i++)
                    Assert.LessOrEqual(Steps(planner.Planned[i].Tiles, basin), 1, $"seed {seed}: a lounger strayed from the pool");
            }
        }

        [Test]
        public void NothingIsPlacedNearAKeyThatPutNothingDown()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(0, 4));
            RoomPropPlanner planner = Planner(room, seed: 1);
            RoomPropPlacement stools = Placement(3, 3);
            stools.Near = "Bar";

            Assert.AreEqual(0, planner.Add(stools));
        }

        [Test]
        public void TowardNearFacesWhatItStandsBeside()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                RoomPropPlacement stage = Placement(1, 1);
                stage.Key       = "Stage";
                stage.Footprint = new Vector2Int(2, 2);
                RoomPropPlacement chairs = Placement(4, 4, PropFacing.TowardNear);
                chairs.Near         = "Stage";
                chairs.NearDistance = 3;
                chairs.Clearance    = 1;   // a quarter turn, not any angle

                planner.Add(stage);
                Vector2 middle = planner.Planned[0].Tiles.center;
                planner.Add(chairs);

                for (int i = 1; i < planner.Planned.Count; i++)
                {
                    PlannedProp chair = planner.Planned[i];
                    Vector2 toStage = middle - chair.Tiles.center;
                    Assert.Greater(Vector2.Dot(toStage, Forward(chair)), 0f, $"seed {seed}: a chair faces away from the stage");
                }
            }
        }

        // --- Size -----------------------------------------------------------------------

        [Test]
        public void AFeatureGrowsWithTheRoomAndStretchesToFit()
        {
            Vector2Int small = GrownFootprint(8, 6);
            Vector2Int large = GrownFootprint(16, 10);

            Assert.AreEqual(new Vector2Int(4, 3), small);
            Assert.AreEqual(new Vector2Int(8, 5), large);
        }

        // The footprint a half-the-room basin gets in a doorless room it can go anywhere in,
        // checked against the stretch it carries.
        private static Vector2Int GrownFootprint(int width, int height)
        {
            RoomStructure room = Room(width, height);
            RoomPropPlanner planner = Planner(room, seed: 5, fillShare: 1f);
            RoomPropPlacement basin = Placement(1, 1);
            basin.Avoid   = RoomTileTags.None;
            basin.Grow    = new Vector2(0.5f, 0.5f);
            basin.Stretch = true;

            Assert.AreEqual(1, planner.Add(basin));
            PlannedProp placed = planner.Planned[0];
            var tiles = new Vector2Int(Mathf.Max(placed.Tiles.width, placed.Tiles.height), Mathf.Min(placed.Tiles.width, placed.Tiles.height));
            var stretch = new Vector2Int(Mathf.RoundToInt(Mathf.Max(placed.Stretch.x, placed.Stretch.y)),
                                         Mathf.RoundToInt(Mathf.Min(placed.Stretch.x, placed.Stretch.y)));
            Assert.AreEqual(tiles, stretch, "the prefab is stretched to the floor it took");
            return tiles;
        }

        [Test]
        public void AGrownFeatureShrinksToTheSpaceThereIs()
        {
            RoomStructure room = Room(12, 12, new Vector2Int(0, 6), new Vector2Int(11, 6), new Vector2Int(6, 0));
            RoomPropPlanner planner = Planner(room, seed: 3, fillShare: 1f);
            RoomPropPlacement basin = Placement(1, 1);
            basin.Grow = new Vector2(0.9f, 0.9f);

            Assert.AreEqual(1, planner.Add(basin), "the walkways leave no room for the full size, but a smaller one fits");
            Assert.Less(planner.Planned[0].Tiles.width * planner.Planned[0].Tiles.height, 10 * 10);
        }

        [Test]
        public void AnUnstretchedPropKeepsItsScale()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(0, 4));
            RoomPropPlanner planner = Planner(room, seed: 1);
            RoomPropPlacement crate = Placement(2, 2);
            crate.Footprint = new Vector2Int(2, 1);

            planner.Add(crate);

            foreach (PlannedProp prop in planner.Planned) Assert.AreEqual(Vector2.one, prop.Stretch);
        }

        // Whichever way a group faces, a chair that would reach into a wall moves the group off it,
        // and the chair keeps its place behind the desk.
        [Test]
        public void AGroupKeepsItsPiecesOutOfTheWalls([Values(PropFacing.AgainstWall, PropFacing.Square)] PropFacing facing)
        {
            var desk  = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPropBuilder.PathFor("OfficeDesk"));
            var chair = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPropBuilder.PathFor("Chair"));
            Assert.IsTrue(desk.TryGetComponent(out PropSizeVariants sizes));
            // How far the chair reaches from its middle, in tiles, and a wall's half thickness.
            const float ChairReach = 0.25f / PlaceholderPropBuilder.TileMetres, Wall = 0.04f;

            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(10, 10, new Vector2Int(0, 5)), seed);
                RoomPropPlacement desks = Placement(6, 6, facing);
                desks.Prefabs = new[] { desk };
                desks.Members = new[] { new PropMember { Prefabs = new[] { chair }, Offset = new Vector2(0f, -0.45f) } };
                planner.Add(desks);

                for (int i = 1; i < planner.Planned.Count; i += 2)
                {
                    PlannedProp seat = planner.Planned[i];
                    Vector2 at = seat.Position;
                    float clear = Mathf.Min(Mathf.Min(at.x, 10f - at.x), Mathf.Min(at.y, 10f - at.y));
                    Assert.GreaterOrEqual(clear, ChairReach + Wall - 0.002f, $"seed {seed}: a chair reaches into the wall at {at}");
                    Vector2 behind = seat.Position - planner.Planned[i - 1].Position;
                    // A bigger or smaller desk moves the chair by what it grew.
                    float grown = sizes.Variants.FirstOrDefault(v => v.Prefab == planner.Planned[i - 1].Prefab).MemberShift.y;
                    Assert.AreEqual(0.45f + grown / PlaceholderPropBuilder.TileMetres, behind.magnitude, 0.001f, $"seed {seed}: the chair left its place");
                }
            }
        }

        [Test]
        public void APieceForBigRoomsIsLeftOutOfSmallOnes()
        {
            RoomPropPlacement stage = Placement(1, 1);
            stage.MinRoomTiles = 80;

            RoomPropPlanner small = Planner(Room(8, 8, new Vector2Int(0, 4)), seed: 1);
            Assert.AreEqual(0, small.Add(stage));
            Assert.AreEqual(0, small.Outcomes.Count, "left out on purpose is not a shortfall");

            RoomPropPlanner large = Planner(Room(10, 10, new Vector2Int(0, 5)), seed: 1);
            Assert.AreEqual(1, large.Add(stage));
        }

        // Steps between the nearest tiles of two rectangles (side by side = 1).
        private static int Steps(RectInt a, RectInt b)
        {
            int dx = Mathf.Max(0, Mathf.Max(a.xMin - (b.xMax - 1), b.xMin - (a.xMax - 1)));
            int dy = Mathf.Max(0, Mathf.Max(a.yMin - (b.yMax - 1), b.yMin - (a.yMax - 1)));
            return Mathf.Max(dx, dy);
        }
    }
}
