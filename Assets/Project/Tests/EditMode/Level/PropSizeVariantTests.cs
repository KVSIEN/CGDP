using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Editor;
using CGD.Level;
using CGD.Loot;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Props that come in several sizes (PropSizeVariants): a room that places one may get any of them.
    public class PropSizeVariantTests
    {
        [TestCase("Crate", 2, 1)]
        [TestCase("CardboardBox", 2, 1)]
        [TestCase("ParkTree", 2, 1)]
        [TestCase("Bush", 2, 1)]
        [TestCase("Rock", 2, 1)]
        [TestCase("Barrel", 2, 1)]
        [TestCase("Shelf", 2, 1)]
        [TestCase("PalletStack", 2, 1)]
        [TestCase("Locker", 2, 1)]
        [TestCase("StorageCabinet", 2, 1)]
        [TestCase("FileCabinet", 2, 1)]
        [TestCase("Table", 2, 1)]
        [TestCase("OfficeDesk", 2, 1)]
        [TestCase("Bench", 2, 1)]
        [TestCase("Sofa", 2, 1)]
        [TestCase("ServerRack", 2, 1)]
        [TestCase("FuelTank", 2, 1)]
        [TestCase("Generator", 2, 2)]
        [TestCase("CargoContainer", 3, 2)]
        public void EveryShippedSizeTurnsUpAndStaysInsideItsFloor(string name, int others, int across)
        {
            GameObject prefab = Load(name);
            Assert.IsTrue(prefab.TryGetComponent(out PropSizeVariants sizes), $"{name} has no sizes");
            Assert.AreEqual(others, sizes.Variants.Count, "other sizes");
            foreach (PropSizeVariant variant in sizes.Variants)
            {
                Assert.IsNotNull(variant.Prefab);
                Assert.Greater(variant.Weight, 0f, variant.Prefab.name);
                Assert.IsFalse(variant.Prefab.TryGetComponent(out PropSizeVariants _), $"{variant.Prefab.name} lists sizes of its own");
                foreach (BoxCollider box in variant.Prefab.GetComponentsInChildren<BoxCollider>(true))
                {
                    Vector3 scale = box.transform.localScale;
                    if (across == 1 && variant.Footprint == Vector2Int.zero)
                    {
                        // Across its diagonal, as a loose prop takes any angle.
                        Assert.LessOrEqual(new Vector2(scale.x, scale.z).magnitude, PlaceholderPropBuilder.TileMetres + 0.001f, variant.Prefab.name);
                        continue;
                    }
                    // A wider prop stands square, on the footprint it is placed on or asks for.
                    Assert.LessOrEqual(scale.x, Mathf.Max(across, variant.Footprint.x) * PlaceholderPropBuilder.TileMetres, variant.Prefab.name);
                    Assert.LessOrEqual(scale.z, PlaceholderPropBuilder.TileMetres, variant.Prefab.name);
                }
            }

            var picked = new HashSet<GameObject>();
            var random = new RandomStream(new Seed(1));
            for (int i = 0; i < 200; i++) picked.Add(sizes.Pick(random));
            Assert.AreEqual(others + 1, picked.Count);
        }

        // A cover spot is chosen to hide someone, so a lower size would waste it.
        [TestCase("Rock")]
        [TestCase("Barrel")]
        public void ACoverSpotNeverGetsALowerSize(string name)
        {
            GameObject prefab = Load(name);
            Assert.IsTrue(prefab.TryGetComponent(out PropSizeVariants sizes));
            float listed = PropHeights.Of(prefab);
            var random = new RandomStream(new Seed(1));
            var picked = new HashSet<GameObject>();
            for (int i = 0; i < 200; i++) picked.Add(sizes.Pick(random, minHeight: listed));

            Assert.AreEqual(2, picked.Count, "the listed size and the larger one");
            foreach (GameObject size in picked) Assert.GreaterOrEqual(PropHeights.Of(size), listed, size.name);
        }

        [TestCase(4f, 2)]
        [TestCase(8f, 3)]
        public void TheLargeTreeOnlyGrowsInATallRoom(float roomHeight, int expected)
        {
            Assert.IsTrue(Load("ParkTree").TryGetComponent(out PropSizeVariants sizes));
            var random = new RandomStream(new Seed(1));
            var picked = new HashSet<GameObject>();
            for (int i = 0; i < 200; i++) picked.Add(sizes.Pick(random, roomHeight));

            Assert.AreEqual(expected, picked.Count);
        }

        // Every size of a cabinet can be searched, and a bigger one has a better chance of loot.
        [TestCase("Locker")]
        [TestCase("StorageCabinet")]
        [TestCase("FileCabinet")]
        [TestCase("OfficeDesk")]
        public void ABiggerCabinetIsLessOftenEmpty(string name)
        {
            GameObject prefab = Load(name);
            Assert.IsTrue(prefab.TryGetComponent(out PropSizeVariants sizes));
            float listed = Emptiness(prefab);

            Assert.Greater(Emptiness(sizes.Variants[0].Prefab), listed, "the small one");
            Assert.Less(Emptiness(sizes.Variants[1].Prefab), listed, "the large one");
        }

        // The weight of nothing in the loot table of a searchable prop.
        private static float Emptiness(GameObject prefab)
        {
            var container = prefab.GetComponentInChildren<LootContainer>();
            Assert.IsNotNull(container, $"{prefab.name} can't be searched");
            Assert.IsNotNull(container.GetComponent<Collider>(), $"{prefab.name} has nothing to aim at");
            var table = new SerializedObject(container.GetComponent<LootDropper>()).FindProperty("_table").objectReferenceValue;
            Assert.IsNotNull(table, $"{prefab.name} has no loot");
            return new SerializedObject(table).FindProperty("_nothingWeight").floatValue;
        }

        [Test]
        public void ABiggerSizeStandsAloneWhereAPileWouldBe()
        {
            GameObject crate = Load("Crate");
            int piles = 0, bigger = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(12, 12), seed);
                RoomPropPlacement stacks = Placement(4, 4);
                stacks.Prefabs = new[] { crate };
                stacks.Pile    = new IntRange(2, 3);
                planner.Add(stacks);

                var onTiles = new Dictionary<RectInt, List<GameObject>>();
                foreach (PlannedProp prop in planner.Planned)
                {
                    if (!onTiles.TryGetValue(prop.Tiles, out List<GameObject> there)) onTiles[prop.Tiles] = there = new List<GameObject>();
                    there.Add(prop.Prefab);
                }
                foreach (List<GameObject> there in onTiles.Values)
                {
                    if (there.Count > 1)
                    {
                        piles++;
                        foreach (GameObject prefab in there) Assert.AreSame(crate, prefab, $"seed {seed}: a bigger crate in a pile");
                    }
                    else
                    {
                        Assert.AreNotSame(crate, there[0], $"seed {seed}: a pile of one small crate");
                        bigger++;
                    }
                }
            }
            Assert.Greater(piles, 0);
            Assert.Greater(bigger, 0);
        }

        [Test]
        public void ABiggerSizeAgainstAWallKeepsItsBackInTheTile()
        {
            GameObject crate = Load("Crate");
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(10, 10), seed);
                RoomPropPlacement crates = Placement(6, 6, PropFacing.AgainstWall);
                crates.Prefabs = new[] { crate };
                crates.Jitter  = 1f;
                planner.Add(crates);

                foreach (PlannedProp prop in planner.Planned)
                {
                    float half = prop.Prefab.GetComponentInChildren<BoxCollider>().transform.localScale.z * 0.5f;
                    Vector2 fromMiddle = prop.Position - prop.Tiles.center;
                    float toBack = -Vector2.Dot(fromMiddle, Forward(prop)) * PlaceholderPropBuilder.TileMetres + half;
                    Assert.Less(toBack, PlaceholderPropBuilder.TileMetres * 0.5f, $"seed {seed}: {prop.Prefab.name} reaches through the wall");
                }
            }
        }

        // The shift a group's pieces make is what the size grew by on each side, so a chair
        // keeps its distance from the edge.
        [TestCase("Table")]
        [TestCase("OfficeDesk")]
        [TestCase("Bench")]
        [TestCase("Sofa")]
        public void AFurnitureSizesMemberShiftMatchesHowMuchItGrew(string name)
        {
            GameObject prefab = Load(name);
            Assert.IsTrue(prefab.TryGetComponent(out PropSizeVariants sizes));
            Vector2 listed = HalfSpan(prefab);

            foreach (PropSizeVariant variant in sizes.Variants)
            {
                Vector2 grown = HalfSpan(variant.Prefab) - listed;
                Assert.AreEqual(grown.x, variant.MemberShift.x, 0.001f, $"{variant.Prefab.name} across");
                Assert.AreEqual(grown.y, variant.MemberShift.y, 0.001f, $"{variant.Prefab.name} front to back");
            }
        }

        // A table with a chair at each end: on one tile there is no room to push them out, on two there is.
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void AGroupOnlyGetsSizesItsPiecesFitRound(int tiles, int expected)
        {
            GameObject table = Load("Table"), chair = Load("Chair");
            var seen = new HashSet<GameObject>();
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomPropPlanner planner = Planner(Room(12, 12), seed);
                RoomPropPlacement group = Placement(3, 3);
                group.Prefabs   = new[] { table };
                group.Footprint = new Vector2Int(tiles, tiles);
                group.Members   = new[]
                {
                    new PropMember { Prefabs = new[] { chair }, Offset = new Vector2(0.37f, 0f), Yaw = 270f },
                    new PropMember { Prefabs = new[] { chair }, Offset = new Vector2(-0.37f, 0f), Yaw = 90f },
                };
                planner.Add(group);

                for (int i = 0; i + 2 < planner.Planned.Count; i += 3)
                {
                    PlannedProp anchor = planner.Planned[i];
                    seen.Add(anchor.Prefab);
                    float reach = Vector2.Distance(anchor.Position, planner.Planned[i + 1].Position) * PlaceholderPropBuilder.TileMetres;
                    Assert.AreEqual(0.37f * PlaceholderPropBuilder.TileMetres + HalfSpan(anchor.Prefab).x - HalfSpan(table).x, reach, 0.001f,
                                    $"seed {seed}: a chair keeps its distance from the edge of {anchor.Prefab.name}");
                }
            }
            Assert.AreEqual(expected, seen.Count);
        }

        // The long container takes a third tile when the floor is free; the other sizes keep the two.
        [Test]
        public void ALongContainerClaimsTheFloorItNeeds()
        {
            GameObject container = Load("CargoContainer");
            int longOnes = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Room(14, 14, new Vector2Int(0, 7));
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.6f);
                RoomPropPlacement stacks = Placement(4, 4);
                stacks.Prefabs   = new[] { container };
                stacks.Footprint = new Vector2Int(2, 1);
                planner.Add(stacks);

                int claimed = 0;
                foreach (PlannedProp prop in planner.Planned)
                {
                    bool isLong = prop.Prefab.name == "CargoContainerLong";
                    if (isLong) longOnes++;
                    Assert.AreEqual(isLong ? 3 : 2, prop.Tiles.width * prop.Tiles.height, $"seed {seed}: {prop.Prefab.name}");
                    claimed += prop.Tiles.width * prop.Tiles.height;
                }
                Assert.AreEqual(claimed, TilesWith(room, RoomTileTags.Prop), $"seed {seed}: containers overlap or leave floor claimed");
            }
            Assert.Greater(longOnes, 0);
        }

        // Half the width and depth of a prefab's widest solid part, in metres.
        private static Vector2 HalfSpan(GameObject prefab)
        {
            Vector2 half = Vector2.zero;
            foreach (BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true))
                half = Vector2.Max(half, new Vector2(box.transform.localScale.x, box.transform.localScale.z) * 0.5f);
            return half;
        }

        private static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPropBuilder.PathFor(name));
            Assert.IsNotNull(prefab, name);
            return prefab;
        }
    }
}
