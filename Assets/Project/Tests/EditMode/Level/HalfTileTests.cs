using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The open halves of a diagonal wall's tile as floor: walked through round the corner, seen
    // through, and holding one small prop each.
    public class HalfTileTests
    {
        // An L of tiles whose corner is the only way from one arm to the other, the way in at one end.
        private static RoomStructure L()
        {
            var tiles = new List<Vector2Int>();
            for (int i = 0; i < 6; i++)
            {
                tiles.Add(new Vector2Int(i, 0));
                if (i > 0) tiles.Add(new Vector2Int(5, i));
            }
            return new RoomStructurePlanner().Prepare(new RoomFootprint(tiles), 4f, new[] { new Vector2Int(0, 0) });
        }

        [Test]
        public void TheOpenHalfJoinsTheTwoTilesItOpensOnto()
        {
            Assert.That(L().TryAddDiagonals(new List<DiagonalWall> { new(new Vector2Int(5, 0), rising: true) }), Is.True,
                        "the half facing the L's inside joins its two arms");
            Assert.That(L().TryAddDiagonals(new List<DiagonalWall> { new(new Vector2Int(5, 0), rising: false) }), Is.False,
                        "turned the other way, the wall cuts the L in two");
        }

        [Test]
        public void ALineStayingInOneHalfSeesPastAndOneCrossingTheWallDoesNot()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(4, 0));
            var wall = new DiagonalWall(new Vector2Int(2, 6), rising: true);
            Assert.That(room.TryAddDiagonals(new List<DiagonalWall> { wall }), Is.True);
            var sight = new SightBlocking(room);

            // Below the tile and to its right: the "/" wall's lower-right half.
            Assert.That(sight.CanSee(new Vector2Int(1, 5), new Vector2Int(3, 6)), Is.True, "in from below the tile and out on its right: through the open half");
            Assert.That(sight.CanSee(new Vector2Int(2, 4), new Vector2Int(2, 8)), Is.False, "straight across the wall");

            Assert.That(room.TryAddHalfProp(wall.Tile, new Vector2Int(1, -1), 3f), Is.True);
            Assert.That(room.TryAddHalfProp(wall.Tile, new Vector2Int(1, -1), 1f), Is.False, "one prop to a half");
            Assert.That(room.TryAddHalfProp(wall.Tile, new Vector2Int(1, 1), 1f), Is.False, "a \"/\" wall has no such half");
            Assert.That(new SightBlocking(room).CanSee(new Vector2Int(1, 5), new Vector2Int(3, 6)), Is.False, "a tall prop in the half hides it");
        }

        [Test]
        public void AHalfTilePlacementFillsTheOpenHalvesFacingOutOfTheWall()
        {
            for (int seed = 1; seed <= Seeds; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(6, 0));
                List<DiagonalWall> run = DiagonalWall.Run(new Vector2Int(3, 5), new Vector2Int(1, 1), 3);
                Assert.That(room.TryAddDiagonals(run), Is.True);

                RoomPropPlanner planner = Planner(room, seed);
                RoomPropPlacement placement = Placement(4, 4);
                placement.HalfTile = true;
                Assert.That(planner.Add(placement), Is.EqualTo(4), $"seed {seed}: three tiles, six halves");

                var halves = new HashSet<(Vector2Int, Vector2Int)>();
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int tile = TileOf(prop);
                    var normal = new Vector2Int((int)Mathf.Sign(prop.Position.x - tile.x - 0.5f), (int)Mathf.Sign(prop.Position.y - tile.y - 0.5f));
                    Assert.That(room.TryGetDiagonal(tile, out DiagonalWall wall), Is.True);
                    Assert.That(Vector2.Distance(prop.Position, wall.HalfMiddle(normal)), Is.LessThan(1e-4f));
                    Assert.That(Mathf.DeltaAngle(prop.Yaw, Mathf.Atan2(normal.x, normal.y) * Mathf.Rad2Deg), Is.EqualTo(0f).Within(0.01f), "facing out of the wall");
                    Assert.That(halves.Add((tile, normal)), Is.True, "one to a half");
                }
            }

            RoomPropPlanner bare = Planner(Room(12, 10, new Vector2Int(6, 0)), 1);
            RoomPropPlacement none = Placement(2, 2);
            none.HalfTile = true;
            Assert.That(bare.Add(none), Is.EqualTo(0));
            Assert.That(bare.Outcomes, Is.Empty, "a room with no diagonal walls isn't short of anything");
        }
    }
}
