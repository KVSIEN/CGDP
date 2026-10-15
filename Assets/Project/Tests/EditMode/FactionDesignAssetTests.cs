using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Factions;
using CGD.Level;

namespace CGD.Tests
{
    // Checks the shipped faction data: every faction is built by a complete design, and every
    // prop a dressing places is a real prefab of the right kind. Guards against broken references
    // and a decoration that would block the way (or furniture you could walk through).
    public class FactionDesignAssetTests
    {
        private const string SettingsPath = "Assets/Project/Data/Level/DefaultLevelBuildSettings.asset";

        private static LevelBuildSettings Settings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LevelBuildSettings>(SettingsPath);
            Assert.IsNotNull(settings, $"Missing {SettingsPath}");
            return settings;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"Missing {path}");
            return asset;
        }

        [TestCase("Tech")]
        [TestCase("Bio")]
        [TestCase("Void")]
        public void EveryFactionHasACompleteDesign(string name)
        {
            var faction = Load<FactionDefinition>($"Assets/Project/Data/Factions/{name}Faction.asset");
            FactionDesign design = Settings().DesignFor(faction);

            Assert.IsNotNull(design, $"{name} has no design in the level build settings");
            Assert.Greater(design.Structure.Count, 0, $"{name} builds no structure");
            foreach (RoomStructureRule rule in design.Structure) Assert.IsNotNull(rule, $"{name} lists a missing structure rule");
            Assert.IsNotNull(design.Dressing, $"{name} has no dressing of its own");

            var cover = Load<RoomDressing>("Assets/Project/Data/Level/Dressing/CombatCoverRoomDressing.asset");
            var loot = Load<RoomDressing>("Assets/Project/Data/Level/Dressing/LootRoomDressing.asset");
            Assert.AreNotSame(cover, design.Dress(cover), $"{name} has no version of the combat cover");
            Assert.AreNotSame(loot, design.Dress(loot), $"{name} has no version of the loot dressing");
        }

        [Test]
        public void EveryPlacedPropIsARealPrefabOfTheRightKind()
        {
            var checkedPrefabs = new HashSet<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoomDressing"))
            {
                var dressing = AssetDatabase.LoadAssetAtPath<RoomDressing>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (RoomPropPlacement placement in dressing.Props)
                {
                    Assert.Greater(placement.Prefabs.Length, 0, $"{dressing.name} has a placement with no prefab");
                    foreach (GameObject prefab in placement.Prefabs)
                    {
                        Assert.IsNotNull(prefab, $"{dressing.name} places a missing prefab");
                        if (!checkedPrefabs.Add(prefab)) continue;

                        bool solid = prefab.GetComponentInChildren<Collider>() != null;
                        if (placement.Decor) Assert.IsFalse(solid, $"{prefab.name} is ground cover but has a collider");
                        else Assert.IsTrue(solid, $"{prefab.name} is furniture but has no collider");
                    }
                }
            }
            Assert.Greater(checkedPrefabs.Count, 10);
        }
    }
}
