using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Sight read at eye height (elevation phase 4): a standing eye sees over low props and
    // ledges, a crouching one hides behind them, a raised eye sees over what hides someone on
    // the floor, and the edge of high ground is a firing position.
    public class HeightAwareSightTests
    {
        // A 10 by 8 room entered from the east: its walkway runs along row 4 to the centre.
        private static readonly Vector2Int Door = new(9, 4);

        private static RoomStructure Hall()
        {
            RoomStructure room = Room(10, 8, Door);
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            return room;
        }

        // A platform a level up in the middle of the south side, columns 4–5 and rows 0–2, with
        // a flight climbing west onto it from (6, 1).
        private static RoomStructure HallWithPlatform()
        {
            RoomStructure room = Hall();
            var tiles = new System.Collections.Generic.List<Vector2Int>();
            for (int y = 0; y < 3; y++)
                for (int x = 4; x < 6; x++)
                    tiles.Add(new Vector2Int(x, y));
            var platform = new ElevatedArea(ElevationKind.Platform, 1, tiles,
                                            new[] { new StairFlight(new Vector2Int(6, 1), Vector2Int.left) }, railed: false);
            Assert.That(room.TryAddElevation(platform), Is.True, "the platform fits");
            return room;
        }

        private static void Put(RoomStructure room, Vector2Int tile, float height)
        {
            Assert.That(room.TryClaim(new[] { tile }, RoomTileTags.Prop), Is.True);
            room.SetPropHeight(tile, height);
        }

        private static SightBlocking Standing(RoomStructure room) => new(room);
        private static SightBlocking Crouching(RoomStructure room) => new(room, SightBlocking.CrouchingEyeHeight);

        [Test]
        public void LowCoverIsSeenOverStandingAndHidesSomeoneCrouching()
        {
            RoomStructure room = Hall();
            Put(room, new Vector2Int(5, 2), 1.1f);
            Vector2Int a = new(2, 2), b = new(8, 2);

            Assert.That(Standing(room).CanSee(a, b), Is.True, "a standing eye sees over a counter");
            Assert.That(Crouching(room).CanSee(a, b), Is.False, "a crouching one doesn't");
            Assert.That(Standing(room).BlocksTile(new Vector2Int(5, 2)), Is.False);
            Assert.That(Standing(room).IsTaken(new Vector2Int(5, 2)), Is.True, "nobody stands on it");
        }

        [Test]
        public void ATallPropOrOneOfUnknownHeightBlocksEveryone()
        {
            RoomStructure room = Hall();
            Put(room, new Vector2Int(5, 2), 2.2f);
            Assert.That(room.TryClaim(new[] { new Vector2Int(5, 6) }, RoomTileTags.Prop), Is.True);

            Assert.That(Standing(room).CanSee(new Vector2Int(2, 2), new Vector2Int(8, 2)), Is.False, "a locker");
            Assert.That(Standing(room).CanSee(new Vector2Int(2, 6), new Vector2Int(8, 6)), Is.False, "no height given");
            Assert.That(room.PropHeightAt(new Vector2Int(5, 6)), Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void ReleasingAPropForgetsItsHeight()
        {
            RoomStructure room = Hall();
            var tile = new Vector2Int(5, 2);
            Put(room, tile, 1f);

            room.Release(new[] { tile }, RoomTileTags.Prop);
            Assert.That(room.PropHeightAt(tile), Is.Zero, "no prop");
            Assert.That(room.TryClaim(new[] { tile }, RoomTileTags.Prop), Is.True);
            Assert.That(room.PropHeightAt(tile), Is.EqualTo(float.PositiveInfinity), "a new prop's height is unknown until said");
        }

        [Test]
        public void ACopyKeepsPropHeights()
        {
            RoomStructure room = Hall();
            Put(room, new Vector2Int(5, 2), 1.1f);

            Assert.That(room.Copy().PropHeightAt(new Vector2Int(5, 2)), Is.EqualTo(1.1f).Within(1e-5f));
        }

        [Test]
        public void APlatformHidesSomeoneCrouchedBehindItButNotSomeoneStanding()
        {
            RoomStructure room = HallWithPlatform();
            Vector2Int west = new(2, 1), east = new(8, 1);

            Assert.That(Standing(room).CanSee(west, east), Is.True);
            Assert.That(Crouching(room).CanSee(west, east), Is.False);
            Assert.That(Crouching(room).CanSee(east, west), Is.False, "both ways");
            Assert.That(Standing(room).BlocksEdge(new Vector2Int(3, 1), new Vector2Int(4, 1)), Is.False, "a ledge is under a standing eye");
            Assert.That(Crouching(room).BlocksEdge(new Vector2Int(3, 1), new Vector2Int(4, 1)), Is.True, "and over a crouching one");
        }

        [Test]
        public void ARaisedEyeSeesOverWhatHidesSomeoneOnTheFloor()
        {
            RoomStructure room = HallWithPlatform();
            Put(room, new Vector2Int(2, 1), 1.8f);

            Assert.That(Standing(room).CanSee(new Vector2Int(3, 1), new Vector2Int(0, 1)), Is.False, "from the floor");
            Assert.That(Standing(room).CanSee(new Vector2Int(4, 1), new Vector2Int(0, 1)), Is.True, "from the platform");
        }

        [Test]
        public void StandingSightRunsOverLowCoverAndCrouchedSightStopsAtIt()
        {
            RoomStructure room = Hall();
            Put(room, new Vector2Int(5, 2), 1.1f);

            var standing = new SightlineMap(room);
            var crouching = new SightlineMap(room, Crouching(room));
            Assert.That(standing.Runs(new Vector2Int(2, 2)).alongX, Is.EqualTo(10));
            Assert.That(crouching.Runs(new Vector2Int(2, 2)).alongX, Is.EqualTo(5));
            Assert.That(standing.Runs(new Vector2Int(5, 2)).alongX, Is.Zero, "nobody stands on the counter");
        }

        [Test]
        public void TheEdgeOfHighGroundIsAFiringPositionAndItsLedgeIsCoverBelow()
        {
            RoomStructure room = HallWithPlatform();
            var posts = new RoomPostMap(room, System.Array.Empty<PlannedProp>());

            Assert.That(posts.KindsAt(new Vector2Int(4, 1)) & RoomPostKinds.HighGround, Is.EqualTo(RoomPostKinds.HighGround));
            Assert.That(posts.KindsAt(new Vector2Int(2, 6)) & RoomPostKinds.HighGround, Is.EqualTo(RoomPostKinds.None), "plain floor");
            Assert.That(RoomCover.Shelters(room, new Vector2Int(3, 1), new Vector2Int(4, 1)), Is.True, "the platform's face, from below");
            Assert.That(RoomCover.Shelters(room, new Vector2Int(4, 1), new Vector2Int(3, 1)), Is.False, "nothing to duck behind from above");
            Assert.That(RoomCover.Shelters(room, new Vector2Int(7, 1), new Vector2Int(6, 1)), Is.False, "the stairs are no cover");
        }

        [Test]
        public void APreferenceForHighGroundWeighsTheEdgeOfRaisedFloor()
        {
            var preference = new PostPreference(1f, 1f, 1f, 1f, 1f, RoomPostKinds.None, highGround: 3f);

            Assert.That(preference.WeightOf(RoomPostKinds.HighGround), Is.EqualTo(3f));
            Assert.That(new PostPreference().WeightOf(RoomPostKinds.HighGround), Is.EqualTo(1f), "no preference by default");
        }

        [Test]
        public void APropIsAsTallAsTheTopOfItsSolidColliders()
        {
            var prop = new GameObject("Prop");
            var bare = new GameObject("Bare");
            try
            {
                var part = new GameObject("Part", typeof(BoxCollider));
                part.transform.SetParent(prop.transform, false);
                part.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                part.transform.localScale    = new Vector3(1f, 1.2f, 1f);
                var zone = new GameObject("Zone", typeof(BoxCollider));
                zone.transform.SetParent(prop.transform, false);
                zone.transform.localScale = Vector3.one * 5f;
                zone.GetComponent<BoxCollider>().isTrigger = true;

                Assert.That(PropHeights.Of(prop), Is.EqualTo(1.1f).Within(1e-4f), "the trigger doesn't count");
                Assert.That(PropHeights.Of(bare), Is.Zero, "no collider: seen through");
                Assert.That(PropHeights.Of(null), Is.EqualTo(float.PositiveInfinity));
            }
            finally
            {
                Object.DestroyImmediate(prop);
                Object.DestroyImmediate(bare);
            }
        }

        [Test]
        public void ThePlannerSaysHowTallWhatItPutsDownIs()
        {
            var prefab = new GameObject("Crate", typeof(BoxCollider));
            try
            {
                prefab.transform.localScale = new Vector3(1f, 1.2f, 1f);
                prefab.GetComponent<BoxCollider>().center = new Vector3(0f, 0.5f, 0f);
                RoomStructure room = Hall();
                RoomPropPlanner planner = Planner(room, 1);
                RoomPropPlacement placement = Placement(1, 1);
                placement.Prefabs = new[] { prefab };
                planner.Add(placement);

                Assert.That(planner.Planned, Has.Count.EqualTo(1));
                Assert.That(room.PropHeightAt(TileOf(planner.Planned[0])), Is.EqualTo(1.2f).Within(1e-4f));
            }
            finally
            {
                Object.DestroyImmediate(prefab);
            }
        }
    }
}
