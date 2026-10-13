using NUnit.Framework;
using UnityEngine;
using CGD.Map;

namespace CGD.Tests
{
    // A hand edit to any authored field of a room or link must count as an edit: the Map Graph
    // window decides what Revert Edits undoes, and whether regenerating asks first, from SameAs.
    public class MapGraphEditStateTests
    {
        private static MapGraph CreateGraph()
        {
            var graph = new MapGraph();
            MapNode start = graph.AddNode(MapNodeType.Start, Vector2.zero);
            MapNode boss  = graph.AddNode(MapNodeType.Boss, new Vector2(220f, 0f));
            graph.Connect(start.Id, boss.Id, ConnectionType.Locked);
            graph.GetConnection(start.Id, boss.Id).AddKeyRoom(start.Id);
            return graph;
        }

        private static MapNode FirstNode(MapGraph graph) => graph.Nodes[0];
        private static MapConnection FirstLink(MapGraph graph) => graph.Connections[0];

        [Test]
        public void AnUneditedCopyIsTheSame()
        {
            MapGraph graph = CreateGraph();
            MapGraph copy  = graph.Clone();

            Assert.IsTrue(FirstNode(copy).SameAs(FirstNode(graph)));
            Assert.IsTrue(FirstLink(copy).SameAs(FirstLink(graph)));
        }

        [Test]
        public void EveryNodeEditCounts()
        {
            MapGraph graph = CreateGraph();

            AssertEdited(graph, n => n.Type = MapNodeType.Boss);
            AssertEdited(graph, n => n.Position += Vector2.one);
            AssertEdited(graph, n => n.Intensity = 0.7f);
            AssertEdited(graph, n => n.Tier = 3);
            AssertEdited(graph, n => n.SetFaction(1, 0.5f));
            AssertEdited(graph, n => n.BreachFaction = 2);
            AssertEdited(graph, n => n.Section = 1);
            AssertEdited(graph, n => n.Category = RoomCategory.Commercial);
        }

        [Test]
        public void EveryLinkEditCounts()
        {
            MapGraph graph = CreateGraph();

            AssertEdited(graph, c => c.Type = ConnectionType.Secret);
            AssertEdited(graph, c => c.Lock = MapLockKind.Terminals);
            AssertEdited(graph, c => c.AddKeyRoom(FirstNode(graph).Id + 1));
            AssertEdited(graph, c => c.ClearKeyRooms());
            AssertEdited(graph, c => c.OneWay = true);
            AssertEdited(graph, c => c.Direct = true);
        }

        [Test]
        public void LookupsFindWhatIsThereAndNothingElse()
        {
            MapGraph graph = CreateGraph();
            MapNode start = FirstNode(graph);

            Assert.IsTrue(graph.TryGetNode(start.Id, out MapNode found));
            Assert.AreSame(start, found);
            Assert.IsFalse(graph.TryGetNode(999, out MapNode missing));
            Assert.IsNull(missing);
            Assert.AreSame(start, graph.FindFirst(MapNodeType.Start));
            Assert.IsNull(graph.FindFirst(MapNodeType.Exit));
            Assert.AreSame(FirstLink(graph), graph.GetConnection(graph.Nodes[1].Id, start.Id));
            Assert.IsNull(graph.GetConnection(start.Id, 999));
        }

        private static void AssertEdited(MapGraph graph, System.Action<MapNode> edit)
        {
            MapGraph copy = graph.Clone();
            edit(FirstNode(copy));
            Assert.IsFalse(FirstNode(copy).SameAs(FirstNode(graph)));
        }

        private static void AssertEdited(MapGraph graph, System.Action<MapConnection> edit)
        {
            MapGraph copy = graph.Clone();
            edit(FirstLink(copy));
            Assert.IsFalse(FirstLink(copy).SameAs(FirstLink(graph)));
        }
    }
}
