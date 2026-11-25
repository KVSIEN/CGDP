using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CGD.Algorithms;
using CGD.Core;

namespace CGD.Tests
{
    // The heap pops in order, the shuffle bag deals every item once a round, union-find groups,
    // Kruskal keeps the cheapest connecting links, the spatial hash finds what brute force finds.
    public class CollectionAndGraphTests
    {
        [Test]
        public void TheHeapPopsLowestFirst()
        {
            var random = new RandomStream(new Seed(8));
            var heap = new MinHeap<float>();
            for (int i = 0; i < 300; i++)
            {
                float priority = random.Range(0f, 100f);
                heap.Push(priority, priority);
            }
            float last = float.MinValue;
            while (heap.TryPop(out float value))
            {
                Assert.That(value, Is.GreaterThanOrEqualTo(last));
                last = value;
            }
        }

        [Test]
        public void TheShuffleBagDealsEveryItemOnceARound()
        {
            var bag = new ShuffleBag<string>(new RandomStream(new Seed(3)));
            bag.Add("a");
            bag.Add("b");
            bag.Add("c", copies: 2);
            for (int round = 0; round < 20; round++)
            {
                string drawn = string.Concat(Enumerable.Range(0, 4).Select(_ => bag.Next()).OrderBy(item => item));
                Assert.That(drawn, Is.EqualTo("abcc"), $"round {round}");
            }
        }

        [Test]
        public void TheDisjointSetJoinsGroups()
        {
            var set = new DisjointSet(6);
            set.Union(0, 1);
            set.Union(2, 3);
            set.Union(1, 3);
            Assert.That(set.AreConnected(0, 2), Is.True);
            Assert.That(set.AreConnected(0, 4), Is.False);
            Assert.That(set.GroupCount, Is.EqualTo(3));
            Assert.That(set.Union(0, 3), Is.False, "already joined");
        }

        [Test]
        public void KruskalKeepsTheCheapestLinksThatConnectEverything()
        {
            var edges = new[]
            {
                new GraphEdge(0, 1, 1f), new GraphEdge(1, 2, 2f), new GraphEdge(2, 3, 1f),
                new GraphEdge(3, 0, 3f), new GraphEdge(0, 2, 5f), new GraphEdge(1, 3, 1.5f),
            };
            List<GraphEdge> tree = MinimumSpanningTree.Kruskal(4, edges);
            Assert.That(tree.Count, Is.EqualTo(3));
            Assert.That(tree.Sum(edge => edge.Weight), Is.EqualTo(3.5f).Within(1e-5f));
        }

        [Test]
        public void TheSpatialHashFindsWhatBruteForceFinds()
        {
            var random = new RandomStream(new Seed(30));
            var grid = new SpatialHashGrid<int>(3f);
            var positions = new List<Vector2>();
            for (int i = 0; i < 200; i++)
            {
                var position = new Vector2(random.Range(-30f, 30f), random.Range(-30f, 30f));
                positions.Add(position);
                grid.Add(i, position);
            }

            var found = new List<int>();
            for (int query = 0; query < 30; query++)
            {
                var centre = new Vector2(random.Range(-30f, 30f), random.Range(-30f, 30f));
                float radius = random.Range(1f, 10f);
                grid.Query(centre, radius, found);
                IEnumerable<int> expected = Enumerable.Range(0, positions.Count).Where(i => (positions[i] - centre).sqrMagnitude <= radius * radius);
                Assert.That(found.OrderBy(i => i), Is.EqualTo(expected));
            }
        }
    }
}
