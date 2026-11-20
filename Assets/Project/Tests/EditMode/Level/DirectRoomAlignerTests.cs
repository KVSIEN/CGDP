using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    public class DirectRoomAlignerTests
    {
        private const int RoomSize = 6, Pitch = 16, MaxShift = 18;

        private MapGraph _graph;
        private LevelLayout _layout;

        [SetUp]
        public void Build()
        {
            _graph  = new MapGraph();
            _layout = new LevelLayout(1f);
        }

        private int Add(int column)
        {
            MapNode node = _graph.AddNode(MapNodeType.Combat, new Vector2(column, 0));
            var footprint = RoomFootprint.Rectangle(new RectInt(column * Pitch, 0, RoomSize, RoomSize));
            _layout.AddRoom(new LevelRoom(node, footprint, null, 4f));
            return node.Id;
        }

        private MapConnection Direct(int a, int b)
        {
            _graph.Connect(a, b);
            MapConnection connection = _graph.Connections[_graph.Connections.Count - 1];
            connection.Direct = true;
            return connection;
        }

        [Test]
        public void TwoRoomsMoveTogetherAndShareOneDoorwayTile()
        {
            int a = Add(0), b = Add(1);
            MapConnection link = Direct(a, b);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.IsTrue(links.ContainsKey(link), string.Join("\n", _layout.Warnings));
            AssertJoins(links[link], a, b);
        }

        // A-B and C-D are fitted first; B-C then needs both pairs, which only works if a fitted
        // pair can still move as one.
        [Test]
        public void FittedRoomsMoveAsAGroupToMeetAnotherGroup()
        {
            int a = Add(0), b = Add(1), c = Add(2), d = Add(3);
            MapConnection ab = Direct(a, b), cd = Direct(c, d), bc = Direct(b, c);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.AreEqual(3, links.Count, string.Join("\n", _layout.Warnings));
            AssertJoins(links[ab], a, b);
            AssertJoins(links[cd], c, d);
            AssertJoins(links[bc], b, c);
        }

        [Test]
        public void ARoomTooFarAwayKeepsItsHallway()
        {
            int a = Add(0), b = Add(3);
            Direct(a, b);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.AreEqual(0, links.Count);
            Assert.AreEqual(1, _layout.Warnings.Count);
        }

        // The doorway tile is outside both rooms and each inside tile is a floor tile beside it.
        private void AssertJoins(DirectLink link, int a, int b)
        {
            LevelRoom roomA = _layout.Rooms[a], roomB = _layout.Rooms[b];
            Assert.IsNull(_layout.RoomAt(link.Tile), "the doorway tile is inside a room");
            Assert.IsTrue(roomA.Footprint.Contains(link.InsideA), "A's doorway isn't on A's floor");
            Assert.IsTrue(roomB.Footprint.Contains(link.InsideB), "B's doorway isn't on B's floor");
            Assert.AreEqual(1, Steps(link.Tile, link.InsideA));
            Assert.AreEqual(1, Steps(link.Tile, link.InsideB));
        }

        private static int Steps(Vector2Int from, Vector2Int to) => Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }
}
