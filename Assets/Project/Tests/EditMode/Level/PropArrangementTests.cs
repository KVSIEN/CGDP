using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // How props are arranged so a room reads as ordered: square to the room, packed outward
    // from the middle on a lattice, one set per cell of a divided room.
    public class PropArrangementTests
    {
        [Test]
        public void PropsTurnedWithIntentStandSquareToTheRoom()
        {
            foreach (PropFacing facing in new[] { PropFacing.TowardCentre, PropFacing.Square })
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomPropPlanner planner = Planner(Room(10, 10), seed);
                    planner.Add(Placement(4, 6, facing));

                    foreach (PlannedProp prop in planner.Planned)
                        Assert.AreEqual(0f, Mathf.Repeat(prop.Yaw, 90f), 0.001f, $"{facing}, seed {seed}: a prop stands at {prop.Yaw}°");
                }
        }

        [Test]
        public void OnlyLooseClutterTakesAnyAngle()
        {
            var angles = new HashSet<float>();
            for (int seed = 0; seed < 10; seed++)
            {
                RoomPropPlanner planner = Planner(Room(10, 10), seed);
                planner.Add(Placement(4, 6, PropFacing.Random));
                foreach (PlannedProp prop in planner.Planned) angles.Add(Mathf.Repeat(prop.Yaw, 90f));
            }
            Assert.Greater(angles.Count, 4, "random clutter came out square");
        }

        [Test]
        public void TowardCentreFacesTheMiddle()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 12);
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(Placement(6, 8, PropFacing.TowardCentre));

                Vector2 middle = room.Footprint.Anchor;
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2 toMiddle = middle - prop.Tiles.center;
                    Assert.GreaterOrEqual(Vector2.Dot(toMiddle, Forward(prop)), 0f, $"seed {seed}: a prop turns its back on the middle");
                }
            }
        }

        // MinEmpty is the room's gap fill: a piece goes only into floor with nothing built
        // within that many tiles, so a busy corner is left alone and a bare one is filled.
        [Test]
        public void MinEmptyKeepsEveryPieceClearOfWhatWasPlacedBefore()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(16, 16), seed, fillShare: 0.6f);
                planner.Add(Placement(6, 6));
                var before = new List<PlannedProp>(planner.Planned);

                RoomPropPlacement fill = Placement(4, 6);
                fill.MinEmpty = 2;
                planner.Add(fill);

                for (int i = before.Count; i < planner.Planned.Count; i++)
                    foreach (PlannedProp other in before)
                    {
                        RectInt a = planner.Planned[i].Tiles, b = other.Tiles;
                        int dx = Mathf.Max(0, Mathf.Max(a.xMin - (b.xMax - 1), b.xMin - (a.xMax - 1)));
                        int dy = Mathf.Max(0, Mathf.Max(a.yMin - (b.yMax - 1), b.yMin - (a.yMax - 1)));
                        Assert.Greater(Mathf.Max(dx, dy), 2, $"seed {seed}: a gap-fill piece stands within 2 tiles of another prop");
                    }
            }
        }

        // A furnished neighbour behind a wall doesn't count: each bare cell still gets its piece.
        [Test]
        public void MinEmptyLooksOnlyInItsOwnZone()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = ThreeZoneRoom();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                planner.Add(Placement(1, 1), zones: new[] { room.ZoneOf(Vector2Int.zero) });

                RoomPropPlacement fill = Placement(1, 1);
                fill.MinEmpty = 8;
                fill.EachZone = true;

                Assert.AreEqual(2, planner.Add(fill), $"seed {seed}");
            }
        }

        // Centred on a lattice: a count fills the lattice points nearest the middle, so no
        // point farther out is taken while a nearer one is left empty.
        [Test]
        public void CentredFillsTheNearestLatticePointsFirst()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(14, 14, new Vector2Int(0, 7));
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                RoomPropPlacement cases = Placement(5, 5);
                cases.Lattice   = new Vector2Int(2, 2);
                cases.Selection = PropSelection.Centred;
                planner.Add(cases);

                Vector2 middle = room.Footprint.Anchor;
                float farthestTaken = 0f;
                foreach (PlannedProp prop in planner.Planned)
                    farthestTaken = Mathf.Max(farthestTaken, Vector2.Distance(prop.Tiles.center, middle));

                Assert.AreEqual(5, planner.Planned.Count, $"seed {seed}");
                foreach (Vector2Int tile in room.Footprint.Tiles)
                {
                    if (!OnLattice(tile, middle, 2) || !room.IsFree(tile) || room.Has(tile, cases.Avoid)) continue;
                    if (TooClose(tile, planner.Planned)) continue;
                    float distance = Vector2.Distance(tile + Vector2.one * 0.5f, middle);
                    Assert.GreaterOrEqual(distance, farthestTaken - 0.001f, $"seed {seed}: {tile} was left empty nearer the middle");
                }
            }
        }

        [Test]
        public void CentredHugsWhatItStandsNear()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(14, 14), seed, fillShare: 0.6f);
                RoomPropPlacement stage = Placement(1, 1);
                stage.Key       = "Stage";
                stage.Footprint = new Vector2Int(2, 2);
                RoomPropPlacement benches = Placement(3, 3, PropFacing.TowardNear);
                benches.Near         = "Stage";
                benches.NearDistance = 4;
                benches.Selection    = PropSelection.Centred;

                planner.Add(stage);
                RectInt stageTiles = planner.Planned[0].Tiles;
                planner.Add(benches);

                for (int i = 1; i < planner.Planned.Count; i++)
                    Assert.AreEqual(1, Chebyshev(planner.Planned[i].Tiles, stageTiles), $"seed {seed}: a bench stands back from the stage");
            }
        }

        // Three zones side by side; the middle one is the main space.
        [Test]
        public void EachZonePutsTheCountInEveryCell()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = ThreeZoneRoom();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                RoomPropPlacement bunks = Placement(1, 1, PropFacing.AgainstWall);
                bunks.Zone     = PropZone.Others;
                bunks.EachZone = true;

                Assert.AreEqual(2, planner.Add(bunks), $"seed {seed}");
                var zones = new HashSet<int>();
                foreach (PlannedProp prop in planner.Planned) zones.Add(room.ZoneOf(TileOf(prop)));
                Assert.AreEqual(2, zones.Count, $"seed {seed}: two bunks share a cell");
                Assert.IsFalse(zones.Contains(room.LargestZone), $"seed {seed}: a bunk in the main space");
                Assert.AreEqual(2, planner.Outcomes[0].Wanted, "the outcome adds up every cell's count");
            }
        }

        // A building's last rooms still get what every room gets once the first ones used up the room's share.
        [Test]
        public void EachZoneHoldsEveryCellToItsOwnShare()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = ThreeZoneRoom();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.2f);
                RoomPropPlacement crowd = Placement(60, 60, PropFacing.Square);
                crowd.Zone    = PropZone.Largest;
                crowd.Spacing = 0;
                planner.Add(crowd);
                Assert.IsTrue(planner.Outcomes[0].HitFillCap, $"seed {seed}: the main space didn't use up the room's share");

                RoomPropPlacement lockers = Placement(1, 1, PropFacing.AgainstWall);
                lockers.Zone     = PropZone.Others;
                lockers.EachZone = true;
                Assert.AreEqual(2, planner.Add(lockers), $"seed {seed}: a cell went without its locker");
            }
        }

        [Test]
        public void TowardNearWithNothingNamedFacesTheMiddle()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 12);
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(Placement(6, 8, PropFacing.TowardNear));

                Vector2 middle = room.Footprint.Anchor;
                foreach (PlannedProp prop in planner.Planned)
                    Assert.GreaterOrEqual(Vector2.Dot(middle - prop.Tiles.center, Forward(prop)), 0f, $"seed {seed}: a seat turns its back on the room");
            }
        }

        [Test]
        public void OfficeMazeOfficesAreZonesOfTheirOwn()
        {
            var rule = ScriptableObject.CreateInstance<OfficeMazeStructureRule>();
            try
            {
                int divided = 0;
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(16, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();
                    if (room.ZoneCount >= 3) divided++;
                }
                Assert.Greater(divided, Seeds / 2, "offices didn't come out as zones of their own");
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        private static RoomStructure ThreeZoneRoom()
        {
            RoomStructure room = Room(12, 4, new Vector2Int(5, 0));
            foreach (int x in new[] { 2, 8 })
            {
                var edges = new List<PartitionEdge>();
                // One gap so the room stays whole; zones count the line as closed.
                for (int y = 1; y < 4; y++) edges.Add(new PartitionEdge(new Vector2Int(x, y), new Vector2Int(x + 1, y), 3f));
                Assert.IsTrue(room.TryAddPartition(edges));
            }
            room.AssignZones();
            Assert.AreEqual(3, room.ZoneCount);
            return room;
        }

        private static bool OnLattice(Vector2Int tile, Vector2 middle, int pitch)
        {
            var origin = new Vector2Int(Mathf.FloorToInt(middle.x), Mathf.FloorToInt(middle.y));
            return Mod(tile.x - origin.x, pitch) == 0 && Mod(tile.y - origin.y, pitch) == 0;
        }

        private static int Mod(int value, int by) => (value % by + by) % by;

        // A lattice point a placed prop's spacing rules out.
        private static bool TooClose(Vector2Int tile, IReadOnlyList<PlannedProp> placed)
        {
            foreach (PlannedProp prop in placed)
                if (Chebyshev(new RectInt(tile, Vector2Int.one), prop.Tiles) <= 0) return true;
            return false;
        }

        private static int Chebyshev(RectInt a, RectInt b)
        {
            int dx = Mathf.Max(0, Mathf.Max(a.xMin - (b.xMax - 1), b.xMin - (a.xMax - 1)));
            int dy = Mathf.Max(0, Mathf.Max(a.yMin - (b.yMax - 1), b.yMin - (a.yMax - 1)));
            return Mathf.Max(dx, dy);
        }
    }
}
