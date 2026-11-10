using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // What gives a faction's rooms their look: ordered (lattices, mirrored pairs, rows down the
    // room), organic (clusters, ground cover that takes no floor), crowded (more props, more of the
    // floor) and ruined or office-like structure that still leaves every room walkable.
    public class RoomStyleTests
    {
        private static T Rule<T>() where T : RoomStructureRule => ScriptableObject.CreateInstance<T>();

        // Judged from the tile tags themselves, not from the rule's own idea of an open edge: a wall may
        // not stand between two walkway tiles, or touch a tile in front of a doorway.
        private static void AssertWallsLeaveRoutesOpen(RoomStructure room, int seed)
        {
            foreach (PartitionEdge wall in room.Partitions)
            {
                bool acrossRoute = room.Has(wall.Tile, RoomTileTags.Walkway) && room.Has(wall.Other, RoomTileTags.Walkway);
                bool nextToDoor = room.Has(wall.Tile, RoomTileTags.NearDoor) || room.Has(wall.Other, RoomTileTags.NearDoor);
                Assert.IsFalse(acrossRoute, $"seed {seed}: a wall across the route at {wall.Tile}");
                Assert.IsFalse(nextToDoor, $"seed {seed}: a wall beside the doorway at {wall.Tile}");
            }
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        // --- Placement: order ---------------------------------------------------------

        [Test]
        public void LatticePlacementsLineUpOnTheGrid()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(14, 10, new Vector2Int(0, 5));
                RoomPropPlanner planner = Planner(room, seed);
                var placement = Placement(40, 40);
                placement.Lattice = new Vector2Int(3, 2);
                planner.Add(placement);

                Vector2Int origin = new(Mathf.FloorToInt(room.Footprint.Anchor.x), Mathf.FloorToInt(room.Footprint.Anchor.y));
                Assert.Greater(planner.Planned.Count, 3, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int tile = TileOf(prop) - origin;
                    Assert.AreEqual(0, ((tile.x % 3) + 3) % 3, $"seed {seed}: {TileOf(prop)} is off the lattice");
                    Assert.AreEqual(0, ((tile.y % 2) + 2) % 2, $"seed {seed}: {TileOf(prop)} is off the lattice");
                }
            }
        }

        [Test]
        public void RowsRunDownTheLongAxisFacingTheMiddle()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(14, 8, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var line = Placement(6, 6, PropFacing.AlongRoom);
                line.Footprint = new Vector2Int(1, 2);
                line.Lattice = new Vector2Int(2, 2);
                Assert.Greater(planner.Add(line), 0, $"seed {seed}");

                foreach (PlannedProp prop in planner.Planned)
                {
                    Vector2Int forward = Forward(prop);
                    Assert.AreEqual(0, forward.y, $"seed {seed}: a machine on a wide room should lie along x");
                    Assert.AreEqual(prop.Position.x <= room.Footprint.Anchor.x ? 1 : -1, forward.x, $"seed {seed}: it should face the middle");
                }
            }
        }

        [Test]
        public void SymmetricPlacementsComeInMirroredPairs()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                // An odd number of rows, with the doorway on the middle one, so the room is symmetric.
                RoomStructure room = Room(12, 9, new Vector2Int(0, 4));
                RoomPropPlanner planner = Planner(room, seed);
                var racks = Placement(10, 10, PropFacing.AgainstWall);
                racks.On = RoomTileTags.Edge;
                racks.Symmetric = true;
                planner.Add(racks);

                int unpaired = 0;
                foreach (PlannedProp prop in planner.Planned)
                {
                    bool hasTwin = false;
                    foreach (PlannedProp other in planner.Planned)
                        hasTwin |= Mathf.Approximately(other.Position.x, prop.Position.x)
                                && Mathf.Approximately(other.Position.y, 9f - prop.Position.y)
                                && Forward(other).y == -Forward(prop).y;
                    if (!hasTwin) unpaired++;
                }
                Assert.Greater(planner.Planned.Count, 2, $"seed {seed}");
                Assert.LessOrEqual(unpaired, 1, $"seed {seed}: {unpaired} props have no mirror image");
            }
        }

        // Machines standing end to end along the room (two tiles between centres, in the same row).
        private static int MachinesNoseToTail(IReadOnlyList<PlannedProp> props)
        {
            int pairs = 0;
            for (int i = 0; i < props.Count; i++)
                for (int j = i + 1; j < props.Count; j++)
                {
                    Vector2 apart = props[j].Position - props[i].Position;
                    if (Mathf.Approximately(Mathf.Abs(apart.x), 2f) && Mathf.Approximately(apart.y, 0f)) pairs++;
                }
            return pairs;
        }

        [Test]
        public void LinesRunNoseToTailMoreThanScatteredMachines()
        {
            int scattered = 0, lined = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var machines = Placement(6, 6, PropFacing.AlongRoom);
                machines.Footprint = new Vector2Int(1, 2);
                machines.Lattice = new Vector2Int(2, 2);

                RoomPropPlanner byChance = Planner(Room(16, 10, new Vector2Int(0, 5)), seed);
                byChance.Add(machines);
                scattered += MachinesNoseToTail(byChance.Planned);

                machines.Selection = PropSelection.Line;
                RoomPropPlanner inRows = Planner(Room(16, 10, new Vector2Int(0, 5)), seed);
                inRows.Add(machines);
                lined += MachinesNoseToTail(inRows.Planned);
            }
            Assert.Greater(lined, scattered * 1.4f);
        }

        // --- Placement: organic -------------------------------------------------------

        private static float AverageNearestNeighbour(IReadOnlyList<PlannedProp> props)
        {
            float total = 0f;
            foreach (PlannedProp prop in props)
            {
                float nearest = float.MaxValue;
                foreach (PlannedProp other in props)
                    if (!ReferenceEquals(other.Prefab, prop.Prefab) || other.Position != prop.Position)
                        nearest = Mathf.Min(nearest, (other.Position - prop.Position).magnitude);
                total += nearest;
            }
            return total / props.Count;
        }

        [Test]
        public void ClustersGatherPropsCloserThanRandomPlacement()
        {
            float random = 0f, cluster = 0f;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var placement = Placement(10, 10);
                RoomPropPlanner byChance = Planner(Room(14, 14, new Vector2Int(0, 7)), seed);
                byChance.Add(placement);
                random += AverageNearestNeighbour(byChance.Planned);

                placement.Selection = PropSelection.Cluster;
                RoomPropPlanner gathered = Planner(Room(14, 14, new Vector2Int(0, 7)), seed);
                gathered.Add(placement);
                cluster += AverageNearestNeighbour(gathered.Planned);
            }
            Assert.Less(cluster, random * 0.85f);
        }

        [Test]
        public void DecorationsTakeNoFloorAndMayLieOnWalkways()
        {
            int onWalkway = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
                room.TryAddPillar(new Vector2(6.5f, 2.5f), 0.8f, null);
                RoomPropPlanner planner = Planner(room, seed);
                var moss = Placement(60, 60);
                moss.Decor = true;
                moss.Avoid = RoomTileTags.Structure | RoomTileTags.Prop;
                planner.Add(moss);

                Assert.AreEqual(0, TilesWith(room, RoomTileTags.Prop), $"seed {seed}: ground cover took floor");
                foreach (PlannedProp prop in planner.Planned)
                {
                    Assert.AreNotEqual(new Vector2Int(6, 2), TileOf(prop), $"seed {seed}: ground cover on a pillar");
                    if (room.Has(TileOf(prop), RoomTileTags.Walkway)) onWalkway++;
                }

                // It doesn't use up the room's share of floor either.
                RoomStructure other = Room(10, 8, new Vector2Int(0, 4));
                RoomPropPlanner crates = Planner(other, seed);
                crates.Add(moss);
                crates.Add(Placement(200, 200));
                RoomStructure plain = Room(10, 8, new Vector2Int(0, 4));
                Planner(plain, seed).Add(Placement(200, 200));
                Assert.AreEqual(TilesWith(plain, RoomTileTags.Prop), TilesWith(other, RoomTileTags.Prop), $"seed {seed}");
            }
            Assert.Greater(onWalkway, 0);
        }

        // --- Placement: crowding ------------------------------------------------------

        [Test]
        public void ARoomsStyleSetsHowMuchFloorPropsMayTake()
        {
            var crates = Placement(200, 200);

            RoomStructure open = Room(8, 8, new Vector2Int(0, 4));
            Planner(open, 1, fillShare: 0.1f).Add(crates);
            RoomStructure crowded = Room(8, 8, new Vector2Int(0, 4));
            Planner(crowded, 1, fillShare: 0.5f).Add(crates);

            Assert.LessOrEqual(TilesWith(open, RoomTileTags.Prop), 6);
            Assert.Greater(TilesWith(crowded, RoomTileTags.Prop), 20);
            Assert.LessOrEqual(TilesWith(crowded, RoomTileTags.Prop), 32);
        }

        [Test]
        public void ACountScaleThinsOrThickensAPlacementButNeverARequiredOne()
        {
            var props = Placement(4, 4);
            Assert.AreEqual(2, Planner(Room(10, 10, new Vector2Int(0, 5)), 1, countScale: 0.5f).Add(props));
            Assert.AreEqual(8, Planner(Room(10, 10, new Vector2Int(0, 5)), 1, countScale: 2f).Add(props));

            props.Required = true;
            Assert.AreEqual(4, Planner(Room(10, 10, new Vector2Int(0, 5)), 1, countScale: 2f).Add(props));
        }

        // --- Structure ----------------------------------------------------------------

        [Test]
        public void OfficeMazeKeepsTheRoomWalkableAndFramesItsDoorways()
        {
            var rule = Rule<OfficeMazeStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();

                    Assert.Greater(room.Partitions.Count, 12, $"seed {seed}: no office walls");
                    Assert.Greater(room.Openings.Count, 3, $"seed {seed}: no doorways");
                    Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}: part of the maze is cut off");
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}: office walls split the room into zones");
                    foreach (PartitionEdge opening in room.Openings)
                        Assert.IsFalse(room.IsPartitioned(opening.Tile, opening.Other), $"seed {seed}: a doorway has a wall in it");
                }

                RoomStructure closet = Room(4, 4, new Vector2Int(0, 2));
                rule.Apply(closet, new RandomStream(new Seed(1)));
                Assert.AreEqual(0, closet.Partitions.Count);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void OfficeMazeWallsLeaveRoutesAndDoorsOpen()
        {
            var rule = Rule<OfficeMazeStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));

                    AssertWallsLeaveRoutesOpen(room, seed);
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void WallRemnantsStandAloneOffTheRoutes()
        {
            var rule = Rule<WallRemnantsStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();

                    Assert.Greater(room.Partitions.Count, 0, $"seed {seed}");
                    Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}");
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}: a stub of wall made zones");
                    AssertWallsLeaveRoutesOpen(room, seed);
                    foreach (PartitionEdge wall in room.Partitions) Assert.IsFalse(wall.DividesZones);
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void ColumnGridsCanCrumble()
        {
            var rule = Rule<GridPillarsStructureRule>();
            try
            {
                RoomStructure whole = Room(12, 12, new Vector2Int(0, 6));
                rule.Apply(whole, new RandomStream(new Seed(1)));
                int standing = whole.Pillars.Count;
                Assert.Greater(standing, 2);
                foreach (StructurePillar pillar in whole.Pillars) Assert.AreEqual(0f, pillar.Height);

                SetField(rule, "_brokenChance", 1f);
                RoomStructure broken = Room(12, 12, new Vector2Int(0, 6));
                rule.Apply(broken, new RandomStream(new Seed(1)));
                Assert.AreEqual(standing, broken.Pillars.Count);
                foreach (StructurePillar pillar in broken.Pillars)
                    Assert.That(pillar.Height, Is.InRange(0.01f, 3.99f), "a broken column should stand shorter than the room");

                SetField(rule, "_missingChance", 1f);
                RoomStructure gone = Room(12, 12, new Vector2Int(0, 6));
                rule.Apply(gone, new RandomStream(new Seed(1)));
                Assert.AreEqual(0, gone.Pillars.Count);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }
    }
}
