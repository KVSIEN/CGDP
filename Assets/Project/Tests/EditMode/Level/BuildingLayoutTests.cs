using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Buildings carved into a room: the plan tiles the whole lot, every room is reachable from
    // the hall through its doorway, the walls leave the room whole, and a town keeps its streets
    // open round buildings that are each their own zone.
    public class BuildingLayoutTests
    {
        private static BuildingPlan Plan(int hallTiles = 1, params BuildingSpaceSpec[] spaces)
        {
            var plan = ScriptableObject.CreateInstance<BuildingPlan>();
            Set(plan, "_hallTiles", hallTiles);
            Set(plan, "_spaces", spaces);
            return plan;
        }

        private static BuildingSpaceSpec Spec(string name, int min, int max, int widthMin = 3, int widthMax = 4,
                                              BuildingSpacePlacement placement = BuildingSpacePlacement.Anywhere, bool fills = false)
        {
            var spec = new BuildingSpaceSpec();
            Set(spec, "_name", name);
            Set(spec, "_count", new IntRange(min, max));
            Set(spec, "_width", new IntRange(widthMin, widthMax));
            Set(spec, "_placement", placement);
            Set(spec, "_fills", fills);
            return spec;
        }

        private static BuildingPlan School() => Plan(1,
            Spec("Principal", 1, 1, 3, 3, BuildingSpacePlacement.Entrance),
            Spec("Classroom", 2, 4, 3, 4, fills: true),
            Spec("Restroom", 1, 2, 2, 3),
            Spec("Gym", 1, 1, 3, 4, BuildingSpacePlacement.Back));

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = null;
            for (System.Type type = target.GetType(); field == null && type != null; type = type.BaseType)
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        private static RandomStream Stream(int seed) => new(new Seed((ulong)seed));

        [Test]
        public void ThePlanTilesTheWholeLotWithNoOverlap()
        {
            BuildingPlan plan = School();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                    foreach (Vector2Int front in BuildingLayoutPlanner.Fronts)
                    {
                        var lot = new RectInt(3, 5, 14, 11);
                        BuildingBlueprint blueprint = new BuildingLayoutPlanner(plan).Plan(lot, front, Stream(seed));
                        Assert.IsNotNull(blueprint, $"seed {seed}, front {front}");

                        var seen = new HashSet<Vector2Int>();
                        foreach (PlannedSpace space in blueprint.Spaces)
                            foreach (Vector2Int tile in space.Rect.allPositionsWithin)
                            {
                                Assert.IsTrue(lot.Contains(tile), $"seed {seed}: {space.Name} left the lot at {tile}");
                                Assert.IsTrue(seen.Add(tile), $"seed {seed}: {tile} is in two spaces");
                            }
                        Assert.AreEqual(lot.width * lot.height, seen.Count, $"seed {seed}, front {front}: floor left unassigned");
                    }
            }
            finally { Object.DestroyImmediate(plan); }
        }

        [Test]
        public void EveryRoomIsReachableFromTheFrontDoorThroughDoorways()
        {
            BuildingPlan plan = School();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    var lot = new RectInt(0, 0, 16, 12);
                    BuildingBlueprint blueprint = new BuildingLayoutPlanner(plan).Plan(lot, Vector2Int.up, Stream(seed));

                    var owner = new Dictionary<Vector2Int, int>();
                    for (int i = 0; i < blueprint.Spaces.Count; i++)
                        foreach (Vector2Int tile in blueprint.Spaces[i].Rect.allPositionsWithin) owner[tile] = i;

                    var reached = new HashSet<int> { owner[blueprint.FrontDoor.A] };
                    Assert.IsTrue(blueprint.Spaces[owner[blueprint.FrontDoor.A]].IsHall, $"seed {seed}: the front door doesn't open onto the hall");
                    for (bool grew = true; grew;)
                    {
                        grew = false;
                        foreach (BuildingDoor door in blueprint.Doors)
                        {
                            int a = owner[door.A], b = owner[door.B];
                            Assert.AreEqual(1, Mathf.Abs(door.A.x - door.B.x) + Mathf.Abs(door.A.y - door.B.y), $"seed {seed}: a doorway between tiles that don't touch");
                            if (reached.Contains(a) != reached.Contains(b)) { reached.Add(a); reached.Add(b); grew = true; }
                        }
                    }
                    Assert.AreEqual(blueprint.Spaces.Count, reached.Count, $"seed {seed}: a room with no way in");
                }
            }
            finally { Object.DestroyImmediate(plan); }
        }

        [Test]
        public void RequiredRoomsAreBuiltWhenTheyFit()
        {
            BuildingPlan plan = School();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    BuildingBlueprint blueprint = new BuildingLayoutPlanner(plan).Plan(new RectInt(0, 0, 20, 14), Vector2Int.right, Stream(seed));
                    Assert.IsEmpty(blueprint.Missing, $"seed {seed}: {string.Join(", ", blueprint.Missing)} didn't fit");
                    foreach (string name in new[] { "Principal", "Classroom", "Restroom", "Gym", "Hall" })
                        Assert.IsTrue(blueprint.Spaces.Exists(s => s.Name == name), $"seed {seed}: no {name}");
                }
            }
            finally { Object.DestroyImmediate(plan); }
        }

        [Test]
        public void ARoomTakesItsPinnedLayoutAndTheOthersTakeNone()
        {
            var layout = ScriptableObject.CreateInstance<RoomLayout>();
            BuildingSpaceSpec classroom = Spec("Classroom", 2, 4, 3, 4, fills: true);
            Set(classroom, "_layout", layout);
            BuildingPlan plan = Plan(1, Spec("Principal", 1, 1, 3, 3, BuildingSpacePlacement.Entrance), classroom);
            try
            {
                BuildingBlueprint blueprint = new BuildingLayoutPlanner(plan).Plan(new RectInt(0, 0, 14, 10), Vector2Int.up, Stream(3));
                foreach (PlannedSpace space in blueprint.Spaces)
                    Assert.AreSame(space.Name == "Classroom" ? layout : null, space.Layout, space.Name);
            }
            finally
            {
                Object.DestroyImmediate(plan);
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void ZoneRulesInASingleZoneSpaceKeepTheMainSpaceAndDropTheRest()
        {
            RoomPropPlacement any = Placement(1, 1);
            RoomPropPlacement main = Placement(1, 1);
            main.Zone = PropZone.Largest;
            RoomPropPlacement others = Placement(1, 1);
            others.Zone = PropZone.Others;

            Assert.AreSame(any, any.ForSingleZone());
            Assert.AreEqual(PropZone.Any, main.ForSingleZone().Zone);
            Assert.AreEqual(PropZone.Largest, main.Zone, "the original is untouched");
            Assert.IsNull(others.ForSingleZone());
        }

        [Test]
        public void ARoomWithALayoutIsFurnishedByIt()
        {
            var layout = ScriptableObject.CreateInstance<RoomLayout>();
            Set(layout, "_props", new List<RoomPropPlacement> { Placement(2, 2) });
            var function = ScriptableObject.CreateInstance<RoomFunction>();
            try
            {
                RoomStructure room = Room(6, 6, new Vector2Int(0, 3));
                room.AssignZones();
                room.AddSpace(new Vector2Int(2, 2), function, "Surgery", layout);

                RoomPropPlanner planner = Planner(room, 1);
                foreach (RoomPropPlacement placement in function.PropsWith(room.Spaces[0].Layout))
                    planner.Add(placement.ForSingleZone(), zones: new[] { room.ZoneOf(room.Spaces[0].Tile) });
                Assert.AreEqual(2, planner.Planned.Count, "the layout's set pieces furnish the room");
            }
            finally
            {
                Object.DestroyImmediate(function);
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void ALotTooSmallForThePlanGetsNoBuilding()
        {
            BuildingPlan plan = School();
            try
            {
                Assert.IsNull(new BuildingLayoutPlanner(plan).Plan(new RectInt(0, 0, 2, 8), Vector2Int.up, Stream(1)), "too narrow for a hall and a room");
                Assert.IsNull(new BuildingLayoutPlanner(plan).Plan(new RectInt(0, 0, 8, 3), Vector2Int.up, Stream(1)), "too shallow");
            }
            finally { Object.DestroyImmediate(plan); }
        }

        [Test]
        public void ABuildingRoomIsWalledIntoOneZonePerSpaceAndStaysWhole()
        {
            BuildingPlan plan = School();
            var rule = ScriptableObject.CreateInstance<BuildingStructureRule>();
            Set(rule, "_plan", plan);
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(16, 12, new Vector2Int(0, 6), new Vector2Int(15, 3));
                    rule.Apply(room, Stream(seed));
                    room.AssignZones();

                    Assert.Greater(room.Spaces.Count, 3, $"seed {seed}: no building");
                    Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}: the walls cut the room");
                    Assert.AreEqual(room.Spaces.Count, room.ZoneCount, $"seed {seed}: spaces and zones differ");

                    var zones = new HashSet<int>();
                    foreach (StructureSpace space in room.Spaces)
                        Assert.IsTrue(zones.Add(room.ZoneOf(space.Tile)), $"seed {seed}: two spaces share a zone");
                    foreach (Vector2Int entrance in room.Entrances)
                        Assert.IsFalse(room.Has(entrance, RoomTileTags.Structure), $"seed {seed}: a doorway is built over");
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
                Object.DestroyImmediate(plan);
            }
        }

        [Test]
        public void ATownKeepsOpenStreetsAndWalledBuildings()
        {
            BuildingPlan plan = School();
            var lot = new TownLot();
            Set(lot, "_plan", plan);
            Set(lot, "_weight", 1f);
            var rule = ScriptableObject.CreateInstance<TownStructureRule>();
            Set(rule, "_lots", new[] { lot });
            Set(rule, "_emptyLotChance", 0f);
            try
            {
                int withBuildings = 0;
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure town = Room(30, 30, new Vector2Int(0, 15), new Vector2Int(29, 15));
                    rule.Apply(town, Stream(seed));
                    town.AssignZones();

                    Assert.IsTrue(town.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}: a building or the street is cut off from the doorways");
                    if (town.ZoneCount < 4) continue;
                    withBuildings++;

                    int streetZone = town.ZoneOf(town.Entrances[0]);
                    Assert.AreEqual(streetZone, town.ZoneOf(town.Entrances[1]), $"seed {seed}: the two ways in lead to different streets");
                }
                Assert.Greater(withBuildings, Seeds / 2, "most towns should have several buildings");
            }
            finally
            {
                Object.DestroyImmediate(rule);
                Object.DestroyImmediate(plan);
            }
        }

        [Test]
        public void AStreetFunctionFurnishesOnlyTheStreet()
        {
            BuildingPlan plan = School();
            var lot = new TownLot();
            Set(lot, "_plan", plan);
            Set(lot, "_weight", 1f);
            var street = ScriptableObject.CreateInstance<RoomFunction>();
            Set(street, "_props", new List<RoomPropPlacement> { Placement(3, 3) });
            var rule = ScriptableObject.CreateInstance<TownStructureRule>();
            Set(rule, "_lots", new[] { lot });
            Set(rule, "_emptyLotChance", 0f);
            Set(rule, "_streetFunction", street);
            try
            {
                RoomStructure town = Room(30, 30, new Vector2Int(0, 15));
                rule.Apply(town, Stream(5));
                town.AssignZones();
                Assert.AreEqual("Street", town.Spaces[town.Spaces.Count - 1].Name, "the street is the last space");

                StructureSpace streetSpace = town.Spaces[town.Spaces.Count - 1];
                var planner = Planner(town, 5);
                foreach (StructureSpace space in town.Spaces)
                    if (space.Function != null) planner.Add(space.Function.PropsWith(null), zones: new[] { town.ZoneOf(space.Tile) });

                Assert.AreEqual(3, planner.Planned.Count);
                foreach (PlannedProp prop in planner.Planned)
                    Assert.AreEqual(town.ZoneOf(streetSpace.Tile), town.ZoneOf(TileOf(prop)), "a street prop stands inside a building");
            }
            finally
            {
                Object.DestroyImmediate(rule);
                Object.DestroyImmediate(street);
                Object.DestroyImmediate(plan);
            }
        }
    }
}
