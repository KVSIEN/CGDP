using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CGD.Algorithms;
using CGD.Core;

namespace CGD.Tests
{
    // A* finds the shortest way, the distance map leads to its sources, lines have no gaps,
    // caves and walks repeat for a seed and stay connected.
    public class GridAlgorithmTests
    {
        private const int Size = 30;

        [Test]
        public void AStarFindsTheShortestPathTheDistanceMapAgreesWith()
        {
            int reachable = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                bool[,] wall = RandomWalls(seed, 0.22f);
                bool Walkable(Vector2Int tile) => !wall[tile.x, tile.y];
                var start = new Vector2Int(0, 0);
                var goal = new Vector2Int(Size - 1, Size - 1);

                GridDistanceMap map = GridDistanceMap.Build(Size, Size, new[] { goal }, Walkable);
                List<Vector2Int> path = GridPathfinder.FindPath(Size, Size, start, goal, Walkable);
                if (map.DistanceAt(start) == GridDistanceMap.Unreachable)
                {
                    Assert.That(path, Is.Null, $"seed {seed}: no route");
                    continue;
                }

                reachable++;
                Assert.That(path.Count - 1, Is.EqualTo(map.DistanceAt(start)), $"seed {seed}: shortest");
                Vector2Int at = start;
                while (map.TryStepToward(at, out Vector2Int next)) at = next;
                Assert.That(at, Is.EqualTo(goal), $"seed {seed}: stepping down the map arrives");
            }
            Assert.That(reachable, Is.GreaterThan(10));
        }

        [Test]
        public void AStarGoesRoundCostlyTilesAndReportsNoRoute()
        {
            List<Vector2Int> round = GridPathfinder.FindPath(5, 3, new Vector2Int(0, 1), new Vector2Int(4, 1), _ => true,
                                                             stepCost: tile => tile == new Vector2Int(2, 1) ? 10f : 1f);
            Assert.That(round, Does.Not.Contain(new Vector2Int(2, 1)));
            Assert.That(GridPathfinder.FindPath(5, 5, Vector2Int.zero, new Vector2Int(4, 4), tile => tile.x != 2), Is.Null);
        }

        [Test]
        public void ALineVisitsEveryTileBetweenItsEndsOnce()
        {
            var random = new RandomStream(new Seed(5));
            for (int i = 0; i < 100; i++)
            {
                var from = new Vector2Int(random.Range(-20, 20), random.Range(-20, 20));
                var to = new Vector2Int(random.Range(-20, 20), random.Range(-20, 20));
                List<Vector2Int> line = GridLine.Between(from, to);
                Assert.That(line[0], Is.EqualTo(from));
                Assert.That(line[line.Count - 1], Is.EqualTo(to));
                Assert.That(line.Count, Is.EqualTo(Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y)) + 1));
            }
            Assert.That(GridLine.IsClear(Vector2Int.zero, new Vector2Int(6, 0), tile => tile.x == 3), Is.False);
        }

        [Test]
        public void CavesRepeatForASeedAndAWalkStaysConnected()
        {
            bool[,] Cave() => CellularAutomaton.Run(CellularAutomaton.RandomFill(40, 20, 0.45f, new RandomStream(new Seed(21))), 5);
            Assert.That(Cave(), Is.EqualTo(Cave()));

            HashSet<Vector2Int> walked = DrunkardsWalk.Walk(new Vector2Int(10, 10), 400, new RandomStream(new Seed(2)),
                                                            tile => GridNeighbours.InBounds(tile, 20, 20));
            GridDistanceMap reach = GridDistanceMap.Build(20, 20, new[] { new Vector2Int(10, 10) }, walked.Contains);
            Assert.That(walked.All(tile => reach.DistanceAt(tile) != GridDistanceMap.Unreachable), Is.True);
        }

        private static bool[,] RandomWalls(ulong seed, float chance)
        {
            var random = new RandomStream(new Seed(seed));
            var wall = new bool[Size, Size];
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++) wall[x, y] = random.Chance(chance);
            wall[0, 0] = wall[Size - 1, Size - 1] = false;
            return wall;
        }
    }
}
