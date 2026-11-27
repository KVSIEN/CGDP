using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // Diagonal corridors: a routed corridor's square bends are cut into staircases of tiles
    // (CorridorCorners), and the wall plan walls each staircase with one straight 45° wall a side.
    public class DiagonalCorridorTests
    {
        private static readonly Vector2Int Right = Vector2Int.right, Up = Vector2Int.up, Left = Vector2Int.left;

        // A corridor from `start`, `length` tiles along each leg in turn.
        private static List<Vector2Int> Walk(Vector2Int start, params (Vector2Int way, int length)[] legs)
        {
            var path = new List<Vector2Int> { start };
            foreach (var (way, length) in legs)
                for (int i = 0; i < length; i++) path.Add(path[path.Count - 1] + way);
            return path;
        }

        private static void AssertWhole(List<Vector2Int> path)
        {
            for (int i = 1; i < path.Count; i++)
                Assert.That((path[i] - path[i - 1]).sqrMagnitude, Is.EqualTo(1), $"step {i} is one tile");
            for (int i = 0; i < path.Count; i++)
                for (int j = i + 2; j < path.Count; j++)
                    Assert.That((path[i] - path[j]).sqrMagnitude, Is.GreaterThan(1), $"tiles {i} and {j} touch: the corridor widens or loops");
        }

        [Test]
        public void ABendIsCutIntoAStaircaseAsLongAsBefore()
        {
            List<Vector2Int> path = Walk(Vector2Int.zero, (Right, 8), (Up, 8));
            List<Vector2Int> cut = CorridorCorners.Cut(path, 3, _ => true);

            Assert.That(cut.Count, Is.EqualTo(path.Count));
            Assert.That(cut[0], Is.EqualTo(path[0]));
            Assert.That(cut[cut.Count - 1], Is.EqualTo(path[path.Count - 1]));
            Assert.That(cut, Does.Not.Contain(new Vector2Int(8, 0)), "the square corner is gone");
            Assert.That(cut, Does.Contain(new Vector2Int(6, 1)).And.Contain(new Vector2Int(7, 2)), "the staircase crosses the inside of the bend");
            AssertWhole(cut);
        }

        [Test]
        public void ABendIsLeftSquareWhenItsArmsAreShortOrTheInsideIsTaken()
        {
            List<Vector2Int> shortArms = Walk(Vector2Int.zero, (Right, 2), (Up, 2));
            Assert.That(CorridorCorners.Cut(shortArms, 3, _ => true), Is.EqualTo(shortArms));

            List<Vector2Int> path = Walk(Vector2Int.zero, (Right, 8), (Up, 8));
            Assert.That(CorridorCorners.Cut(path, 3, tile => tile != new Vector2Int(6, 1)), Is.EqualTo(path), "a tile inside the bend is taken");
            Assert.That(CorridorCorners.Cut(path, 0, _ => true), Is.EqualTo(path), "no steps: square bends");
        }

        [Test]
        public void ATightUTurnTakesAShorterCut()
        {
            List<Vector2Int> path = Walk(Vector2Int.zero, (Right, 6), (Up, 4), (Left, 6));
            List<Vector2Int> cut = CorridorCorners.Cut(path, 3, _ => true);
            Assert.That(cut, Is.Not.EqualTo(path), "a two-step cut fits inside the turn");
            AssertWhole(cut);
        }

        [Test]
        public void ACorridorsGuideStripeCutsItsBendsOnTheDiagonal()
        {
            List<Vector2Int> path = CorridorCorners.Cut(Walk(Vector2Int.zero, (Right, 8), (Up, 8)), 3, _ => true);
            List<Vector2> line = FloorStripeBuilder.GuideLine(path);
            Assert.That(line, Is.EqualTo(new List<Vector2> { new(0.5f, 0.5f), new(5f, 0.5f), new(8.5f, 4f), new(8.5f, 8.5f) }),
                        "straight, one 45° run down the staircase, straight");
        }

        [Test]
        public void AStaircaseIsWalledByOneStraightDiagonalEachSide()
        {
            var settings = ScriptableObject.CreateInstance<LevelBuildSettings>();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_chamferCorridorBends").boolValue = true;
            serialized.FindProperty("_diagonalCorridorSteps").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var layout = new LevelLayout(settings.TileSize);
            List<Vector2Int> path = CorridorCorners.Cut(Walk(Vector2Int.zero, (Right, 8), (Up, 8)), 3, _ => true);
            layout.AddCorridor(new MapConnection(0, 1, ConnectionType.Normal), path);
            LevelWallPlan plan = LevelWallPlan.Create(layout, settings);

            int diagonals = 0;
            foreach (LevelWallPlan.Segment segment in plan.Segments)
            {
                Vector2 run = segment.To - segment.From;
                bool square = Mathf.Approximately(run.x, 0f) || Mathf.Approximately(run.y, 0f);
                bool diagonal = Mathf.Approximately(Mathf.Abs(run.x), Mathf.Abs(run.y));
                Assert.That(square || diagonal, Is.True, $"{segment.From} → {segment.To} is square or 45°");
                if (diagonal && run.magnitude > 2f) diagonals++;
            }
            Assert.That(diagonals, Is.EqualTo(2), "one long diagonal wall a side");
            // No jog: where a long diagonal ends, no half-tile piece turns off it at a right angle to
            // a straight wall; the diagonal runs on to meet that wall instead.
            foreach (LevelWallPlan.Segment diagonal in plan.Segments)
            {
                if ((diagonal.To - diagonal.From).magnitude <= 2f) continue;
                foreach (LevelWallPlan.Segment stub in plan.Segments)
                {
                    Vector2 run = stub.To - stub.From;
                    if (!Mathf.Approximately(run.magnitude, 0.5f) || (stub.From != diagonal.To && stub.To != diagonal.From)) continue;
                    Vector2 far = stub.From == diagonal.To ? stub.To : stub.From;
                    foreach (LevelWallPlan.Segment next in plan.Segments)
                    {
                        if (next.From != far && next.To != far || next.From == stub.From && next.To == stub.To) continue;
                        Vector2 on = next.To - next.From;
                        Assert.That(Mathf.Abs(on.x * run.y - on.y * run.x), Is.LessThan(1e-4f), $"a jog at {far}");
                    }
                }
            }
            Assert.That(plan.Fills, Is.Not.Empty, "the notches the walls take in are floored");
            foreach (var (a, corner, b) in plan.Fills)
            {
                Vector2 middle = (a + corner + b) / 3f;
                Assert.That(layout.IsWalkable(new Vector2Int(Mathf.FloorToInt(middle.x), Mathf.FloorToInt(middle.y))), Is.False,
                            "a fill lies on the solid tile beside the staircase");
            }
        }
    }
}
