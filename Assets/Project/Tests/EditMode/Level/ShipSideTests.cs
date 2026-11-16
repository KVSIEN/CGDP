using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Level;
using CGD.Map;

namespace CGD.Tests
{
    // A room's side of the ship: its section's where its place can sit on it, else the place's
    // usual one; and the shipped sections, places and side designs that make that work.
    public class ShipSideTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private static RoomFunction Function(params ShipSide[] sides)
        {
            var function = ScriptableObject.CreateInstance<RoomFunction>();
            Set(function, "_sides", sides);
            return function;
        }

        private static MapSectionDefinition Section(ShipSide side)
        {
            var section = ScriptableObject.CreateInstance<MapSectionDefinition>();
            Set(section, "_side", side);
            return section;
        }

        [Test]
        public void ARoomTakesItsSectionsSideWhenItsPlaceCanSitThere()
        {
            RoomFunction restaurant = Function(ShipSide.GuestFirstClass, ShipSide.Crew);
            MapSectionDefinition engineering = Section(ShipSide.Crew);
            try
            {
                Assert.AreEqual(ShipSide.Crew, restaurant.SideIn(engineering), "a restaurant in Engineering is a crew mess");
            }
            finally
            {
                Object.DestroyImmediate(restaurant);
                Object.DestroyImmediate(engineering);
            }
        }

        [Test]
        public void OtherwiseItKeepsItsPlacesUsualSide()
        {
            RoomFunction engineRoom = Function(ShipSide.Crew);
            MapSectionDefinition commerce = Section(ShipSide.GuestFirstClass);
            RoomFunction junction = Function();
            try
            {
                Assert.AreEqual(ShipSide.Crew, engineRoom.SideIn(commerce));
                Assert.AreEqual(ShipSide.Crew, engineRoom.SideIn(null));
                Assert.AreEqual(ShipSide.None, junction.SideIn(commerce), "a place with no sides has none");
            }
            finally
            {
                Object.DestroyImmediate(engineRoom);
                Object.DestroyImmediate(commerce);
                Object.DestroyImmediate(junction);
            }
        }

        [TestCase("Commerce", ShipSide.GuestFirstClass)]
        [TestCase("Habitation", ShipSide.GuestSteerage)]
        [TestCase("Engineering", ShipSide.Crew)]
        public void EachSectionIsOnItsSide(string section, ShipSide side)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapSectionDefinition>($"Assets/Project/Data/Map/Sections/{section}MapSection.asset");
            Assert.IsNotNull(asset);
            Assert.AreEqual(side, asset.Side);
        }

        [TestCase(ShipSide.GuestFirstClass)]
        [TestCase(ShipSide.GuestSteerage)]
        [TestCase(ShipSide.Crew)]
        public void EachSideHasADesign(ShipSide side)
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            Assert.IsNotNull(settings.DesignFor(side), $"no design for {side}");
        }

        [Test]
        public void CrewRoomsGetAStaffDoor()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            ShipSideDesign crew = settings.DesignFor(ShipSide.Crew);
            Assert.IsNotNull(crew.Dressing);
            Assert.IsTrue(crew.Dressing.Props.Count > 0 && crew.Dressing.Props[0].Required, "the service door is the crew dressing's first, required piece");
        }

        [Test]
        public void EveryPlaceButAJunctionIsOnASide()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            foreach (RoomFunction function in settings.Functions)
            {
                if (function.name == "JunctionRoomFunction") continue;
                Assert.Greater(function.Sides.Count, 0, $"{function.name} is on no side");
                Assert.AreNotEqual(ShipSide.None, function.Sides[0], $"{function.name}'s usual side is None");
            }
        }
    }
}
