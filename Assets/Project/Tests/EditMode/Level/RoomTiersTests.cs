using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Floor in tiers: any number of levels up or down, as the room's range and ceiling allow,
    // climbed a level at a time. Each elevation feature and each terrace profile can be built
    // several levels deep, every tier reached from the one below it, and no tile ends up more
    // than a level from its neighbour.
    public class RoomTiersTests
    {
        private const int Seeds = 12;

        // Rooms are 4 m high (RoomTestKit): floor up to level 2 leaves the 2 m of headroom, level 3 doesn't.
        [Test]
        public void ALevelStandsWithinTheRangeAndUnderTheCeiling()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            room.AllowLevels(-3, 5);

            Assert.That(room.AllowsLevel(-3), Is.True);
            Assert.That(room.AllowsLevel(-4), Is.False, "below the range");
            Assert.That(room.AllowsLevel(2), Is.True);
            Assert.That(room.AllowsLevel(3), Is.False, "no headroom under a 4 m ceiling");
            Assert.That(room.TiersAllowed(1, 5), Is.EqualTo(2));
            Assert.That(room.TiersAllowed(-1, 5), Is.EqualTo(3));
            Assert.That(room.TiersAllowed(-1, 2), Is.EqualTo(2), "never more than wanted");
        }

        [Test]
        public void AllowedLevelsAreNoLongerCappedAtOneDown()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            room.AllowLevels(-4, 0);

            Assert.That(room.LowestLevel, Is.EqualTo(-4));
        }

        [Test]
        public void TieredFeaturesClimbALevelPerTier(
            [Values(ElevationKind.Platform, ElevationKind.SunkenFloor, ElevationKind.Mound, ElevationKind.Crater)] ElevationKind kind)
        {
            ElevationStructureRule rule = Rule(kind, tiers: 2);
            int deepest = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(22, 18, new Vector2Int(21, 9));
                room.AllowLevels(-3, 3);
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                string at = $"seed {seed}";

                AssertSound(room, at);
                foreach (ElevatedArea area in room.Elevations)
                {
                    Assert.That(Mathf.Abs(area.Level), Is.InRange(1, 2), $"{at}: no more tiers than asked for");
                    Assert.That(area.Stairs, Is.Not.Empty, $"{at}: every tier has its way up");
                    deepest = Mathf.Max(deepest, Mathf.Abs(area.Level));
                }
            }
            Assert.That(deepest, Is.EqualTo(2), "some room got both tiers");
        }

        [Test]
        public void OneTierBuildsThePlainFeature([Values(ElevationKind.Platform, ElevationKind.Mound)] ElevationKind kind)
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure plain = Room(22, 18, new Vector2Int(21, 9));
                RoomStructure tiered = Room(22, 18, new Vector2Int(21, 9));
                plain.AllowLevels(-3, 3);
                // Two tiers asked for, but the room only allows one level up.
                tiered.AllowLevels(-3, 1);
                Rule(kind, tiers: 1).Apply(plain, new RandomStream(new Seed((ulong)seed)));
                Rule(kind, tiers: 2).Apply(tiered, new RandomStream(new Seed((ulong)seed)));

                foreach (Vector2Int tile in plain.Footprint.Tiles)
                    Assert.That(tiered.LevelOf(tile), Is.EqualTo(plain.LevelOf(tile)), $"seed {seed}, {tile}");
            }
        }

        [Test]
        public void RaisedWalksAtTwoTiersStepUpToBothWalls()
        {
            // 16 by 14, doorways at both ends of the middle: rows 0–1 at 2, 2–3 at 1, 4–9 at 0, 10–11 at 1,
            // 12–13 at 2. Three tiers would fit across, but level 3 has no headroom under a 4 m ceiling.
            RoomStructure room = Room(16, 14, new Vector2Int(15, 7), new Vector2Int(0, 6));
            room.AllowLevels(-3, 3);
            Terraces(tiers: (1, 3), (1, 1f), (0, 4f), (1, 1f)).Apply(room, new RandomStream(new Seed(1UL)));

            Assert.That(room.LevelOf(new Vector2Int(3, 0)), Is.EqualTo(2), "the south walk, two tiers up");
            Assert.That(room.LevelOf(new Vector2Int(3, 2)), Is.EqualTo(1), "the step below it");
            Assert.That(room.LevelOf(new Vector2Int(3, 7)), Is.Zero, "the middle, level with the doorways");
            Assert.That(room.LevelOf(new Vector2Int(3, 13)), Is.EqualTo(2), "the north walk");
            AssertSound(room, "raised walks");
        }

        [Test]
        public void ASunkenCourtDeepensAsTheRangeAllows()
        {
            // 16 by 14, doorways in the corners of the edge lanes, far enough from the deepest lane
            // that the floor kept level in front of them steps only a level down.
            RoomStructure room = Room(16, 14, new Vector2Int(15, 0), new Vector2Int(0, 13));
            room.AllowLevels(-2, 0);
            Terraces(tiers: (1, 4), (0, 1f), (-1, 4f), (0, 1f)).Apply(room, new RandomStream(new Seed(1UL)));

            int lowest = 0;
            foreach (Vector2Int tile in room.Footprint.Tiles) lowest = Mathf.Min(lowest, room.LevelOf(tile));
            Assert.That(lowest, Is.EqualTo(-2), "as deep as the range lets it go, no deeper");
            AssertSound(room, "sunken court");
        }

        [Test]
        public void TooNarrowARoomForMoreTiersKeepsTheProfile()
        {
            // 9 rows: room for three lanes of two and more, not for five of two.
            RoomStructure room = Room(16, 9, new Vector2Int(15, 4), new Vector2Int(0, 4));
            room.AllowLevels(-3, 3);
            Terraces(tiers: (1, 2), (1, 1f), (0, 2f), (1, 1f)).Apply(room, new RandomStream(new Seed(1UL)));

            int highest = 0;
            foreach (Vector2Int tile in room.Footprint.Tiles) highest = Mathf.Max(highest, room.LevelOf(tile));
            Assert.That(highest, Is.EqualTo(1));
        }

        // Every tile reachable both ways, every doorway on level 0, no neighbours two levels apart.
        private static void AssertSound(RoomStructure room, string at)
        {
            Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"{at}: every tile reachable both ways");
            foreach (Vector2Int entrance in room.Entrances)
                Assert.That(room.LevelOf(entrance), Is.Zero, $"{at}: a doorway opens onto the doorways' level");
            foreach (Vector2Int tile in room.Footprint.Tiles)
                foreach (Vector2Int side in GridSides.All)
                    if (room.Contains(tile + side))
                        Assert.That(Mathf.Abs(room.LevelOf(tile) - room.LevelOf(tile + side)), Is.LessThanOrEqualTo(1), $"{at}: {tile} beside {tile + side}");
        }

        private static ElevationStructureRule Rule(ElevationKind kind, int tiers)
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            SetRange(serialized, "_tiers", tiers, tiers);
            // Big enough for a second tier: a platform's two tiles in, a sunken floor's two all round.
            SetRange(serialized, "_depth", 5, 6);
            SetRange(serialized, "_length", 6, 8);
            SetRange(serialized, "_radius", 2, 2);
            serialized.FindProperty("_maxShare").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static TerracesStructureRule Terraces((int min, int max) tiers, params (int level, float weight)[] lanes)
        {
            var rule = ScriptableObject.CreateInstance<TerracesStructureRule>();
            var serialized = new SerializedObject(rule);
            SerializedProperty list = serialized.FindProperty("_lanes");
            list.arraySize = lanes.Length;
            for (int i = 0; i < lanes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Level").intValue = lanes[i].level;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").floatValue = lanes[i].weight;
            }
            SetRange(serialized, "_tiers", tiers.min, tiers.max);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static void SetRange(SerializedObject serialized, string name, int min, int max)
        {
            serialized.FindProperty(name).FindPropertyRelative("Min").intValue = min;
            serialized.FindProperty(name).FindPropertyRelative("Max").intValue = max;
        }
    }
}
