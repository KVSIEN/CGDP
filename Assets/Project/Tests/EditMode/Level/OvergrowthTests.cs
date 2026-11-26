using CGD.Algorithms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Overgrowth: a low drift of growth lying on a room's floor that never changes how the room
    // plays. It stays under its height, never slopes steeper than its profile allows, leaves the
    // doorways, stairs, ledges, furniture and the floor beside low cover bare, keeps to the part
    // of the room it was given, and grows the same way from the same seed.
    public class OvergrowthTests
    {
        private const int Seeds = 12;
        private const float TileSize = 3f;
        private const float Slack = 1e-4f;

        // --- The field -----------------------------------------------------------------------

        [Test]
        public void TheFieldBlendsBetweenItsSamples()
        {
            // One tile, two samples to a side: corners 0, the middle sample 1.
            var heights = new float[9];
            heights[4] = 1f;
            var field = new OvergrowthField(null, new RectInt(0, 0, 1, 1), 2, heights);

            Assert.That(field.HeightAt(new Vector2(0.5f, 0.5f)), Is.EqualTo(1f).Within(Slack));
            Assert.That(field.HeightAt(new Vector2(0.25f, 0.5f)), Is.EqualTo(0.5f).Within(Slack));
            Assert.That(field.HeightAt(new Vector2(2f, 2f)), Is.EqualTo(0f), "off the grid");
            Assert.That(field.Covers(Vector2Int.zero), Is.True);
            Assert.That(field.Covers(Vector2Int.one), Is.False);
        }

        // --- Planned growth ------------------------------------------------------------------

        [Test]
        public void GrowthKeepsUnderItsHeightAndSlopeAndLeavesTheDoorwayBare()
        {
            OvergrowthProfile profile = Profile(height: 0.35f, maxSlope: 20f);
            var planner = new OvergrowthPlanner(TileSize);
            float rise = Mathf.Tan(20f * Mathf.Deg2Rad) * TileSize / planner.SamplesPerTile;
            bool grew = false;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
                OvergrowthField field = planner.Plan(room, new OvergrowthRequest(profile), Stream(seed));
                if (field == null) continue;
                grew = true;
                string at = $"seed {seed}";

                for (int row = 0; row < field.Rows; row++)
                    for (int column = 0; column < field.Columns; column++)
                    {
                        float height = field.HeightAt(column, row);
                        Assert.That(height, Is.InRange(0f, 0.35f + Slack), $"{at}: under its height");
                        if (column + 1 < field.Columns)
                            Assert.That(Mathf.Abs(height - field.HeightAt(column + 1, row)), Is.LessThanOrEqualTo(rise + Slack), $"{at}: no bank too steep");
                        if (row + 1 < field.Rows)
                            Assert.That(Mathf.Abs(height - field.HeightAt(column, row + 1)), Is.LessThanOrEqualTo(rise + Slack), $"{at}: no bank too steep");
                    }
                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (room.Has(tile, RoomTileTags.NearDoor | RoomTileTags.Centre))
                        Assert.That(field.Covers(tile), Is.False, $"{at}: {tile} in front of the doorway or the centre stays bare");
            }
            Assert.That(grew, Is.True, "something grew");
        }

        [Test]
        public void TheSameSeedGrowsTheSameFloor()
        {
            OvergrowthProfile profile = Profile();
            var planner = new OvergrowthPlanner(TileSize);
            OvergrowthField first = planner.Plan(Room(12, 10, new Vector2Int(11, 5)), new OvergrowthRequest(profile), Stream(3));
            OvergrowthField again = planner.Plan(Room(12, 10, new Vector2Int(11, 5)), new OvergrowthRequest(profile), Stream(3));

            Assert.That(first, Is.Not.Null);
            for (int row = 0; row < first.Rows; row++)
                for (int column = 0; column < first.Columns; column++)
                    Assert.That(again.HeightAt(column, row), Is.EqualTo(first.HeightAt(column, row)));
        }

        [Test]
        public void FurnitureAndTheFloorBesideLowCoverStayBare()
        {
            OvergrowthProfile profile = Profile();
            var planner = new OvergrowthPlanner(TileSize);
            var crate = new Vector2Int(3, 3);
            var shelf = new Vector2Int(2, 8);
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
                Assert.That(room.TryClaim(new[] { crate }, RoomTileTags.Prop), Is.True);
                room.SetPropHeight(crate, 1f);
                Assert.That(room.TryClaim(new[] { shelf }, RoomTileTags.Prop), Is.True);
                room.SetPropHeight(shelf, 2.4f);
                OvergrowthField field = planner.Plan(room, new OvergrowthRequest(profile), Stream(seed));
                if (field == null) continue;
                string at = $"seed {seed}";

                Assert.That(field.Covers(crate), Is.False, $"{at}: the crate stands on the floor");
                Assert.That(field.Covers(shelf), Is.False, $"{at}: the shelf stands on the floor");
                foreach (Vector2Int side in GridNeighbours.Four)
                    Assert.That(field.Covers(crate + side), Is.False, $"{at}: whoever crouches behind the low crate is hidden by all of it");
            }
        }

        [Test]
        public void BothSidesOfALedgeStayBare()
        {
            ElevationStructureRule platform = PlatformRule();
            OvergrowthProfile profile = Profile();
            var planner = new OvergrowthPlanner(TileSize);
            int ledges = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(16, 12, new Vector2Int(15, 6));
                room.AllowLevels(-1, 1);
                platform.Apply(room, Stream(seed));
                OvergrowthField field = planner.Plan(room, new OvergrowthRequest(profile), Stream(seed));
                if (field == null) continue;

                foreach (Vector2Int tile in room.Footprint.Tiles)
                    foreach (Vector2Int side in GridNeighbours.Four)
                    {
                        if (!room.Contains(tile + side) || room.LevelOf(tile + side) == room.LevelOf(tile)) continue;
                        ledges++;
                        Assert.That(field.Covers(tile), Is.False, $"seed {seed}: {tile} beside a change of level stays bare");
                    }
                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (room.Has(tile, RoomTileTags.Stairs | RoomTileTags.Landing))
                        Assert.That(field.Covers(tile), Is.False, $"seed {seed}: stairs and their landings stay bare");
            }
            Assert.That(ledges, Is.GreaterThan(0), "some room got a platform");
        }

        [Test]
        public void GrowthKeepsToThePartOfTheRoomItWasGiven()
        {
            OvergrowthProfile profile = Profile();
            var planner = new OvergrowthPlanner(TileSize);
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(0, 5));
                OvergrowthField field = planner.Plan(room, new OvergrowthRequest(profile, 1f, tile => tile.x >= 6), Stream(seed));
                if (field == null) continue;

                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (tile.x < 6) Assert.That(field.Covers(tile), Is.False, $"seed {seed}: {tile} is not the growth's");
            }
        }

        [Test]
        public void NoProfileGrowsNothing()
        {
            Assert.That(new OvergrowthPlanner(TileSize).Plan(Room(8, 8, new Vector2Int(7, 4)), new OvergrowthRequest(null), Stream(1)), Is.Null);
        }

        // --- Helpers -------------------------------------------------------------------------

        private static RandomStream Stream(int seed) => new(new Seed((ulong)seed));

        // Spread wide, so each test room grows plenty.
        private static OvergrowthProfile Profile(float height = 0.3f, float maxSlope = 20f)
        {
            var profile = ScriptableObject.CreateInstance<OvergrowthProfile>();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("_height").floatValue = height;
            serialized.FindProperty("_maxSlope").floatValue = maxSlope;
            serialized.FindProperty("_coverage").floatValue = 1f;
            serialized.FindProperty("_wallPile").floatValue = 1f;
            serialized.FindProperty("_patches").FindPropertyRelative("Min").intValue = 4;
            serialized.FindProperty("_patches").FindPropertyRelative("Max").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ElevationStructureRule PlatformRule()
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)ElevationKind.Platform;
            serialized.FindProperty("_maxShare").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }
    }
}
