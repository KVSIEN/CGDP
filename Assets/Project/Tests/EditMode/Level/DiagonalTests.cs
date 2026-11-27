using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // 45° walls on the square grid: each runs corner to corner across a tile, so runs, chevrons
    // and the room's walls always meet; the tile is solid to planning and seen over when low.
    public class DiagonalTests
    {
        [Test]
        public void ARunJoinsCornerToCorner()
        {
            foreach (Vector2Int step in new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) })
            {
                List<DiagonalWall> run = DiagonalWall.Run(new Vector2Int(3, 3), step, 4);
                for (int i = 1; i < run.Count; i++)
                {
                    DiagonalWall a = run[i - 1], b = run[i];
                    bool joined = a.To == b.From || a.To == b.To || a.From == b.From || a.From == b.To;
                    Assert.That(joined, Is.True, $"step {step}: piece {i} meets the one before it");
                    Assert.That(Mathf.Abs((a.To - a.From).x), Is.EqualTo(1f), "corner to corner");
                }
            }
        }

        [Test]
        public void AQuarterTurnSwapsTheDiagonalAndFourComeBack()
        {
            var wall = new DiagonalWall(new Vector2Int(2, 5), rising: true);
            Assert.That(wall.Turned(1).Rising, Is.False);
            Assert.That(wall.Turned(4).Tile, Is.EqualTo(wall.Tile));
            Assert.That(wall.Turned(4).Rising, Is.True);
        }

        [Test]
        public void AChevronsArmsMeetAtItsPointWhicheverWayItFaces()
        {
            for (int turns = 0; turns < 4; turns++)
            {
                List<DiagonalWall> chevron = ChevronCoverStructureRule.Chevron(2, turns, new Vector2Int(5, 5), 1.1f);
                Assert.That(chevron.Count, Is.EqualTo(4));
                var ends = new Dictionary<Vector2, int>();
                foreach (DiagonalWall wall in chevron)
                {
                    ends[wall.From] = ends.TryGetValue(wall.From, out int f) ? f + 1 : 1;
                    ends[wall.To]   = ends.TryGetValue(wall.To, out int t) ? t + 1 : 1;
                }
                int loose = 0;
                foreach (int uses in ends.Values) if (uses == 1) loose++;
                Assert.That(loose, Is.EqualTo(2), $"turns {turns}: one unbroken line with two free ends");
            }
        }

        [Test]
        public void ADiagonalTakesItsTileAndKeepsTheRoomWhole()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            List<DiagonalWall> run = DiagonalWall.Run(new Vector2Int(1, 6), new Vector2Int(1, 1), 1);
            Assert.That(room.TryAddDiagonals(run), Is.True);
            Assert.That(room.Has(new Vector2Int(1, 6), RoomTileTags.Structure), Is.True);
            Assert.That(room.TryGetDiagonal(new Vector2Int(1, 6), out DiagonalWall stood), Is.True);
            Assert.That(stood.Rising, Is.True);

            // A wall right across the room would cut it in two.
            var across = new List<DiagonalWall>();
            for (int y = 0; y < 8; y++) across.Add(new DiagonalWall(new Vector2Int(4, y), rising: y % 2 == 0));
            Assert.That(room.TryAddDiagonals(across), Is.False);
            Assert.That(room.Diagonals.Count, Is.EqualTo(1), "all or none");

            Assert.That(room.TryAddDiagonals(new[] { new DiagonalWall(new Vector2Int(9, 4), true) }), Is.False, "never on a doorway's front");
        }

        [Test]
        public void ALowDiagonalIsSeenOverAndAFullOneIsNot()
        {
            foreach (float height in new[] { 1.1f, 0f })
            {
                RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
                Assert.That(room.TryAddDiagonals(new[] { new DiagonalWall(new Vector2Int(3, 1), true, height) }), Is.True);
                bool seen = new SightBlocking(room).CanSee(new Vector2Int(1, 1), new Vector2Int(6, 1));
                Assert.That(seen, Is.EqualTo(height > 0f), height > 0f ? "stood over" : "full height");
                if (height > 0f)
                    Assert.That(new SightBlocking(room, SightBlocking.CrouchingEyeHeight).CanSee(new Vector2Int(2, 1), new Vector2Int(6, 1)), Is.False,
                                "crouched behind it");
            }
        }

        [Test]
        public void APropAgainstAnAngledWallFacesStraightOutOfIt()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            Assert.That(room.TryAddDiagonals(new[] { new DiagonalWall(new Vector2Int(3, 2), true) }), Is.True);
            RoomPropPlanner planner = Planner(room, 1);
            RoomPropPlacement planter = Placement(2, 2, PropFacing.AgainstDiagonal);
            planner.Add(planter);

            Assert.That(planner.Planned.Count, Is.EqualTo(2));
            foreach (PlannedProp prop in planner.Planned)
            {
                Vector2Int tile = TileOf(prop);
                Assert.That(Mathf.Abs(tile.x - 3) + Mathf.Abs(tile.y - 2), Is.EqualTo(1), "beside the wall");
                float yaw = Mathf.Repeat(prop.Yaw, 360f);
                Assert.That(Mathf.Repeat(yaw - 45f, 90f), Is.EqualTo(0f).Within(0.01f), "on a diagonal");
                // A "/" wall faces (1, -1) toward the floor right of or below it, (-1, 1) toward the rest.
                bool rightOrBelow = tile.x > 3 || tile.y < 2;
                Assert.That(yaw, Is.EqualTo(rightOrBelow ? 135f : 315f).Within(0.01f), $"facing away from it at {tile}");
            }
        }

        [Test]
        public void ADiagonalPropTurnsFortyFiveDegreesOffSquare()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            RoomPropPlanner planner = Planner(room, 2);
            planner.Add(Placement(3, 3, PropFacing.Diagonal));
            Assert.That(planner.Planned.Count, Is.EqualTo(3));
            foreach (PlannedProp prop in planner.Planned)
                Assert.That(Mathf.Repeat(prop.Yaw - 45f, 90f), Is.EqualTo(0f).Within(0.01f));
        }

        // --- Rules and shapes -----------------------------------------------------------

        private static AngledCornerStructureRule CornerRule(float height)
        {
            var rule = ScriptableObject.CreateInstance<AngledCornerStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_height").floatValue = height;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        [Test]
        public void AnAngledCornerRunsWallToWallWithAWayIn()
        {
            AngledCornerStructureRule rule = CornerRule(0f);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(11, 5), new Vector2Int(6, 0));
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"seed {seed}: the corner is reached through its gap");
                if (room.Diagonals.Count == 0) continue;

                built++;
                var ends = new Dictionary<Vector2, int>();
                foreach (DiagonalWall wall in room.Diagonals)
                {
                    ends[wall.From] = ends.TryGetValue(wall.From, out int f) ? f + 1 : 1;
                    ends[wall.To]   = ends.TryGetValue(wall.To, out int t) ? t + 1 : 1;
                }
                bool onTheWalls = false;
                foreach (var (point, uses) in ends)
                    if (uses == 1 && (point.x == 0 || point.x == 12 || point.y == 0 || point.y == 10)) onTheWalls = true;
                Assert.That(onTheWalls, Is.True, $"seed {seed}: it meets the room's walls");
            }
            Assert.That(built, Is.GreaterThan(Seeds / 2));
        }

        [Test]
        public void AnOctagonsSlantedEdgesStepOneTileAtATime()
        {
            var shape = ScriptableObject.CreateInstance<RoomShape>();
            var serialized = new SerializedObject(shape);
            SerializedProperty parts = serialized.FindProperty("_parts");
            parts.arraySize = 1;
            SerializedProperty part = parts.GetArrayElementAtIndex(0);
            part.FindPropertyRelative("Kind").enumValueIndex = (int)RoomShapeKind.Octagon;
            part.FindPropertyRelative("Center").vector2Value = new Vector2(0.5f, 0.5f);
            part.FindPropertyRelative("SizeMin").vector2Value = Vector2.one;
            part.FindPropertyRelative("SizeMax").vector2Value = Vector2.one;
            serialized.FindProperty("_angledWalls").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RoomFootprint footprint = RoomShapeRasterizer.Rasterize(shape, new RectInt(0, 0, 14, 10), new RandomStream(new Seed(1)));
            Assert.That(footprint.CurvedWalls && footprint.SharpDiagonals, Is.True);
            Assert.That(footprint.Contains(new Vector2Int(0, 0)), Is.False, "the corner is cut");
            Assert.That(footprint.Contains(new Vector2Int(7, 5)), Is.True);
            // Row by row, each row's floor starts at most one tile further in than its neighbour's.
            int previous = -1;
            for (int y = 0; y < 10; y++)
            {
                int first = 0;
                while (!footprint.Contains(new Vector2Int(first, y))) first++;
                if (previous >= 0) Assert.That(Mathf.Abs(first - previous), Is.LessThanOrEqualTo(1), $"row {y}");
                previous = first;
            }
        }
    }
}
