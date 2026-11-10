using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The room dressing rules: whatever is placed, the room stays walkable from every doorway,
    // nothing overlaps, and each option (walls, footprints, clearance, spread, cover, piles)
    // does what it says.
    public class RoomDressingTests
    {
        // How exposed the room is to long shots: the squared length of every unbroken stretch of
        // floor along each row and column, summed. Cover that cuts long lanes lowers it most.
        private static int SightlineExposure(RoomStructure structure)
        {
            int exposure = 0;
            RectInt bounds = structure.Footprint.Bounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++) exposure += Exposure(structure, new Vector2Int(bounds.xMin, y), Vector2Int.right, bounds.width);
            for (int x = bounds.xMin; x < bounds.xMax; x++) exposure += Exposure(structure, new Vector2Int(x, bounds.yMin), Vector2Int.up, bounds.height);
            return exposure;
        }

        private static int Exposure(RoomStructure structure, Vector2Int from, Vector2Int step, int length)
        {
            int total = 0, run = 0;
            for (int i = 0; i <= length; i++)
            {
                Vector2Int tile = from + step * i;
                bool open = i < length && structure.Footprint.Contains(tile) && !structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop);
                if (open)
                {
                    run++;
                    continue;
                }
                total += run * run;
                run = 0;
            }
            return total;
        }

        // --- Structure ------------------------------------------------------------------

        [Test]
        public void APillarThatWouldSealACornerIsRefused()
        {
            RoomStructure room = Room(6, 6, new Vector2Int(5, 3));

            Assert.IsTrue(room.TryAddPillar(new Vector2(0.5f, 1.5f), 0.8f, null));
            // Tile (0, 0) would now only be reachable through (1, 0).
            Assert.IsFalse(room.TryAddPillar(new Vector2(1.5f, 0.5f), 0.8f, null));
            Assert.IsFalse(room.Has(new Vector2Int(1, 0), RoomTileTags.Structure));
        }

        [Test]
        public void AWallThatWouldSealAPocketBesideAPillarIsRefused()
        {
            RoomStructure room = Room(8, 8, new Vector2Int(7, 4));
            Assert.IsTrue(room.TryAddPillar(new Vector2(2.5f, 1.5f), 0.8f, null));
            Assert.IsTrue(room.TryAddPillar(new Vector2(1.5f, 2.5f), 0.8f, null));

            // Tile (1, 1) is open to the pillars on two sides; walling the other two would leave it enclosed.
            var walls = new[]
            {
                new PartitionEdge(new Vector2Int(0, 1), new Vector2Int(1, 1), 0f),
                new PartitionEdge(new Vector2Int(1, 0), new Vector2Int(1, 1), 0f),
            };
            Assert.IsFalse(room.TryAddPartition(walls));
            Assert.AreEqual(0, room.Partitions.Count);
            Assert.IsFalse(room.IsPartitioned(new Vector2Int(0, 1), new Vector2Int(1, 1)));
        }

        [Test]
        public void ADoorwayCanNeverBeClaimed()
        {
            var entrance = new Vector2Int(0, 3);
            RoomStructure room = Room(6, 6, entrance);

            Assert.IsFalse(room.TryClaim(new[] { entrance }, RoomTileTags.Prop));
        }

        [Test]
        public void PartitionSkipsAWallThatWouldCutOffASliver()
        {
            var rule = ScriptableObject.CreateInstance<PartitionStructureRule>();
            try
            {
                AssertPartitionSkipsSlivers(rule);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        private static void AssertPartitionSkipsSlivers(PartitionStructureRule rule)
        {
            RoomStructure narrow = Room(3, 5, new Vector2Int(1, 0));
            rule.Apply(narrow, new RandomStream(new Seed(1)));
            Assert.AreEqual(0, narrow.Partitions.Count);

            RoomStructure wide = Room(10, 10, new Vector2Int(5, 0));
            rule.Apply(wide, new RandomStream(new Seed(1)));
            Assert.Greater(wide.Partitions.Count, 0);
        }

        // --- Safety ---------------------------------------------------------------------

        [Test]
        public void TheRoomStaysWalkableWhateverIsPlaced()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(9, 7, new Vector2Int(0, 3), new Vector2Int(8, 3));
                RoomPropPlanner planner = Planner(room, seed);

                // Few enough to fit under the fill cap, so every placement gets its turn.
                var wallCrates = Placement(8, 8, PropFacing.AgainstWall);
                wallCrates.On = RoomTileTags.Edge;
                var cover = Placement(4, 4, PropFacing.AcrossSightline, spacing: 1);
                cover.Selection = PropSelection.Cover;
                var piles = Placement(4, 4);
                piles.Pile = new IntRange(2, 4);
                var container = Placement(2, 2);
                container.Footprint = new Vector2Int(2, 1);

                int tiles = 0;
                tiles += planner.Add(wallCrates);
                tiles += planner.Add(cover);
                tiles += planner.Add(piles);
                tiles += planner.Add(container) * 2;

                Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}");
                foreach (Vector2Int entrance in room.Entrances)
                    Assert.IsFalse(room.Has(entrance, RoomTileTags.Prop | RoomTileTags.Structure), $"seed {seed}");
                Assert.AreEqual(tiles, TilesWith(room, RoomTileTags.Prop), $"seed {seed}: a prop took more or less floor than it should");
            }
        }

        [Test]
        public void PropsNeverGoOnWalkwaysOrNearDoors()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
                Planner(room, seed).Add(Placement(40, 40));

                foreach (Vector2Int tile in room.Footprint.Tiles)
                    if (room.Has(tile, RoomTileTags.Prop))
                        Assert.IsFalse(room.Has(tile, RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre), $"seed {seed} tile {tile}");
            }
        }

        [Test]
        public void ClutterStopsAtTheFillCap()
        {
            RoomStructure room = Room(8, 8, new Vector2Int(0, 4));
            RoomPropPlanner planner = Planner(room, 3);

            planner.Add(Placement(200, 200));

            Assert.LessOrEqual(TilesWith(room, RoomTileTags.Prop), Mathf.FloorToInt(64 * 0.35f));
            Assert.Greater(TilesWith(room, RoomTileTags.Prop), 0);
        }

        [Test]
        public void ARequiredPlacementLoosensItsRulesUntilItFits()
        {
            var impossible = Placement(1, 1, PropFacing.AgainstWall);
            impossible.On = RoomTileTags.Corner;
            impossible.Clearance = 9;

            RoomStructure strict = Room(4, 4, new Vector2Int(0, 2));
            Assert.AreEqual(0, Planner(strict, 1).Add(impossible));

            impossible.Required = true;
            RoomStructure required = Room(4, 4, new Vector2Int(0, 2));
            Assert.AreEqual(1, Planner(required, 1).Add(impossible));
        }

        // --- Walls and footprints -------------------------------------------------------

        [Test]
        public void WallPropsHaveTheirBackToAWall()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var placement = Placement(12, 12, PropFacing.AgainstWall);
                placement.On = RoomTileTags.Edge;
                planner.Add(placement);

                Assert.Greater(planner.Planned.Count, 0);
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int tile = TileOf(prop);
                    Assert.IsTrue(room.IsWall(tile, -Forward(prop)), $"seed {seed}: prop on {tile} faces {Forward(prop)} but has no wall behind it");
                }
            }
        }

        [Test]
        public void WideWallPropsLieAlongTheWall()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 9, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var placement = Placement(3, 3, PropFacing.AgainstWall);
                placement.Footprint = new Vector2Int(3, 1);
                placement.On = RoomTileTags.Edge;
                planner.Add(placement);

                foreach (PlannedProp prop in planner.Planned)
                {
                    // One tile deep, so it was moved toward the wall by a quarter tile; undo that for its middle.
                    Vector2Int forward = Forward(prop);
                    Vector2 middle = prop.Position + (Vector2)forward * 0.25f;
                    Vector2 along = new(Mathf.Abs(forward.y), Mathf.Abs(forward.x));
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 spot = middle + along * i;
                        var tile = new Vector2Int(Mathf.FloorToInt(spot.x), Mathf.FloorToInt(spot.y));
                        Assert.IsTrue(room.Footprint.Contains(tile), $"seed {seed}: {tile} is off the floor");
                        Assert.IsTrue(room.IsWall(tile, -forward), $"seed {seed}: {tile} has no wall behind it");
                    }
                }
            }
        }

        // --- Clearance ------------------------------------------------------------------

        [Test]
        public void ThingsToUseKeepFloorInFrontOfThem()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(RoomPropPlacement.Nook(new GameObject[1]));
                Assert.AreEqual(1, planner.Planned.Count, $"seed {seed}");

                var clutter = Placement(60, 60);
                planner.Add(clutter);

                PlannedProp nook = planner.Planned[0];
                Vector2Int front = TileOf(nook) + Forward(nook);
                Assert.IsTrue(room.Footprint.Contains(front), $"seed {seed}");
                Assert.IsFalse(room.Has(front, RoomTileTags.Prop | RoomTileTags.Structure), $"seed {seed}: something stands in front of the nook");
                Assert.IsTrue(room.Has(front, RoomTileTags.Access), $"seed {seed}");
            }
        }

        [Test]
        public void PropsWithClearanceNeverFaceAWall()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var placement = Placement(10, 10);
                placement.On = RoomTileTags.Edge;
                placement.Clearance = 1;
                planner.Add(placement);

                Assert.Greater(planner.Planned.Count, 0);
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int front = TileOf(prop) + Forward(prop);
                    Assert.IsTrue(room.Footprint.Contains(front), $"seed {seed}: the prop on {TileOf(prop)} faces a wall");
                    Assert.IsFalse(room.Has(front, RoomTileTags.Prop), $"seed {seed}: the prop on {TileOf(prop)} faces another prop");
                }
            }
        }

        // --- Spacing --------------------------------------------------------------------

        private static float NearestPair(IReadOnlyList<PlannedProp> props)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < props.Count; i++)
                for (int j = i + 1; j < props.Count; j++)
                    nearest = Mathf.Min(nearest, (props[i].Position - props[j].Position).magnitude);
            return nearest;
        }

        [Test]
        public void SpreadKeepsPropsFartherApartThanRandom()
        {
            float random = 0f, spread = 0f;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var placement = Placement(6, 6);
                RoomStructure a = Room(12, 12, new Vector2Int(0, 6));
                RoomPropPlanner byChance = Planner(a, seed);
                byChance.Add(placement);
                random += NearestPair(byChance.Planned);

                placement.Selection = PropSelection.Spread;
                RoomStructure b = Room(12, 12, new Vector2Int(0, 6));
                RoomPropPlanner evenly = Planner(b, seed);
                evenly.Add(placement);
                spread += NearestPair(evenly.Planned);
            }
            Assert.Greater(spread / Seeds, random / Seeds * 1.2f);
        }

        [Test]
        public void SpacingKeepsFreeTilesBetweenProps()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(Placement(10, 10, spacing: 2));

                foreach (PlannedProp a in planner.Planned)
                    foreach (PlannedProp b in planner.Planned)
                    {
                        if (a.Position == b.Position) continue;
                        Vector2Int d = TileOf(a) - TileOf(b);
                        Assert.Greater(Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), 2, $"seed {seed}");
                    }
            }
        }

        // --- Cover ----------------------------------------------------------------------

        [Test]
        public void CoverCutsMoreSightlinesThanRandomPlacement()
        {
            float random = 0f, cover = 0f;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var placement = Placement(4, 4, PropFacing.AcrossSightline, spacing: 1);
                placement.Avoid |= RoomTileTags.Edge;

                RoomStructure a = Room(12, 12, new Vector2Int(0, 6));
                int before = SightlineExposure(a);
                Planner(a, seed).Add(placement);
                random += SightlineExposure(a);

                placement.Selection = PropSelection.Cover;
                RoomStructure b = Room(12, 12, new Vector2Int(0, 6));
                Assert.Greater(Planner(b, seed).Add(placement), 0, $"seed {seed}");
                Assert.Less(SightlineExposure(b), before, $"seed {seed}");
                cover += SightlineExposure(b);
            }
            Assert.Less(cover, random);
        }

        [Test]
        public void BarriersFaceTheLaneTheyBlock()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                // Twelve tiles long, four wide: the lanes run along x.
                RoomStructure room = Room(12, 4, new Vector2Int(0, 2));
                RoomPropPlanner planner = Planner(room, seed);
                var cover = Placement(1, 1, PropFacing.AcrossSightline, spacing: 1);
                cover.Selection = PropSelection.Cover;
                cover.Avoid |= RoomTileTags.Edge;
                if (planner.Add(cover) == 0) continue;

                Vector2Int forward = Forward(planner.Planned[0]);
                Assert.AreEqual(0, forward.y, $"seed {seed}: the barrier faces {forward}, not along the room");
            }
        }

        // --- Piles ----------------------------------------------------------------------

        [Test]
        public void PilesStayInsideTheirTile()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(8, 8, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var pile = Placement(1, 1);
                pile.On = RoomTileTags.Corner;
                pile.Pile = new IntRange(2, 4);

                Assert.AreEqual(1, planner.Add(pile), $"seed {seed}");
                Assert.That(planner.Planned.Count, Is.InRange(2, 4), $"seed {seed}");
                Vector2Int tile = TileOf(planner.Planned[0]);
                foreach (PlannedProp prop in planner.Planned)
                    Assert.AreEqual(tile, TileOf(prop), $"seed {seed}");
                Assert.AreEqual(1, TilesWith(room, RoomTileTags.Prop));
            }
        }

        [Test]
        public void ScalingByRoomSizeFurnishesBigRoomsMore()
        {
            var placement = Placement(4, 4);
            placement.ScaleByRoomSize = true;
            placement.Spacing = 0;

            int small = Planner(Room(5, 5, new Vector2Int(0, 2)), 1).Add(placement);
            int big = Planner(Room(14, 14, new Vector2Int(0, 7)), 1).Add(placement);

            Assert.Less(small, 4);
            Assert.Greater(big, 4);
        }

        [Test]
        public void TheSameSeedFurnishesTheSameRoom()
        {
            var placement = Placement(8, 8);
            placement.Selection = PropSelection.Spread;
            placement.Pile = new IntRange(1, 3);

            RoomPropPlanner first = Planner(Room(10, 10, new Vector2Int(0, 5)), 7);
            first.Add(placement);
            RoomPropPlanner second = Planner(Room(10, 10, new Vector2Int(0, 5)), 7);
            second.Add(placement);

            Assert.AreEqual(first.Planned.Count, second.Planned.Count);
            for (int i = 0; i < first.Planned.Count; i++)
            {
                Assert.AreEqual(first.Planned[i].Position, second.Planned[i].Position);
                Assert.AreEqual(first.Planned[i].Yaw, second.Planned[i].Yaw);
            }
        }
    }
}
