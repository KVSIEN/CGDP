using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CGD.Crafting;
using CGD.Expedition;
using CGD.Items;
using CGD.Saving;
using CGD.Stats;

namespace CGD.Tests
{
    // Ship upgrades are bought level by level from the hold, paid in full or not at all, and
    // kept on disk with the rest of the ship.
    public class ShipUpgradesTests
    {
        private string             _directory;
        private ResourceDefinition _scrap;
        private StatModifierPreset _bonusOne;
        private StatModifierPreset _bonusTwo;
        private ShipUpgrade        _hull;
        private ShipUpgradeCatalog _upgrades;
        private ItemCatalog        _items;

        [SetUp]
        public void CreateUpgrade()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cgd-upgrade-tests-" + System.Guid.NewGuid().ToString("N"));
            _scrap    = ScriptableObject.CreateInstance<ResourceDefinition>();
            _bonusOne = ScriptableObject.CreateInstance<StatModifierPreset>();
            _bonusTwo = ScriptableObject.CreateInstance<StatModifierPreset>();
            _hull     = ScriptableObject.CreateInstance<ShipUpgrade>();
            _hull.SetUp("hull",
                new ShipUpgradeLevel { Cost = new[] { new ItemAmount(_scrap, 5) }, Bonus = _bonusOne },
                new ShipUpgradeLevel { Cost = new[] { new ItemAmount(_scrap, 4), new ItemAmount(_scrap, 6) }, Bonus = _bonusTwo });
            _upgrades = ScriptableObject.CreateInstance<ShipUpgradeCatalog>();
            _upgrades.SetUpgrades(_hull);
            _items = ScriptableObject.CreateInstance<ItemCatalog>();
            _items.SetEntries(new[] { ("scrap", (ItemDefinition)_scrap) });
        }

        [TearDown]
        public void Clean()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
            Object.DestroyImmediate(_scrap);
            Object.DestroyImmediate(_bonusOne);
            Object.DestroyImmediate(_bonusTwo);
            Object.DestroyImmediate(_hull);
            Object.DestroyImmediate(_upgrades);
            Object.DestroyImmediate(_items);
        }

        [Test]
        public void ALevelIsPaidFromTheHold()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 7);

            Assert.IsTrue(ledger.BuyUpgrade(_hull));
            Assert.AreEqual(1, ledger.Upgrades.LevelOf(_hull));
            Assert.AreEqual(2, ledger.Storage.CountOf(_scrap));
        }

        [Test]
        public void NothingIsTakenWhenTheHoldFallsShort()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 14);
            Assert.IsTrue(ledger.BuyUpgrade(_hull));

            // Level 2 lists scrap twice: 10 in all, and only 9 are left.
            Assert.IsFalse(ledger.Upgrades.CanAfford(_hull, ledger.Storage));
            Assert.IsFalse(ledger.BuyUpgrade(_hull));
            Assert.AreEqual(9, ledger.Storage.CountOf(_scrap));
            Assert.AreEqual(1, ledger.Upgrades.LevelOf(_hull));
        }

        [Test]
        public void AMaxedUpgradeCantBeBoughtAgain()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 100);
            Assert.IsTrue(ledger.BuyUpgrade(_hull));
            Assert.IsTrue(ledger.BuyUpgrade(_hull));

            Assert.IsTrue(ledger.Upgrades.IsMaxed(_hull));
            Assert.IsNull(ledger.Upgrades.Next(_hull));
            Assert.IsFalse(ledger.BuyUpgrade(_hull));
            Assert.AreEqual(85, ledger.Storage.CountOf(_scrap));
        }

        [Test]
        public void EveryOwnedLevelGivesItsBonus()
        {
            var ledger = new ExpeditionLedger();
            var bonuses = new List<ShipUpgradeLevel>();
            ledger.Upgrades.CollectBonuses(_upgrades, bonuses);
            Assert.AreEqual(0, bonuses.Count);

            ledger.Storage.Add(_scrap, 100);
            ledger.BuyUpgrade(_hull);
            ledger.BuyUpgrade(_hull);
            ledger.Upgrades.CollectBonuses(_upgrades, bonuses);

            Assert.AreEqual(2, bonuses.Count);
            Assert.AreSame(_bonusOne, bonuses[0].Bonus);
            Assert.AreSame(_bonusTwo, bonuses[1].Bonus);
        }

        [Test]
        public void DyingNeverCostsUpgrades()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 5);
            ledger.BuyUpgrade(_hull);

            ledger.Deploy();
            ledger.Die(new Inventory());

            Assert.AreEqual(1, ledger.Upgrades.LevelOf(_hull));
        }

        [Test]
        public void UpgradesComeBackAfterARestart()
        {
            var file = new SaveFile(Path.Combine(_directory, "ship.json"));
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 5);
            using (var save = new ShipPersistence(ledger, _items, file))
            {
                ledger.BuyUpgrade(_hull);
                Assert.IsTrue(save.IsDirty, "buying marks the save dirty");
                Assert.IsTrue(save.Save());
            }

            var restarted = new ExpeditionLedger();
            using var load = new ShipPersistence(restarted, _items, file);
            Assert.AreEqual(ShipLoadResult.Loaded, load.Load());
            Assert.AreEqual(1, restarted.Upgrades.LevelOf(_hull));
            Assert.IsFalse(load.IsDirty);
        }

        [Test]
        public void AVersionOneSaveLoadsWithoutUpgrades()
        {
            Directory.CreateDirectory(_directory);
            string path = Path.Combine(_directory, "ship.json");
            File.WriteAllText(path, "{ \"version\": 1, \"hold\": { \"stacks\": [], \"gear\": [] }, \"kit\": { \"stacks\": [], \"gear\": [] } }");

            var ledger = new ExpeditionLedger();
            using var load = new ShipPersistence(ledger, _items, new SaveFile(path));
            Assert.AreEqual(ShipLoadResult.Loaded, load.Load());
            Assert.AreEqual(0, ledger.Upgrades.LevelOf(_hull));
        }
    }
}
