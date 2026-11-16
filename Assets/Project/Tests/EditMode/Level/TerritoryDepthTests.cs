using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // Territory depth: a room's map influence sets how strongly its faction's design shows
    // (full in the core, thinner on the frontier), and FactionStyle blends each part of the
    // design toward a plain room by that strength.
    public class TerritoryDepthTests
    {
        private const string DesignsPath = "Assets/Project/Data/Level/Designs/";

        private static readonly TerritoryDepth Depth = new(0.7f, 0.35f, 0.4f);

        [TestCase(1f, 1f)]
        [TestCase(0.7f, 1f)]
        [TestCase(0.525f, 0.7f)]
        [TestCase(0.35f, 0.4f)]
        [TestCase(0.2f, 0.4f)]
        [TestCase(0f, 1f)]   // no influence painted: full strength
        public void InfluenceSetsTheStrength(float influence, float strength)
        {
            Assert.AreEqual(strength, Depth.StrengthOf(influence), 0.001f);
        }

        [Test]
        public void TheDepthIsNamed()
        {
            Assert.AreEqual("core", Depth.NameOf(1f));
            Assert.AreEqual("held", Depth.NameOf(0.7f));
            Assert.AreEqual("frontier", Depth.NameOf(0.4f));
        }

        [Test]
        public void AtFullStrengthTheStyleIsTheDesign()
        {
            var design = AssetDatabase.LoadAssetAtPath<FactionDesign>(DesignsPath + "VoidFactionDesign.asset");
            var style = new FactionStyle(design);

            Assert.AreEqual(design.RoomSizeScale, style.RoomSizeScale);
            Assert.AreEqual(design.FillShare, style.FillShare);
            Assert.AreEqual(design.PropScale, style.PropScale);
            Assert.AreSame(design.Posts, style.Posts);
        }

        [Test]
        public void OnTheFrontierTheStyleMovesTowardAPlainRoom()
        {
            var design = AssetDatabase.LoadAssetAtPath<FactionDesign>(DesignsPath + "VoidFactionDesign.asset");
            var style = new FactionStyle(design, 0.4f);

            Assert.AreEqual(Mathf.Lerp(1f, design.RoomSizeScale, 0.4f), style.RoomSizeScale, 0.0001f);
            Assert.AreEqual(Mathf.Lerp(RoomPropPlanner.DefaultFillShare, design.FillShare, 0.4f), style.FillShare, 0.0001f);
            Assert.AreEqual(Mathf.Lerp(1f, design.PropScale, 0.4f), style.PropScale, 0.0001f);
        }

        [Test]
        public void AStyleWithoutADesignIsAPlainRoom()
        {
            FactionStyle style = default;
            Assert.AreEqual(1f, style.RoomSizeScale);
            Assert.AreEqual(RoomPropPlanner.DefaultFillShare, style.FillShare);
            Assert.AreEqual(1f, style.PropScale);
            Assert.IsNull(style.Posts);
        }

        [Test]
        public void PostPreferencesFadeTowardPlainFloorButKeepWhatTheyHold()
        {
            var ambusher = new PostPreference(4f, 1f, 1f, 1f, 1f, RoomPostKinds.Concealed);
            PostPreference frontier = ambusher.Toward(0.4f);

            Assert.AreEqual(2.2f, frontier.WeightOf(RoomPostKinds.Concealed), 0.0001f);
            Assert.AreEqual(1f, frontier.WeightOf(RoomPostKinds.None));
            Assert.AreEqual(ambusher.Hold, frontier.Hold);
        }

        [Test]
        public void ChanceBasedPartsNeverShowAtZeroAndDrawNothingAtFull()
        {
            var none = new FactionStyle(null, 0f);
            var random = new RandomStream(new Seed(5UL));
            for (int i = 0; i < 50; i++) Assert.IsFalse(none.Shows(random));

            var full = new FactionStyle(null, 1f);
            var drawn = new RandomStream(new Seed(9UL));
            var fresh = new RandomStream(new Seed(9UL));
            Assert.IsTrue(full.Shows(drawn));
            Assert.AreEqual(fresh.Value, drawn.Value, "a full-strength room drew a random number");
        }

        [Test]
        public void AdditionsAreBuiltByChance()
        {
            var wall = ScriptableObject.CreateInstance<PartitionStructureRule>();
            try
            {
                Assert.AreEqual(1, Plan(wall, 0f).ZoneCount, "built at chance 0");
                Assert.AreEqual(2, Plan(wall, 1f).ZoneCount, "not built at chance 1");
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        private static RoomStructure Plan(RoomStructureRule addition, float chance)
        {
            MapNode node = new MapGraph().AddNode(MapNodeType.Combat, Vector2.zero);
            var room = new LevelRoom(node, RoomFootprint.Rectangle(new RectInt(0, 0, 12, 8)), null, 4f);
            return new RoomStructurePlanner().Plan(room, System.Array.Empty<LevelDoorway>(), new RandomStream(new Seed(3UL)),
                                                   factionAdditions: new[] { addition }, additionChance: chance);
        }
    }
}
