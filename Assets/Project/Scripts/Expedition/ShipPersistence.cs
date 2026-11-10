using System;
using System.Collections.Generic;
using CGD.Items;
using CGD.Saving;

namespace CGD.Expedition
{
    // Keeps the ledger's hold, kit and upgrades in a SaveFile. Changes only mark the save dirty; the owner
    // calls SaveIfDirty once a frame, so a run settling (dozens of item moves) is one write.
    public class ShipPersistence : IDisposable
    {
        private readonly ExpeditionLedger _ledger;
        private readonly ItemCatalog      _catalog;
        private readonly SaveFile         _file;

        public bool IsDirty { get; private set; }
        public string Path => _file.Path;
        // Entries the last load couldn't restore, or the last save couldn't record (see InventoryRecord).
        public int SkippedOnLoad { get; private set; }
        public int SkippedOnSave { get; private set; }
        public ShipLoadResult? LastLoad { get; private set; }
        public bool? LastWriteSucceeded { get; private set; }

        public ShipPersistence(ExpeditionLedger ledger, ItemCatalog catalog, SaveFile file)
        {
            _ledger  = ledger  ?? throw new ArgumentNullException(nameof(ledger));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _file    = file    ?? throw new ArgumentNullException(nameof(file));

            _ledger.Storage.Changed  += MarkDirty;
            _ledger.Kit.Changed      += MarkDirty;
            _ledger.Upgrades.Changed += MarkDirty;
        }

        public void Dispose()
        {
            _ledger.Storage.Changed  -= MarkDirty;
            _ledger.Kit.Changed      -= MarkDirty;
            _ledger.Upgrades.Changed -= MarkDirty;
        }

        // Replaces the hold, kit and upgrades with the saved ones. Call before the first run is deployed.
        public ShipLoadResult Load()
        {
            SkippedOnLoad = 0;
            LastLoad = ReadInto();
            IsDirty = false;
            return LastLoad.Value;
        }

        public bool SaveIfDirty() => !IsDirty || Save();

        public bool Save()
        {
            var save = new ShipSave
            {
                hold = InventoryRecord.Capture(_ledger.Storage, _catalog, out int skippedHold),
                kit  = InventoryRecord.Capture(_ledger.Kit,     _catalog, out int skippedKit),
            };
            foreach (KeyValuePair<string, int> owned in _ledger.Upgrades.Owned)
                save.upgrades.Add(new ShipSave.UpgradeRecord { id = owned.Key, level = owned.Value });
            SkippedOnSave = skippedHold + skippedKit;

            bool written = _file.Write(save);
            LastWriteSucceeded = written;
            // Not retried every frame after a failure: the next change tries again.
            IsDirty = false;
            return written;
        }

        // A fresh start: empties the hold and kit, removes the upgrades and deletes the save.
        public void Wipe()
        {
            _ledger.Storage.Clear();
            _ledger.Kit.Clear();
            _ledger.Upgrades.Clear();
            _file.Delete();
            IsDirty = false;
        }

        private ShipLoadResult ReadInto()
        {
            if (!_file.Exists) return ShipLoadResult.NoSave;

            if (!_file.TryRead(out ShipSave save) || save.version < 1 || save.version > ShipSave.CurrentVersion)
            {
                _file.SetAsideUnreadable();
                return ShipLoadResult.SetAside;
            }

            SkippedOnLoad = (save.hold ?? new InventoryRecord()).RestoreInto(_ledger.Storage, _catalog)
                          + (save.kit  ?? new InventoryRecord()).RestoreInto(_ledger.Kit,     _catalog);
            _ledger.Upgrades.Restore(Owned(save.upgrades));
            return ShipLoadResult.Loaded;
        }

        private static IEnumerable<KeyValuePair<string, int>> Owned(List<ShipSave.UpgradeRecord> records)
        {
            if (records == null) yield break;
            foreach (ShipSave.UpgradeRecord record in records)
                if (record != null) yield return new KeyValuePair<string, int>(record.id, record.level);
        }

        private void MarkDirty() => IsDirty = true;
    }
}
