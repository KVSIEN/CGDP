using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // A furnished room's loose props can be moved one at a time and put back, without ever
    // costing the room its floor, its doorways or its prop count.
    public class RoomPropMoveTests
    {
        private static readonly Vector2Int Door = new(11, 6);

        private static RandomStream Stream(ulong seed = 3) => new(new Seed(seed));

        private static HashSet<Vector2Int> PropTiles(RoomStructure room)
        {
            var tiles = new HashSet<Vector2Int>();
            foreach (Vector2Int tile in room.Footprint.Tiles)
                if (room.Has(tile, RoomTileTags.Prop)) tiles.Add(tile);
            return tiles;
        }

        private static RoomPropPlanner Furnished(RoomStructure room, RoomPropPlacement placement, bool refinable = true)
        {
            RoomPropPlanner planner = Planner(room, 1);
            planner.Add(placement, refinable: refinable);
            return planner;
        }

        [Test]
        public void OnlyWhatWasAskedAndAllowedCanMove()
        {
            Assert.AreEqual(0, Furnished(Room(12, 12, Door), Placement(5, 5), refinable: false).MovableCount, "not asked");

            RoomPropPlacement required = Placement(1, 1);
            required.Required = true;
            Assert.AreEqual(0, Furnished(Room(12, 12, Door), required).MovableCount, "required");

            RoomPropPlacement symmetric = Placement(2, 2);
            symmetric.Symmetric = true;
            Assert.AreEqual(0, Furnished(Room(12, 12, Door), symmetric).MovableCount, "has a twin");

            RoomPropPlacement keyed = Placement(2, 2);
            keyed.Key = "pool";
            Assert.AreEqual(0, Furnished(Room(12, 12, Door), keyed).MovableCount, "others stand near it");

            RoomPropPlacement cluster = Placement(3, 3);
            cluster.Selection = PropSelection.Cluster;
            Assert.AreEqual(0, Furnished(Room(12, 12, Door), cluster).MovableCount, "its selection gives it a shape");

            Assert.AreEqual(5, Furnished(Room(12, 12, Door), Placement(5, 5)).MovableCount);
        }

        [Test]
        public void MovingKeepsTheCountTheFloorAndTheRoomWhole()
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 12, Door);
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(Placement(8, 8, spacing: 1), refinable: true);
                int props = planner.Planned.Count;
                int floor = PropTiles(room).Count;
                RandomStream random = Stream((ulong)seed);

                for (int round = 0; round < 3; round++)
                    for (int piece = 0; piece < planner.MovableCount; piece++)
                        planner.TryMove(piece, random, out _);

                Assert.AreEqual(props, planner.Planned.Count, $"seed {seed}");
                Assert.AreEqual(floor, PropTiles(room).Count, $"seed {seed}: floor taken");
                Assert.IsTrue(room.StaysConnected(Array.Empty<Vector2Int>()), $"seed {seed}: the room stays whole");

                var tiles = new HashSet<Vector2Int>();
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int tile = TileOf(prop);
                    Assert.IsTrue(room.Has(tile, RoomTileTags.Prop), $"seed {seed}: a prop stands on floor that is marked taken");
                    Assert.IsTrue(tiles.Add(tile), $"seed {seed}: two props on one tile");
                }
            }
        }

        [Test]
        public void MovedPropsKeepTheirSpacing()
        {
            RoomStructure room = Room(12, 12, Door);
            RoomPropPlanner planner = Planner(room, 4);
            planner.Add(Placement(6, 6, spacing: 2), refinable: true);
            RandomStream random = Stream();

            for (int round = 0; round < 4; round++)
                for (int piece = 0; piece < planner.MovableCount; piece++)
                    planner.TryMove(piece, random, out _);

            for (int a = 0; a < planner.Planned.Count; a++)
                for (int b = a + 1; b < planner.Planned.Count; b++)
                {
                    Vector2Int apart = TileOf(planner.Planned[a]) - TileOf(planner.Planned[b]);
                    Assert.Greater(Mathf.Max(Mathf.Abs(apart.x), Mathf.Abs(apart.y)), 2, "two tiles of floor stay between them");
                }
        }

        [Test]
        public void UndoPutsEverythingBack()
        {
            RoomStructure room = Room(12, 12, Door);
            RoomPropPlanner planner = Planner(room, 2);
            planner.Add(Placement(6, 6), refinable: true);
            var before = new List<PlannedProp>(planner.Planned);
            HashSet<Vector2Int> floor = PropTiles(room);
            RandomStream random = Stream();

            var undos = new List<Action>();
            for (int piece = 0; piece < planner.MovableCount; piece++)
                if (planner.TryMove(piece, random, out Action undo)) undos.Add(undo);
            Assert.IsNotEmpty(undos, "something moved");

            for (int i = undos.Count - 1; i >= 0; i--) undos[i]();

            Assert.AreEqual(before.Count, planner.Planned.Count);
            for (int i = 0; i < before.Count; i++)
            {
                Assert.AreEqual(before[i].Position, planner.Planned[i].Position, $"prop {i}");
                Assert.AreEqual(before[i].Yaw, planner.Planned[i].Yaw, $"prop {i}");
                Assert.AreEqual(before[i].Tiles, planner.Planned[i].Tiles, $"prop {i}");
            }
            Assert.IsTrue(floor.SetEquals(PropTiles(room)));
        }

        [Test]
        public void AFullRoomHasNowhereToMoveTo()
        {
            RoomStructure room = Room(4, 4, new Vector2Int(3, 1));
            RoomPropPlanner planner = Planner(room, 1, fillShare: 0.8f);
            planner.Add(Placement(40, 40), refinable: true);
            HashSet<Vector2Int> floor = PropTiles(room);
            RandomStream random = Stream();

            for (int piece = 0; piece < planner.MovableCount; piece++)
            {
                bool moved = planner.TryMove(piece, random, out Action undo);
                if (moved) undo();
            }

            Assert.IsTrue(floor.SetEquals(PropTiles(room)), "a failed or undone move leaves the floor as it was");
        }

        [Test]
        public void DecorationsMoveWithoutTakingFloor()
        {
            RoomStructure room = Room(12, 12, Door);
            RoomPropPlacement patch = Placement(4, 4);
            patch.Decor = true;
            RoomPropPlanner planner = Furnished(room, patch);
            var before = new List<PlannedProp>(planner.Planned);
            RandomStream random = Stream();

            int moved = 0;
            for (int piece = 0; piece < planner.MovableCount; piece++)
                if (planner.TryMove(piece, random, out _)) moved++;

            Assert.Greater(moved, 0);
            Assert.AreEqual(0, PropTiles(room).Count, "decorations take no floor, moved or not");
            Assert.AreEqual(before.Count, planner.Planned.Count);
        }

        [Test]
        public void RecordingMovablePropsDoesNotChangeThePlan()
        {
            // The furnishing is planned the same whether or not its props are kept to be moved.
            RoomStructure plain = Room(12, 12, Door);
            RoomPropPlanner unrefined = Planner(plain, 9);
            unrefined.Add(Placement(6, 6), refinable: false);

            RoomStructure refinable = Room(12, 12, Door);
            RoomPropPlanner refined = Planner(refinable, 9);
            refined.Add(Placement(6, 6), refinable: true);

            Assert.AreEqual(unrefined.Planned.Count, refined.Planned.Count);
            for (int i = 0; i < unrefined.Planned.Count; i++)
                Assert.AreEqual(unrefined.Planned[i].Position, refined.Planned[i].Position, $"prop {i}");
        }
    }
}
