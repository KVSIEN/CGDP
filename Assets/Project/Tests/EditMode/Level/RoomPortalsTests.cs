using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // Which rooms the camera could see into (RoomPortals): its own room, and each room behind a
    // doorway in view, a few rooms deep; from a corridor, both rooms it joins.
    public class RoomPortalsTests
    {
        // Rooms A, B and C of 5 × 5 tiles in a row along x, two-tile corridors between them.
        private LevelLayout _layout;
        private int _a, _b, _c;

        [SetUp]
        public void Build()
        {
            var graph = new MapGraph();
            _layout = new LevelLayout(1f);
            _a = Add(graph, 0);
            _b = Add(graph, 7);
            _c = Add(graph, 14);
            Join(_a, _b, 4);
            Join(_b, _c, 11);
        }

        private int Add(MapGraph graph, int x)
        {
            MapNode node = graph.AddNode(MapNodeType.Combat, new Vector2(x, 0));
            _layout.AddRoom(new LevelRoom(node, RoomFootprint.Rectangle(new RectInt(x, 0, 5, 5)), null, 4f));
            return node.Id;
        }

        // A doorway out of `from`'s east wall at x = `wallX`, a corridor, and one into `to`'s west wall.
        private void Join(int from, int to, int wallX)
        {
            var connection = new MapConnection(from, to, ConnectionType.Normal);
            _layout.Doorways.Add(new LevelDoorway(_layout.Rooms[from], connection, new Vector2Int(wallX, 2), new Vector2Int(wallX + 1, 2), false));
            _layout.Doorways.Add(new LevelDoorway(_layout.Rooms[to], connection, new Vector2Int(wallX + 3, 2), new Vector2Int(wallX + 2, 2), false));
            _layout.AddCorridor(connection, new[] { new Vector2Int(wallX + 1, 2), new Vector2Int(wallX + 2, 2) });
        }

        private static readonly Vector3 InA = new(2.5f, 1f, 2.5f);

        private HashSet<int> Visible(Vector3 camera, System.Predicate<Bounds> inView, int depth = 4, int fallback = -1)
        {
            var visible = new HashSet<int>();
            Assert.That(new RoomPortals(_layout).TryVisible(camera, inView, depth, visible, fallback), Is.True);
            return visible;
        }

        [Test]
        public void WithEveryDoorwayInViewEveryRoomIsSeen() =>
            Assert.That(Visible(InA, _ => true), Is.EquivalentTo(new[] { _a, _b, _c }));

        [Test]
        public void WithNoDoorwayInViewOnlyTheCamerasRoomIs() =>
            Assert.That(Visible(InA, _ => false), Is.EquivalentTo(new[] { _a }));

        [Test]
        public void ARoomIsSeenOnlyThroughADoorwayInView()
        {
            // Only doorways west of x = 8 are in view: A's into B, not B's on to C.
            Assert.That(Visible(InA, box => box.center.x < 8f), Is.EquivalentTo(new[] { _a, _b }));
        }

        [Test]
        public void SightStopsAFewRoomsDeep() =>
            Assert.That(Visible(InA, _ => true, depth: 1), Is.EquivalentTo(new[] { _a, _b }));

        [Test]
        public void FromACorridorBothRoomsItJoinsAreSeen() =>
            Assert.That(Visible(new Vector3(5.5f, 1f, 2.5f), _ => false), Is.EquivalentTo(new[] { _a, _b }));

        [Test]
        public void OutsideEveryRoomItFallsBackToThePlayersRoomOrGivesUp()
        {
            var outside = new Vector3(30f, 1f, 30f);
            Assert.That(Visible(outside, _ => false, fallback: _c), Is.EquivalentTo(new[] { _c }));
            Assert.That(new RoomPortals(_layout).TryVisible(outside, _ => true, 4, new HashSet<int>()), Is.False);
        }
    }
}
