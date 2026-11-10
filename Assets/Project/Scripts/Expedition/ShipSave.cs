using System;
using System.Collections.Generic;
using CGD.Items;

namespace CGD.Expedition
{
    // What persists between play sessions: the ship's hold, the kit packed for the next run
    // and the ship's upgrades. A run in progress is never saved — quitting mid-run counts as
    // not making it out, as leaving the level does.
    [Serializable]
    public class ShipSave
    {
        // Bump when the layout changes, and teach ShipPersistence to read the old one.
        // 2: upgrades (a version 1 save simply has none).
        public const int CurrentVersion = 2;

        public int                 version  = CurrentVersion;
        public InventoryRecord     hold     = new();
        public InventoryRecord     kit      = new();
        public List<UpgradeRecord> upgrades = new();

        [Serializable]
        public class UpgradeRecord
        {
            public string id;
            public int    level;
        }
    }
}
