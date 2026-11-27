using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Platforms and islands with their corners cut at 45°: each corner tile turns solid, half
    // raised toward the platform, with the ledge's guard running across its diagonal.
    public class CutCornerTests
    {
        private static ElevationStructureRule Rule(ElevationKind kind, ElevationRole role, int depth, int length, float cut)
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            serialized.FindProperty("_role").enumValueIndex = (int)role;
            serialized.FindProperty("_depth").FindPropertyRelative("Min").intValue = depth;
            serialized.FindProperty("_depth").FindPropertyRelative("Max").intValue = depth;
            serialized.FindProperty("_length").FindPropertyRelative("Min").intValue = length;
            serialized.FindProperty("_length").FindPropertyRelative("Max").intValue = length;
            serialized.FindProperty("_cutCorners").floatValue = cut;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        [Test]
        public void AStageCutsItsFrontCornersAndAnIslandAllFour(
            [Values(ElevationKind.Platform, ElevationKind.Island)] ElevationKind kind)
        {
            ElevationRole role = kind == ElevationKind.Platform ? ElevationRole.Stage : ElevationRole.Bastion;
            ElevationStructureRule rule = Rule(kind, role, kind == ElevationKind.Platform ? 3 : 4, 5, cut: 1f);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(14, 12, new Vector2Int(7, 0));
                room.SetArrival(new Vector2Int(7, 0));
                room.AllowLevels(0, 1);
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                if (room.Elevations.Count == 0) continue;
                built++;

                ElevatedArea area = room.Elevations[0];
                int cut = 0;
                foreach (GroundCorner corner in area.Corners)
                {
                    Assert.That(corner.Sheer, Is.True);
                    cut++;
                    Assert.That(room.Has(corner.Tile, RoomTileTags.Structure), Is.True, $"seed {seed}: the corner tile is solid");
                    Assert.That(room.LevelOf(corner.Tile), Is.EqualTo(0));
                    Assert.That(area.Tiles, Does.Contain(corner.Tile + new Vector2Int(corner.Up.x, 0))
                                               .And.Member(corner.Tile + new Vector2Int(0, corner.Up.y)), "the platform on both sides of it");
                    foreach (StairFlight stairs in area.Stairs)
                        Assert.That(stairs.Tile != corner.Tile && stairs.Foot != corner.Tile, Is.True, "no flight on a cut corner");
                }
                Assert.That(cut, Is.EqualTo(kind == ElevationKind.Island ? 4 : 2), $"seed {seed}");
                Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True);
            }
            Assert.That(built, Is.GreaterThan(Seeds / 2));
        }

        [Test]
        public void NoCornerIsCutWithoutTheChanceOrOnABlockTooSmall()
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure square = Room(14, 12, new Vector2Int(7, 0));
                square.AllowLevels(0, 1);
                Rule(ElevationKind.Island, ElevationRole.Bastion, 4, 5, cut: 0f).Apply(square, new RandomStream(new Seed((ulong)seed)));
                RoomStructure small = Room(14, 12, new Vector2Int(7, 0));
                small.AllowLevels(0, 1);
                Rule(ElevationKind.Island, ElevationRole.Bastion, 2, 3, cut: 1f).Apply(small, new RandomStream(new Seed((ulong)seed)));
                foreach (RoomStructure room in new[] { square, small })
                    foreach (ElevatedArea area in room.Elevations)
                        Assert.That(area.Corners, Is.Empty, $"seed {seed}");
            }
        }
    }
}
