using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // A placement's Floor: props kept up on a platform, down in a pit or on the doorways' level,
    // and on the ground floor in a room that has no such floor.
    public class PropFloorTests
    {
        // A 10 by 8 room entered from the east, its walkway along row 4.
        private static readonly Vector2Int Door = new(9, 4);

        private static RoomStructure Hall()
        {
            RoomStructure room = Room(10, 8, Door);
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            return room;
        }

        // Two tiles out from the west wall, four along it, climbed from (2, 1).
        private static RoomStructure HallWithPlatform()
        {
            RoomStructure room = Hall();
            var tiles = new List<Vector2Int>();
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 2; x++)
                    tiles.Add(new Vector2Int(x, y));
            var platform = new ElevatedArea(ElevationKind.Platform, 1, tiles, new[] { new StairFlight(new Vector2Int(2, 1), Vector2Int.left) }, false);
            Assert.That(room.TryAddElevation(platform), Is.True);
            return room;
        }

        [Test]
        public void RaisedPropsStandOnThePlatform()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = HallWithPlatform();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(2, 2);
                placement.Floor = PropFloor.Raised;
                planner.Add(placement);

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    Assert.That(room.LevelOf(TileOf(prop)), Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void GroundPropsKeepOffThePlatform()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = HallWithPlatform();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(8, 8);
                placement.Floor = PropFloor.Ground;
                planner.Add(placement);

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    Assert.That(room.LevelOf(TileOf(prop)), Is.Zero, $"seed {seed}");
            }
        }

        [Test]
        public void ARoomWithoutThatFloorPlacesThemOnTheGround()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Hall(), seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(2, 2);
                placement.Floor = PropFloor.Sunken;

                Assert.That(planner.Add(placement), Is.EqualTo(2), $"seed {seed}");
            }
        }

        [Test]
        public void ARelaxedPlacementDropsItsFloor()
        {
            var placement = new RoomPropPlacement { Floor = PropFloor.Raised };

            Assert.That(placement.Relaxed(1).Floor, Is.EqualTo(PropFloor.Any));
        }
    }
}
