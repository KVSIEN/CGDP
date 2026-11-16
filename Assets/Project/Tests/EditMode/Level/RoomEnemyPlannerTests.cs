using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Factions;
using CGD.Level;
using CGD.Map;
using CGD.Stealth;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Where a room's enemies start: what each tile offers (RoomPostMap), spawns spaced and
    // repeatable by seed, a faction's favoured posts winning and held, each Breach faction on
    // its own side, and patrol loops on floor one can stand on.
    public class RoomEnemyPlannerTests
    {
        private const int Margin = 1;
        private static readonly Vector2Int WestDoor = new(0, 4);
        private static readonly Vector2Int EastDoor = new(9, 4);

        private readonly List<Object> _made = new();

        [TearDown]
        public void Clean()
        {
            foreach (Object made in _made) Object.DestroyImmediate(made);
            _made.Clear();
        }

        private static LevelRoom RoomOf(RoomStructure structure, FactionDefinition breach = null)
        {
            MapNode node = new MapGraph().AddNode(breach != null ? MapNodeType.Breach : MapNodeType.Combat, Vector2.zero);
            var room = new LevelRoom(node, structure.Footprint, null, 4f, breachFaction: breach);
            room.AttachStructure(structure);
            return room;
        }

        private PlannedProp Patch(RectInt tiles)
        {
            var prefab = new GameObject("Patch", typeof(BoxCollider), typeof(ConcealmentZone));
            _made.Add(prefab);
            return new PlannedProp(prefab, tiles.center, 0f, tiles, decor: true);
        }

        private static RoomEnemyPlan Plan(LevelRoom room, IReadOnlyList<PlannedProp> props, int count, int seed,
                                          PostPreference holder = null, PostPreference breach = null) =>
            RoomEnemyPlanner.Plan(room, props, count, new RandomStream(new Seed((ulong)seed)), null, holder, breach ?? holder, Margin);

        [Test]
        public void ConcealmentPropsMakeConcealedPosts()
        {
            RoomStructure room = Room(10, 10, WestDoor);
            var posts = new RoomPostMap(room, new[] { Patch(new RectInt(6, 6, 2, 2)) });

            Assert.IsTrue((posts.KindsAt(new Vector2Int(6, 7)) & RoomPostKinds.Concealed) != 0);
            Assert.IsTrue((posts.KindsAt(new Vector2Int(2, 2)) & RoomPostKinds.Concealed) == 0);
        }

        [Test]
        public void NarrowFloorIsAChoke()
        {
            var posts = new RoomPostMap(Room(10, 2), System.Array.Empty<PlannedProp>());
            Assert.IsTrue((posts.KindsAt(new Vector2Int(5, 1)) & RoomPostKinds.Choke) != 0);
        }

        [Test]
        public void ALongLaneWithCoverBesideItIsOverwatch()
        {
            RoomStructure room = Room(12, 8);
            Assert.IsTrue(room.TryClaim(new[] { new Vector2Int(6, 4) }, RoomTileTags.Structure));
            var posts = new RoomPostMap(room, System.Array.Empty<PlannedProp>());

            Assert.IsTrue((posts.KindsAt(new Vector2Int(6, 3)) & RoomPostKinds.Overwatch) != 0, "beside the pillar");
            Assert.IsTrue((posts.KindsAt(new Vector2Int(1, 1)) & RoomPostKinds.Overwatch) == 0, "the room's own walls are no cover");
        }

        [Test]
        public void SpawnsKeepApartAndRepeatBySeed()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelRoom room = RoomOf(Room(12, 12, WestDoor));
                RoomEnemyPlan plan = Plan(room, System.Array.Empty<PlannedProp>(), 4, seed);
                RoomEnemyPlan again = Plan(RoomOf(Room(12, 12, WestDoor)), System.Array.Empty<PlannedProp>(), 4, seed);

                Assert.AreEqual(4, plan.Spawns.Count, $"seed {seed}");
                for (int i = 0; i < plan.Spawns.Count; i++)
                {
                    Assert.AreEqual(again.Spawns[i].Tile, plan.Spawns[i].Tile, $"seed {seed}: not the same room twice");
                    for (int j = i + 1; j < plan.Spawns.Count; j++)
                    {
                        Vector2Int a = plan.Spawns[i].Tile, b = plan.Spawns[j].Tile;
                        Assert.That(Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)), Is.GreaterThanOrEqualTo(RoomEnemyPlanner.MinSpacing), $"seed {seed}");
                    }
                }
            }
        }

        [Test]
        public void AFactionStartsOnItsFavouredPostAndHoldsIt()
        {
            var ambusher = new PostPreference(1000000f, 1f, 1f, 1f, 1f, RoomPostKinds.Concealed);
            for (int seed = 0; seed < Seeds; seed++)
            {
                LevelRoom room = RoomOf(Room(10, 10, WestDoor));
                RoomEnemyPlan plan = Plan(room, new[] { Patch(new RectInt(6, 6, 2, 2)) }, 1, seed, ambusher);

                PlannedSpawn spawn = plan.Spawns[0];
                Assert.IsTrue(new RectInt(6, 6, 2, 2).Contains(spawn.Tile), $"seed {seed}: started on {spawn.Tile}");
                Assert.IsTrue(spawn.Holds, $"seed {seed}");
            }
        }

        [Test]
        public void WithoutAPreferenceEveryonePatrols()
        {
            RoomEnemyPlan plan = Plan(RoomOf(Room(10, 10, WestDoor)), System.Array.Empty<PlannedProp>(), 3, 1);
            foreach (PlannedSpawn spawn in plan.Spawns) Assert.IsFalse(spawn.Holds);
            Assert.That(plan.PatrolOf(false).Count, Is.GreaterThan(1));
        }

        [Test]
        public void EachBreachFactionStartsAndPatrolsOnItsOwnSide()
        {
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            _made.Add(faction);
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure structure = SplitRoom();
                LevelRoom room = RoomOf(structure, faction);
                room.AttachBreachEntrances(new[] { WestDoor });
                int west = structure.ZoneOf(WestDoor);

                RoomEnemyPlan plan = Plan(room, System.Array.Empty<PlannedProp>(), 6, seed);
                Assert.IsNotNull(plan.Split);
                foreach (PlannedSpawn spawn in plan.Spawns)
                    Assert.AreEqual(spawn.Breach, structure.ZoneOf(spawn.Tile) == west, $"seed {seed}: {spawn.Tile} on the wrong side");
                foreach (Vector2Int tile in plan.PatrolOf(true)) Assert.AreEqual(west, structure.ZoneOf(tile), $"seed {seed}");
                foreach (Vector2Int tile in plan.PatrolOf(false)) Assert.AreNotEqual(west, structure.ZoneOf(tile), $"seed {seed}");
            }
        }

        [Test]
        public void PatrolLoopsStandOnFloor()
        {
            RoomStructure structure = Room(10, 10, WestDoor);
            // Pillars in the room's outer corners, where the old loop's corners would have been.
            foreach (Vector2Int pillar in new[] { new Vector2Int(8, 8), new Vector2Int(8, 1), new Vector2Int(1, 1), new Vector2Int(1, 8) })
                Assert.IsTrue(structure.TryClaim(new[] { pillar }, RoomTileTags.Structure));

            RoomEnemyPlan plan = Plan(RoomOf(structure), System.Array.Empty<PlannedProp>(), 0, 1);
            foreach (Vector2Int tile in plan.PatrolOf(false))
                Assert.IsFalse(structure.Has(tile, RoomTileTags.Structure | RoomTileTags.Prop), $"{tile} is not floor");
        }

        // 10 × 10, cut by a wall between columns 3 and 4 with an opening at the walkway.
        private static RoomStructure SplitRoom()
        {
            RoomStructure room = Room(10, 10, WestDoor, EastDoor);
            var edges = new List<PartitionEdge>();
            for (int y = 0; y < 10; y++)
                if (!room.EdgeMustStayOpen(new Vector2Int(3, y), new Vector2Int(4, y)))
                    edges.Add(new PartitionEdge(new Vector2Int(3, y), new Vector2Int(4, y), 3f));
            Assert.IsTrue(room.TryAddPartition(edges));
            room.AssignZones();
            return room;
        }
    }
}
