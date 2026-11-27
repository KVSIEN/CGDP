using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
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

        private int Add(RectInt area, params DoorSocket[] sockets)
        {
            MapNode node = _graph.AddNode(MapNodeType.Combat, area.position);
            RoomFootprint footprint = sockets.Length > 0 ? RoomFootprint.Rectangle(area, sockets) : RoomFootprint.Rectangle(area);
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
        public void ADistantRoomIsPulledInToTouch()
        {
            int a = Add(0), b = Add(3);
            MapConnection link = Direct(a, b);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.IsTrue(links.ContainsKey(link), string.Join("\n", _layout.Warnings));
            AssertJoins(links[link], a, b);
        }

        // A bystander beside each room stops the other from coming flush: they close to the
        // nearest distance that clears it, joined by one straight run.
        [Test]
        public void RoomsThatCannotTouchAreJoinedByTheShortestStraightRun()
        {
            int a = Add(new RectInt(0, 0, 6, 6), new DoorSocket(new Vector2Int(5, 1), Vector2Int.right));
            int b = Add(new RectInt(30, 0, 6, 12), new DoorSocket(new Vector2Int(30, 1), Vector2Int.left));
            Add(new RectInt(7, 8, 4, 4));
            Add(new RectInt(25, 8, 4, 4));
            MapConnection link = Direct(a, b);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.IsTrue(links.ContainsKey(link), string.Join("\n", _layout.Warnings));
            IReadOnlyList<Vector2Int> tiles = links[link].Tiles;
            Assert.AreEqual(8, tiles.Count);
            for (int i = 1; i < tiles.Count; i++) Assert.AreEqual(tiles[i - 1] + Vector2Int.right, tiles[i]);
            Assert.AreEqual(1, Steps(links[link].OutsideA, links[link].InsideA));
            Assert.AreEqual(1, Steps(links[link].OutsideB, links[link].InsideB));
        }

        [Test]
        public void RoomsWhoseDoorwaysCannotFaceKeepTheirHallway()
        {
            int a = Add(new RectInt(0, 0, 6, 6), new DoorSocket(new Vector2Int(5, 1), Vector2Int.right));
            int b = Add(new RectInt(16, 0, 6, 6), new DoorSocket(new Vector2Int(21, 1), Vector2Int.right));
            Direct(a, b);

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.AreEqual(0, links.Count);
            Assert.AreEqual(1, _layout.Warnings.Count);
        }

        // The generator scene's room and its neighbours all end up sharing a doorway.
        [Test]
        public void AMadeUpRoomsNeighboursAreAllPulledAgainstIt()
        {
            var recipe = new RoomRecipe { DoorWest = true };
            _graph = RoomRecipeGraph.Build(recipe, new Seed(7).Stream());
            foreach (MapNode node in _graph.Nodes)
            {
                var corner = new Vector2Int(Mathf.RoundToInt(node.Position.x) * Pitch, -Mathf.RoundToInt(node.Position.y) * Pitch);
                _layout.AddRoom(new LevelRoom(node, RoomFootprint.Rectangle(new RectInt(corner, Vector2Int.one * RoomSize)), null, 4f));
            }

            Dictionary<MapConnection, DirectLink> links = new DirectRoomAligner(MaxShift).Align(_graph, _layout);

            Assert.AreEqual(4, links.Count, string.Join("\n", _layout.Warnings));
            foreach (MapConnection connection in _graph.Connections) AssertJoins(links[connection], connection.A, connection.B);
        }

        // The doorway tile is outside both rooms and each inside tile is a floor tile beside it.
        private void AssertJoins(DirectLink link, int a, int b)
        {
            LevelRoom roomA = _layout.Rooms[a], roomB = _layout.Rooms[b];
            Assert.AreEqual(1, link.Tiles.Count, "the rooms don't touch");
            Vector2Int tile = link.OutsideA;
            Assert.IsNull(_layout.RoomAt(tile), "the doorway tile is inside a room");
            Assert.IsTrue(roomA.Footprint.Contains(link.InsideA), "A's doorway isn't on A's floor");
            Assert.IsTrue(roomB.Footprint.Contains(link.InsideB), "B's doorway isn't on B's floor");
            Assert.AreEqual(1, Steps(tile, link.InsideA));
            Assert.AreEqual(1, Steps(tile, link.InsideB));
        }

        private static int Steps(Vector2Int from, Vector2Int to) => Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }
}
