using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Director;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // The director's view of a generated level's route (LevelRoute): rooms on the way to the
    // exit are ahead, rooms on the way back to the start behind, the nearest first, the safe
    // Start room never, and a point is picked on the open floor of one of them.
    public class LevelRouteTests
    {
        private const int RoomSize = 5, Gap = 2;

        // Start — A — B — C — D — Exit in a row along x, and a dead end X off B.
        private MapGraph _graph;
        private LevelLayout _layout;
        private int _a, _b, _c, _d, _exit, _x;

        [SetUp]
        public void Build()
        {
            _graph  = new MapGraph();
            _layout = new LevelLayout(1f);
            int start = Add(MapNodeType.Start, 0, 0);
            _a    = Add(MapNodeType.Combat, 1, 0);
            _b    = Add(MapNodeType.Junction, 2, 0);
            _c    = Add(MapNodeType.Combat, 3, 0);
            _d    = Add(MapNodeType.Combat, 4, 0);
            _exit = Add(MapNodeType.Exit, 5, 0);
            _x    = Add(MapNodeType.Treasure, 2, 1);
            _graph.Connect(start, _a);
            _graph.Connect(_a, _b);
            _graph.Connect(_b, _c);
            _graph.Connect(_c, _d);
            _graph.Connect(_d, _exit);
            _graph.Connect(_b, _x);
        }

        private int Add(MapNodeType type, int column, int row)
        {
            MapNode node = _graph.AddNode(type, new Vector2(column, row));
            var footprint = RoomFootprint.Rectangle(new RectInt(column * (RoomSize + Gap), row * (RoomSize + Gap), RoomSize, RoomSize));
            _layout.AddRoom(new LevelRoom(node, footprint, null, 4f));
            return node.Id;
        }

        private LevelRoute Route() => new(_layout, _graph, null);

        private static int[] Ids(System.Collections.Generic.IReadOnlyList<LevelRoom> rooms)
        {
            var ids = new int[rooms.Count];
            for (int i = 0; i < rooms.Count; i++) ids[i] = rooms[i].Node.Id;
            return ids;
        }

        [Test]
        public void AheadIsTowardTheExitNearestFirst()
        {
            Assert.That(Ids(Route().RoomsAlong(_b, RouteSide.Ahead, 2)), Is.EqualTo(new[] { _c, _d }));
            Assert.That(Ids(Route().RoomsAlong(_d, RouteSide.Ahead, 3)), Is.EqualTo(new[] { _exit }));
            Assert.That(Route().RoomsAlong(_exit, RouteSide.Ahead, 3), Is.Empty);
        }

        [Test]
        public void BehindIsBackTowardTheStartWithoutTheSafeRoom()
        {
            Assert.That(Ids(Route().RoomsAlong(_c, RouteSide.Behind, 2)), Is.EqualTo(new[] { _b, _a }));
            Assert.That(Route().RoomsAlong(_a, RouteSide.Behind, 2), Is.Empty);
        }

        [Test]
        public void RoomsKeptCalmAreWalkedThroughButNeverPicked()
        {
            var route = new LevelRoute(_layout, _graph, null, room => room.Node.Id == _c);
            Assert.That(Ids(route.RoomsAlong(_b, RouteSide.Ahead, 2)), Is.EqualTo(new[] { _d, _exit }));
            Assert.That(Ids(route.RoomsAlong(_d, RouteSide.Behind, 2)), Is.EqualTo(new[] { _b, _a }));
        }

        [Test]
        public void FromADeadEndAheadLeadsBackOntoTheRoute()
        {
            Assert.That(Ids(Route().RoomsAlong(_x, RouteSide.Ahead, 2)), Is.EqualTo(new[] { _b, _c }));
        }

        [Test]
        public void APointIsOnTheFloorOfARoomThatWay()
        {
            LevelRoute route = Route();
            var random = new RandomStream(new Seed(7));
            Vector3 inB = _layout.RoomCenterLocal(_layout.Rooms[_b]);
            for (int i = 0; i < 20; i++)
            {
                Assert.That(route.TryPickTile(inB, RouteSide.Ahead, 2, random, out Vector2Int tile), Is.True);
                int room = _layout.RoomAt(tile).Node.Id;
                Assert.That(room == _c || room == _d, Is.True, $"tile {tile} is in room {room}");
            }
        }

        [Test]
        public void NothingIsPickedFromACorridor()
        {
            var between = new Vector3(RoomSize + Gap / 2f, 0f, RoomSize / 2f);
            Assert.That(_layout.RoomAtLocal(between), Is.Null);
            Assert.That(Route().TryPickTile(between, RouteSide.Ahead, 2, new RandomStream(new Seed(1)), out _), Is.False);
        }
    }
}
