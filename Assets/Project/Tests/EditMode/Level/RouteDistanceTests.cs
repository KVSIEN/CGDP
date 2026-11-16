using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Distances from a room's routes, and the placement options built on them: Near Route keeps
    // concealment where people pass, Off Route sends it to dead ends, and a route-marking prop
    // (a service door) adds a route of its own.
    public class RouteDistanceTests
    {
        private static readonly Vector2Int Door = new(0, 4);

        [Test]
        public void WalkwaysAreDistanceZeroAndDistanceGrowsByOneAStep()
        {
            RoomStructure room = Room(9, 9, Door);
            var routes = new RouteMap(room);

            Assert.That(routes.HasRoutes, Is.True);
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                int expected = NearestWalkway(room, tile);
                Assert.That(routes.DistanceOf(tile), Is.EqualTo(expected), $"tile {tile}");
            }
        }

        [Test]
        public void DistanceGoesAroundInnerWalls()
        {
            RoomStructure room = Room(9, 9, Door);
            // A wall above (0,6) and (1,6): (0,7) is three steps up from the doorway's walkway tile
            // as the crow flies, but has to be walked to around the wall's end.
            room.TryAddPartition(new[]
            {
                new PartitionEdge(new Vector2Int(0, 6), new Vector2Int(0, 7), 3f, dividesZones: false),
                new PartitionEdge(new Vector2Int(1, 6), new Vector2Int(1, 7), 3f, dividesZones: false),
            });
            var routes = new RouteMap(room);

            Assert.That(routes.DistanceOf(new Vector2Int(0, 7)), Is.GreaterThan(NearestWalkway(room, new Vector2Int(0, 7))));
        }

        [Test]
        public void NearRoutePropsStandBesideAWalkway()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 10, Door);
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(6, 6);
                placement.NearRoute = 1;
                planner.Add(placement);

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    Assert.That(NearestWalkway(room, TileOf(prop)), Is.LessThanOrEqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void OffRoutePropsStandAwayFromEveryWalkway()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(10, 10, Door);
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(4, 4);
                placement.OffRoute = 3;
                planner.Add(placement);

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    Assert.That(NearestWalkway(room, TileOf(prop)), Is.GreaterThanOrEqualTo(3), $"seed {seed}");
            }
        }

        [Test]
        public void ARelaxedPlacementDropsItsRouteRules()
        {
            var placement = new RoomPropPlacement { NearRoute = 2, OffRoute = 3 };
            RoomPropPlacement relaxed = placement.Relaxed(1);

            Assert.That(relaxed.NearRoute, Is.Zero);
            Assert.That(relaxed.OffRoute, Is.Zero);
        }

        [Test]
        public void AServiceDoorMarksARouteForThePlacementsAfterIt()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                // No doorway, so the service door is the room's only route.
                RoomStructure room = Room(10, 10);
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);

                RoomPropPlacement door = Placement(1, 1, PropFacing.AgainstWall);
                door.On         = RoomTileTags.Edge;
                door.Clearance  = 1;
                door.Required   = true;
                door.MarksRoute = true;
                planner.Add(door);
                Assert.That(planner.Planned.Count, Is.EqualTo(1), $"seed {seed}");
                Vector2Int doorFront = TileOf(planner.Planned[0]) + Forward(planner.Planned[0]);

                RoomPropPlacement cover = Placement(3, 3);
                cover.NearRoute = 2;
                planner.Add(cover);

                Assert.That(planner.Planned.Count, Is.GreaterThan(1), $"seed {seed}");
                for (int i = 1; i < planner.Planned.Count; i++)
                    Assert.That(Steps(TileOf(planner.Planned[i]), doorFront), Is.LessThanOrEqualTo(2), $"seed {seed}");
            }
        }

        [Test]
        public void ARoomWithoutRoutesPutsNoLimitOnRouteDistances()
        {
            RoomStructure room = Room(8, 8);
            RoomPropPlanner planner = Planner(room, 1);
            RoomPropPlacement placement = Placement(3, 3);
            placement.OffRoute = 3;
            planner.Add(placement);

            Assert.That(new RouteMap(room).HasRoutes, Is.False);
            Assert.That(planner.Planned.Count, Is.EqualTo(3));
        }

        // Every concealment placement the game ships says where it stands relative to the routes.
        [Test]
        public void EveryShippedConcealmentPlacementFollowsTheRoutes()
        {
            int checkedPlacements = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:RoomDressing", new[] { "Assets/Project/Data/Level" }))
            {
                var dressing = AssetDatabase.LoadAssetAtPath<RoomDressing>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (RoomPropPlacement placement in dressing.Props)
                {
                    if (!ConcealsAny(placement)) continue;
                    checkedPlacements++;
                    Assert.That(placement.NearRoute > 0 || placement.OffRoute > 0, $"{dressing.name}: concealment with no route rule");
                }
            }
            Assert.That(checkedPlacements, Is.GreaterThan(0));
        }

        private static bool ConcealsAny(RoomPropPlacement placement)
        {
            foreach (GameObject prefab in placement.Prefabs)
                if (RoomTraitMeasure.Conceals(prefab)) return true;
            return false;
        }

        // Steps over open floor to the nearest walkway tile (no inner walls in these rooms).
        private static int NearestWalkway(RoomStructure room, Vector2Int tile)
        {
            int nearest = int.MaxValue;
            foreach (Vector2Int other in room.Footprint.Tiles)
                if (room.Has(other, RoomTileTags.Walkway)) nearest = Mathf.Min(nearest, Steps(tile, other));
            return nearest;
        }

        private static int Steps(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
