using System.IO;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Expedition;
using CGD.Items;
using CGD.Saving;

namespace CGD.Tests
{
    public class ShipPersistenceTests
    {
        private string             _directory;
        private ResourceDefinition _scrap;
        private ArmorDefinition    _vest;
        private ItemCatalog        _catalog;

        [SetUp]
        public void CreateCatalog()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cgd-save-tests-" + System.Guid.NewGuid().ToString("N"));
            _scrap   = ScriptableObject.CreateInstance<ResourceDefinition>();
            _vest    = ScriptableObject.CreateInstance<ArmorDefinition>();
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetEntries(new[] { ("scrap", (ItemDefinition)_scrap), ("vest", (ItemDefinition)_vest) });
        }

        [TearDown]
        public void Clean()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
            Object.DestroyImmediate(_scrap);
            Object.DestroyImmediate(_vest);
            Object.DestroyImmediate(_catalog);
        }

        private SaveFile ShipFile => new(Path.Combine(_directory, "ship.json"));

        [Test]
        public void HoldAndKitComeBackAfterARestart()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 12);
            var variants = new SeedVariants();
            variants.Set(ItemRoll.StatsLayer, 2);
            ItemInstance vest = _vest.CreateInstance(_vest.Roll(ItemTier.Common, new Seed(ulong.MaxValue - 7), variants));
            ledger.Kit.Add(vest);
            using (var save = new ShipPersistence(ledger, _catalog, ShipFile)) Assert.IsTrue(save.Save());

            var restarted = new ExpeditionLedger();
            using var load = new ShipPersistence(restarted, _catalog, ShipFile);

            Assert.AreEqual(ShipLoadResult.Loaded, load.Load());
            Assert.AreEqual(0, load.SkippedOnLoad);
            Assert.AreEqual(12, restarted.Storage.CountOf(_scrap));
            Assert.AreEqual(1, restarted.Kit.Items.Count);
            ItemInstance loaded = restarted.Kit.Items[0];
            Assert.AreSame(_vest, loaded.Definition);
            Assert.AreEqual(vest.Seed, loaded.Seed);
            Assert.AreEqual(vest.SeedText, loaded.SeedText);
            Assert.AreEqual(vest.Tier, loaded.Tier);
        }

        [Test]
        public void ChangesOnlyMarkTheSaveDirtyUntilItIsWritten()
        {
            var ledger = new ExpeditionLedger();
            using var save = new ShipPersistence(ledger, _catalog, ShipFile);
            save.Load();
            Assert.IsFalse(save.IsDirty, "Loading doesn't count as a change");

            ledger.Storage.Add(_scrap, 3);
            ledger.Pack(_scrap, 2);
            Assert.IsTrue(save.IsDirty);
            Assert.IsFalse(ShipFile.Exists, "Nothing is written until the owner flushes");

            Assert.IsTrue(save.SaveIfDirty());
            Assert.IsFalse(save.IsDirty);
            Assert.IsTrue(ShipFile.Exists);
        }

        [Test]
        public void ItemsRemovedFromTheProjectAreSkippedAndTheRestLoads()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 5);
            ledger.Storage.Add(_vest.CreateInstance());
            using (var save = new ShipPersistence(ledger, _catalog, ShipFile)) save.Save();

            var smaller = ScriptableObject.CreateInstance<ItemCatalog>();
            smaller.SetEntries(new[] { ("scrap", (ItemDefinition)_scrap) });
            var restarted = new ExpeditionLedger();
            using var load = new ShipPersistence(restarted, smaller, ShipFile);
            load.Load();

            Assert.AreEqual(1, load.SkippedOnLoad);
            Assert.AreEqual(5, restarted.Storage.CountOf(_scrap));
            Assert.AreEqual(0, restarted.Storage.Items.Count);
            Object.DestroyImmediate(smaller);
        }

        [Test]
        public void ItemsTheCatalogDoesNotListAreLeftOutOfTheSave()
        {
            var unlisted = ScriptableObject.CreateInstance<ResourceDefinition>();
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(unlisted, 4);
            ledger.Storage.Add(_scrap, 1);
            using var save = new ShipPersistence(ledger, _catalog, ShipFile);

            Assert.IsTrue(save.Save());
            Assert.AreEqual(1, save.SkippedOnSave);
            Object.DestroyImmediate(unlisted);
        }

        [TestCase("{ not json")]
        [TestCase("{ \"version\": 99 }")]
        public void AnUnusableSaveIsMovedAsideAndTheShipStartsEmpty(string contents)
        {
            Directory.CreateDirectory(_directory);
            System.IO.File.WriteAllText(ShipFile.Path, contents);
            var ledger = new ExpeditionLedger();
            using var load = new ShipPersistence(ledger, _catalog, ShipFile);

            Assert.AreEqual(ShipLoadResult.SetAside, load.Load());
            Assert.AreEqual(0, ledger.Storage.SlotCount);
            Assert.IsFalse(ShipFile.Exists);
            Assert.IsTrue(System.IO.File.Exists(ShipFile.Path + ".unreadable"));
        }

        [Test]
        public void NoSaveFileIsAFreshStart()
        {
            using var load = new ShipPersistence(new ExpeditionLedger(), _catalog, ShipFile);
            Assert.AreEqual(ShipLoadResult.NoSave, load.Load());
        }

        [Test]
        public void WipeEmptiesTheShipAndDeletesTheSave()
        {
            var ledger = new ExpeditionLedger();
            ledger.Storage.Add(_scrap, 5);
            using var save = new ShipPersistence(ledger, _catalog, ShipFile);
            save.Save();

            save.Wipe();

            Assert.AreEqual(0, ledger.Storage.SlotCount);
            Assert.IsFalse(ShipFile.Exists);
            Assert.IsFalse(save.IsDirty);
        }

        [Test]
        public void OverwritingASaveLeavesNoTemporaryFileBehind()
        {
            var ledger = new ExpeditionLedger();
            using var save = new ShipPersistence(ledger, _catalog, ShipFile);
            ledger.Storage.Add(_scrap, 1);
            save.Save();
            ledger.Storage.Add(_scrap, 1);
            save.Save();

            Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
        }
    }
}
